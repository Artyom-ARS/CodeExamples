using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Enums;
using Common.Logging;
using Common.Models;
using FxConnect.Providers;

namespace OnlinePlatform.Providers
{
    public class TradingCommandProvider : ITradingCommandProvider
    {
        private readonly IFxconnectProvider _fxConnectProvider;

        public TradingCommandProvider(IFxconnectProvider fxConnectProvider)
        {
            _fxConnectProvider = fxConnectProvider;
        }

        public async Task<bool> ProcessTradingCommand(string accountId, string expertId, IList<TradingCommand> commands, List<ActiveOrder> activeOrders)
        {
            if (commands == null || commands.Count == 0)
            {
                return false;
            }

            var orderExecuted = true;

            Parallel.ForEach(commands.OrderBy(x => x?.OpenClose), async (tradeOrder) =>
            {
                if (tradeOrder == null)
                {
                    return;
                }

                await LogTradingCommand(expertId, tradeOrder).ConfigureAwait(false);

                switch (tradeOrder.OpenClose)
                {
                    case OrderOpenClose.Open:
                        var order = await _fxConnectProvider.OpenOrder(accountId, tradeOrder.Instrument, tradeOrder.Lots, tradeOrder.BuySell, expertId).ConfigureAwait(false);
                        orderExecuted = orderExecuted && (order != null);
                        break;
                    case OrderOpenClose.Close:
                        var orderClosed = await _fxConnectProvider.CloseOrder(tradeOrder.OrderId, tradeOrder.BuySell, accountId, tradeOrder.Instrument, tradeOrder.Lots, expertId).ConfigureAwait(false);
                        orderExecuted = orderExecuted && orderClosed;
                        break;
                    case OrderOpenClose.Update:
                        var orderUpdated = await _fxConnectProvider.UpdateOrder(tradeOrder.OrderId, tradeOrder.BuySell, accountId, tradeOrder.Instrument, tradeOrder.Lots,
                            tradeOrder.StopLoss, tradeOrder.TakeProfit, expertId).ConfigureAwait(false);
                        orderExecuted = orderExecuted && orderUpdated;
                        break;
                }
            });

            return orderExecuted;
        }

        private async Task LogTradingCommand(string expertId, TradingCommand tradeOrder)
        {
            var orderStatus = tradeOrder.OpenClose.ToString();
            var log = $"Trading. {orderStatus} order";
            log += $"{Environment.NewLine}Oder Id: {tradeOrder.OrderId}";
            log += $"{Environment.NewLine}Amount: {tradeOrder.Lots}";
            log += $"{Environment.NewLine}Direction: {(tradeOrder.BuySell == OrderBuySell.Buy ? 'B' : 'S')}";
            log += $"{Environment.NewLine}Instrument: {tradeOrder.Instrument}";
            log += $"{Environment.NewLine}Tag: {expertId}";
            Logger.Log.Info(log);
        }
    }
}
