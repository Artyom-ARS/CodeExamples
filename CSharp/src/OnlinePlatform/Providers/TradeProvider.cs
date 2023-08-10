using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Adapters;
using Common.Enums;
using Common.Experts;
using Common.Logging;
using Common.Models;
using Common.Providers;
using ExpertService.Models;
using FxConnect.Models;
using FxConnect.Providers;
using OnlinePlatform.Models;
using PlatformCommon.Providers;

namespace OnlinePlatform.Providers
{
    public class TradeProvider : ITradeProvider
    {
        private readonly IFxconnectProvider _fxConnectProvider;
        private readonly ITradingCommandProvider _tradingCommandProvider;
        private readonly IMarketStatusProvider _marketStatusProvider;
        private readonly IHistoryPricesCacheProvider _historyPricesCacheProvider;
        private readonly IBarsHistoryAdapter _barsHistoryAdapter;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ILockProvider _lockProvider;
        private readonly IPriceUpdateSuspenseProvider _priceUpdateSuspenseProvider;
        private IList<ActiveOrder> _activeOrders;
        private IList<ClosedOrder> _closedOrders;
        private DTOAccount _account;
        private IList<IExpert> _experts;
        private Dictionary<string, PriceTick> _lastPrices;
        private DateTime _printStatusTmstmp;
        private List<PriceBarsHistoryKey> _historyBarsConfigurations;
        private int _printStatusSuspenseSeconds;
        private int _platformStartHandleUpdatesDelaySeconds;
        private Dictionary<string, PriceUpdateSuspenseParameters> _priceUpdateSuspense;

        public TradeProvider(IFxconnectProvider fxConnectProvider, ITradingCommandProvider tradingCommandProvider,
            IMarketStatusProvider marketStatusProvider, IHistoryPricesCacheProvider historyPricesCacheProvider,
            IBarsHistoryAdapter barsHistoryAdapter, IConfigurationProvider configurationProvider, ILockProvider lockProvider,
            IPriceUpdateSuspenseProvider priceUpdateSuspenseProvider)
        {
            _fxConnectProvider = fxConnectProvider;
            _tradingCommandProvider = tradingCommandProvider;
            _marketStatusProvider = marketStatusProvider;
            _historyPricesCacheProvider = historyPricesCacheProvider;
            _barsHistoryAdapter = barsHistoryAdapter;
            _configurationProvider = configurationProvider;
            _lockProvider = lockProvider;
            _priceUpdateSuspenseProvider = priceUpdateSuspenseProvider;
            _closedOrders = new List<ClosedOrder>();
            _activeOrders = new List<ActiveOrder>();
            _lastPrices = new Dictionary<string, PriceTick>();
        }

        public async Task HandleUpdates()
        {
            Thread.Sleep(_platformStartHandleUpdatesDelaySeconds*1000);

            _fxConnectProvider.PriceUpdate += HandlePriceUpdates;
            _fxConnectProvider.OrderUpdate += HandleOrderUpdates;
            await _fxConnectProvider.StartWatchingPriceUpdates().ConfigureAwait(false);

            await _marketStatusProvider.PrintStatus(_account, _activeOrders, _closedOrders, _lastPrices).ConfigureAwait(false);
        }

        public async Task InitializeExperts(IList<IExpert> experts, ExpertParametersForTrade expertParameters)
        {
            _experts = experts;
            foreach (var expert in _experts)
            {
                var parameters = expertParameters.Experts.FirstOrDefault(x => (string)x["Id"] == expert.Id);
                if (parameters == null)
                {
                    continue;
                }

                if (parameters.ContainsKey("disabled") && (bool)parameters["disabled"])
                {
                    continue;
                }
                expert.InitializeParameters(parameters, true);
            }

        }

