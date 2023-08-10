using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Common.Adapters;
using Common.Constants;
using Common.Enums;
using Common.Experts;
using Common.Facades;
using Common.Models;
using ExpertService.Providers;
using HistoryPlatform.Repositories;

namespace HistoryPlatform.Providers
{
    public class TradeProvider : ITradeProvider
    {
        public IReadOnlyCollection<IExpert> Experts => new ReadOnlyCollection<IExpert>(_experts);

        private readonly IHistoryBarsRepository _historyBarsRepository;
        private readonly IPluginProvider _pluginProvider;
        private readonly IConsole _console;
        private readonly ITradeAdapter _tradeAdapter;

        private List<IExpert> _experts;

        public TradeProvider(
            IPluginProvider pluginProvider,
            IHistoryBarsRepository historyBarsRepository,
            IConsole console,
            ITradeAdapter tradeAdapter)
        {
            _pluginProvider = pluginProvider;
            _historyBarsRepository = historyBarsRepository;
            _console = console;
            _tradeAdapter = tradeAdapter;
        }

        public async Task<(List<ClosedOrder>, IDictionary<DateTime, IDictionary<string, decimal>>)> ExecuteExpert(
            IExpert expert,
            TimeFrame timeFrame,
            string objGuid,
            bool closeAllOpen,
            List<PriceTick> priceTickList)
        {
            var signalHistory = new Dictionary<DateTime, IDictionary<string, decimal>>();
            var activeOrders = new List<ActiveOrder>();
            var closedOrders = new List<ClosedOrder>();
            var minutes = await _tradeAdapter.GetTimeFrameMinutes(timeFrame).ConfigureAwait(false);

            foreach (var priceTick in priceTickList)
            {
                await ProcessTakeProfitStopLoss(priceTick, activeOrders, closedOrders).ConfigureAwait(false);
                await _historyBarsRepository.GetLastHistoryBars(priceTick.TickServerTime, objGuid).ConfigureAwait(false);
                var priceHistoryCacheByKey = await _historyBarsRepository.GetpriceHistoryCacheByKey(objGuid).ConfigureAwait(false);
                var (result, signals) = expert.Trade(priceTick, new ReadOnlyCollection<ActiveOrder>(activeOrders), priceHistoryCacheByKey);
                await ProcessOpenClose(result, priceTick, activeOrders, closedOrders, minutes).ConfigureAwait(false);
                await ProcessSignals(signalHistory, signals).ConfigureAwait(false);
            }

            if (activeOrders.Any() && closeAllOpen)
            {
                CloseAllOrders(priceTickList[priceTickList.Count - 1], activeOrders, closedOrders);
            }

            return (closedOrders, signalHistory);
        }

        private async Task ProcessSignals(IDictionary<DateTime, IDictionary<string, decimal>> signalHistory, SignalValues signals)
        {
            if (signals == null)
            {
                return;
            }

            var barTime = signals.SignalTime;
            if (signalHistory.ContainsKey(barTime))
            {
                return;
            }

            signalHistory.Add(barTime, signals.Signals);
        }

        public async Task LoadExperts()
        {
            _experts = await _pluginProvider.ListExperts().ConfigureAwait(false);
        }

        private async Task ProcessTakeProfitStopLoss(PriceTick priceTick, IList<ActiveOrder> activeOrders, IList<ClosedOrder> closedOrders)
        {
            foreach (var order in activeOrders.ToList())
            {
                var stopLossPriceDiff = 0.0m;
                var takeProfitPriceDiff = 0.0m;
                switch (order.BuySell)
                {
                    case OrderBuySell.Buy:
                        stopLossPriceDiff = priceTick.Bid - order.StopLossPrice;
                        takeProfitPriceDiff = priceTick.Bid - order.TakeProfitPrice;
                        break;
                    case OrderBuySell.Sell:
                        stopLossPriceDiff = order.StopLossPrice - priceTick.Ask;
                        takeProfitPriceDiff = order.TakeProfitPrice - priceTick.Ask;
                        break;
                }

                if (stopLossPriceDiff <= 0.0m && order.StopLossPrice > 0.0m)
                {
                    CloseOrder(priceTick, order, activeOrders, closedOrders);
                }

                if (takeProfitPriceDiff >= 0.0m && order.TakeProfitPrice > 0.0m)
                {
                    CloseOrder(priceTick, order, activeOrders, closedOrders);
                }
            }
        }