        public async Task LoginAndSubscribe(OnlineConfigurationParameters configuration)
        {
            Logger.Log.Info("Logging in...");
            _account = await _fxConnectProvider.Login().ConfigureAwait(false);
            if (_account == null)
            {
                throw new Exception("The account is not valid");
            }
            _activeOrders = await _fxConnectProvider.GetActiveOrders(_account.Id).ConfigureAwait(false);

            await _fxConnectProvider.Subscribe(configuration.Instruments).ConfigureAwait(false);

            await _marketStatusProvider.PrintStatus(_account, _activeOrders, _closedOrders, _lastPrices).ConfigureAwait(false);
        }

        public async Task Stop()
        {
            await _fxConnectProvider.Logout().ConfigureAwait(false);
        }

        public async void HandleOrderUpdates(object sender, DTOOrderUpdate orderUpdate)
        {
            await LogOrderUpdate(orderUpdate).ConfigureAwait(false);
            ActiveOrder order;
            switch (orderUpdate.Status)
            {
                case OrderOpenClose.Close:
                    order = _activeOrders.FirstOrDefault(x => x.OrderId == orderUpdate.OrderId);
                    if (order == null)
                    {
                        return;
                    }
                    _activeOrders.Remove(order);
                    var closedOrder = new ClosedOrder
                    {
                        OrderId = order.OrderId,
                        BuySell = order.BuySell,
                        Lots = order.Lots,
                        OpenPrice = order.OpenPrice,
                        OpenTime = order.OpenTime,
                        Instrument = order.Instrument,
                        Tag = order.Tag,
                        ClosePrice = (decimal)orderUpdate.Price,
                        CloseTime = orderUpdate.Time
                    };
                    _closedOrders.Add(closedOrder);
                    break;
                case OrderOpenClose.Open:
                    order = _activeOrders.FirstOrDefault(x => x.OrderId == orderUpdate.OrderId);
                    if (order == null)
                    {
                        order = new ActiveOrder
                        {
                            OrderId = orderUpdate.OrderId,
                            BuySell = orderUpdate.Direction,
                            Lots = orderUpdate.Amount,
                            OpenPrice = (decimal)orderUpdate.Price,
                            OpenTime = orderUpdate.Time,
                            Instrument = orderUpdate.Instrument,
                            Tag = orderUpdate.Tag
                        };
                        _activeOrders.Add(order);
                    }
                    break;
                case OrderOpenClose.Update:
                    break;
                case OrderOpenClose.UpdateSL:
                    order = _activeOrders.FirstOrDefault(x => x.OrderId == orderUpdate.OrderId);
                    if (order == null)
                    {
                        return;
                    }
                    order.StopLossPrice = (decimal) orderUpdate.Price;
                    break;
                case OrderOpenClose.UpdateTP:
                    order = _activeOrders.FirstOrDefault(x => x.OrderId == orderUpdate.OrderId);
                    if (order == null)
                    {
                        return;
                    }
                    order.TakeProfitPrice = (decimal) orderUpdate.Price;
                    break;
            }

            await _marketStatusProvider.PrintStatus(_account, _activeOrders, _closedOrders, _lastPrices).ConfigureAwait(false);
        }

        private async Task LogOrderUpdate(DTOOrderUpdate orderUpdate)
        {
            var orderStatus = orderUpdate?.Status.ToString();
            var log = $"Market. {orderStatus} order";
            log += $"{Environment.NewLine}Oder Id: {orderUpdate.OrderId}";
            log += $"{Environment.NewLine}Amount: {orderUpdate.Amount}";
            log += $"{Environment.NewLine}Direction: {(orderUpdate.Direction == OrderBuySell.Buy ? 'B' : 'S')}";
            log += $"{Environment.NewLine}Instrument: {orderUpdate.Instrument}";
            log += $"{Environment.NewLine}Price: {(decimal)orderUpdate.Price}";
            log += $"{Environment.NewLine}Time: {orderUpdate.Time}";
            log += $"{Environment.NewLine}Tag: {orderUpdate.Tag}";
            Logger.Log.Info(log);
        }

        public async void HandlePriceUpdates(object sender, DTOPriceUpdate priceUpdate)
        {
            if (await _priceUpdateSuspenseProvider.ShouldWait(_priceUpdateSuspense, priceUpdate).ConfigureAwait(false))
            {
                return;
            }

            var instrument = await _fxConnectProvider.GetInstrument(priceUpdate.Instrument).ConfigureAwait(false);
            if (instrument == null)
            {
                return;
            }

            var priceTick = new PriceTick
            {
                Ask = (decimal)priceUpdate.Ask,
                Instrument = priceUpdate.Instrument,
                PointSize = instrument.PointSize,
                TickServerTime = priceUpdate.ServerTime,
                Bid = (decimal)priceUpdate.Bid
            };

            if (_lastPrices.ContainsKey(priceUpdate.Instrument))
            {
                _lastPrices[priceUpdate.Instrument] = priceTick;
            }
            else
            {
                _lastPrices.Add(priceUpdate.Instrument, priceTick);
            }

            if (priceTick.TickServerTime > _printStatusTmstmp.AddSeconds(_printStatusSuspenseSeconds))
            {
                await _marketStatusProvider.PrintStatus(_account, _activeOrders, _closedOrders, _lastPrices).ConfigureAwait(false);
                _printStatusTmstmp = priceTick.TickServerTime;
            }

            var now = priceUpdate.ServerTime;
            var ao = _activeOrders.ToList();

            Parallel.ForEach(_experts, async (expert) =>
            {
                if (!await _lockProvider.Lock(expert.Id).ConfigureAwait(false))
                {
                    Logger.Log.Error($"Skip trading. ExpertId: {expert.Id}");
                    return;
                }

                var prices = await GetHistoryPrices(priceUpdate, _historyBarsConfigurations, now).ConfigureAwait(false);
                var (result, _) = expert.Trade(priceTick, ao.Where(x => x.Tag == expert.Id).ToList(), prices);

                await _tradingCommandProvider.ProcessTradingCommand(_account.Id, expert.Id, result, ao.Where(x => x.Tag == expert.Id).ToList()).ConfigureAwait(false);

                await _lockProvider.UnLock(expert.Id).ConfigureAwait(false);
                if (result != null && result.Any())
                {
                    await _lockProvider.Lock(expert.Id).ConfigureAwait(false);
                }
            });
        }

        public async Task InitializeHistoryPrices()
        {
            var historyConfiguration = _configurationProvider.GetConfiguration<HistoryConfigurationParameters>();
            _historyBarsConfigurations = historyConfiguration.History;
            var platformConfiguration = _configurationProvider.GetConfiguration<OnlineConfigurationParameters>();
            _printStatusSuspenseSeconds = platformConfiguration.PrintStatusSuspenseSeconds == 0 ? 5 : platformConfiguration.PrintStatusSuspenseSeconds;
            _platformStartHandleUpdatesDelaySeconds = platformConfiguration.PlatformStartHandleUpdatesDelaySeconds == 0 ? 10 : platformConfiguration.PlatformStartHandleUpdatesDelaySeconds;
            _priceUpdateSuspense = platformConfiguration.PriceUpdateSuspense;
        }

        private async Task<Dictionary<string, IList<PriceBarForStore>>> GetHistoryPrices(DTOPriceUpdate priceUpdate, IReadOnlyCollection<PriceBarsHistoryKey> listInstances, DateTime now)
        {
            var prices = new Dictionary<string, IList<PriceBarForStore>>();
            foreach (var priceBars in listInstances)
            {
                var match = listInstances.FirstOrDefault(key =>
                    key.Instrument.Contains(priceUpdate.Instrument));
                if (match == null) continue;

                var instrument = priceBars.Instrument;
                var tf = priceBars.TimeFrame;
                var barsCount = priceBars.BarsCount;

                var historyKey = await _barsHistoryAdapter.GetInstrumentHistoryPricesKey(instrument, tf).ConfigureAwait(false);

                var bars = await _historyPricesCacheProvider.GetPricesFromCache(historyKey, tf, barsCount, now).ConfigureAwait(false);
                if (bars.Count == 0)
                {
                    continue;
                }

                prices.Add(historyKey + "-" + barsCount, bars);
            }

            return prices;
        }
    }
}