        private async Task ProcessOpenClose(
            ICollection<TradingCommand> commands,
            PriceTick priceTick,
            IList<ActiveOrder> activeOrders,
            IList<ClosedOrder> closedOrders,
            int minutes)
        {
            if (commands == null || commands.Count == 0)
            {
                return;
            }

            foreach (var command in commands)
            {
                if (command.OpenClose != OrderOpenClose.Close)
                {
                    continue;
                }

                var activeOrder = activeOrders.SingleOrDefault(r => r.OrderId == command.OrderId);
                var timeOpen = activeOrder.OpenTime;
                var timeClose = priceTick.TickServerTime;

                var tsO = new TimeSpan(timeOpen.Hour, timeOpen.Minute, 0).TotalMinutes + minutes;
                var tsC = new TimeSpan(timeClose.Hour, timeClose.Minute, 0).TotalMinutes;
                if (timeOpen.Date != timeClose.Date)
                {
                    continue;
                }

                if (tsO > tsC)
                {
                    return;
                }
            }

            foreach (var tradeCommand in commands)
            {
                switch (tradeCommand.OpenClose)
                {
                    case OrderOpenClose.Open:
                        await OpenCommand(tradeCommand, priceTick, activeOrders).ConfigureAwait(false);
                        break;
                    case OrderOpenClose.Close:
                        await CloseCommand(tradeCommand, priceTick, activeOrders, closedOrders).ConfigureAwait(false);
                        break;
                    case OrderOpenClose.Update:
                        await UpdateCommand(tradeCommand, activeOrders).ConfigureAwait(false);
                        break;
                }
            }
        }

        private async Task UpdateCommand(TradingCommand tradeCommand, IList<ActiveOrder> activeOrders)
        {
            var itemToUpdate = activeOrders.SingleOrDefault(r => r.OrderId == tradeCommand.OrderId);
            if (itemToUpdate == null)
            {
                return;
            }

            itemToUpdate.StopLossPrice = tradeCommand.StopLoss > 0.0m ? tradeCommand.StopLoss : itemToUpdate.StopLossPrice;
            itemToUpdate.TakeProfitPrice = tradeCommand.TakeProfit > 0.0m ? tradeCommand.TakeProfit : itemToUpdate.TakeProfitPrice;
        }

        private async Task OpenCommand(TradingCommand tradeOrder, PriceTick priceTick, IList<ActiveOrder> activeOrders)
        {
            if (tradeOrder.Lots < 1)
            {
                _console.WriteLine("Negative lots");
                return;
            }

            var orderId = Guid.NewGuid();
            var order = new ActiveOrder
            {
                OrderId = orderId.ToString(),
                Lots = tradeOrder.Lots,
                BuySell = tradeOrder.BuySell,
                Instrument = tradeOrder.Instrument,
                OpenTime = priceTick.TickServerTime,
                OpenPrice = tradeOrder.BuySell == OrderBuySell.Buy ? priceTick.Ask : priceTick.Bid,
            };
            if (tradeOrder.StopLoss > 0.0m)
            {
                order.StopLossPrice = tradeOrder.BuySell == OrderBuySell.Buy ? order.OpenPrice - tradeOrder.StopLoss : order.OpenPrice + tradeOrder.StopLoss;
            }

            activeOrders.Add(order);
        }

        private async Task CloseCommand(TradingCommand tradeOrder, PriceTick priceTick, IList<ActiveOrder> activeOrders, IList<ClosedOrder> closedOrders)
        {
            var itemToRemove = activeOrders.SingleOrDefault(r => r.OrderId == tradeOrder.OrderId);
            if (itemToRemove == null)
            {
                return;
            }

            CloseOrder(priceTick, itemToRemove, activeOrders, closedOrders);
        }

        private void CloseOrder(PriceTick priceTick, ActiveOrder orderToClose, IList<ActiveOrder> activeOrders, IList<ClosedOrder> closedOrders)
        {
            var closePrice = orderToClose.BuySell == OrderBuySell.Buy ? priceTick.Bid : priceTick.Ask;
            var profit = (orderToClose.OpenPrice - closePrice) * orderToClose.Lots / priceTick.PointSize - PlatformParameters.Commission * orderToClose.Lots;
            var closedOrder = new ClosedOrder
            {
                OpenTime = orderToClose.OpenTime,
                OpenPrice = orderToClose.OpenPrice,
                CloseTime = priceTick.TickServerTime,
                ClosePrice = closePrice,
                Profit = orderToClose.BuySell == OrderBuySell.Sell ? profit : -profit,
                Lots = orderToClose.Lots,
                Instrument = orderToClose.Instrument,
                BuySell = orderToClose.BuySell,
            };

            activeOrders.Remove(orderToClose);
            closedOrders.Add(closedOrder);
        }

        private void CloseAllOrders(PriceTick priceTick, IList<ActiveOrder> activeOrders, IList<ClosedOrder> closedOrders)
        {
            while (activeOrders.Any())
            {
                var orderToClose = activeOrders[0];
                CloseOrder(priceTick, orderToClose, activeOrders, closedOrders);
            }
        }
    }
}
