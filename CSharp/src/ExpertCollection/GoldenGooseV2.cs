using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Adapters;
using Common.Enums;
using Common.Experts;
using Common.Logging;
using Common.Models;

namespace ExpertCollection
{
    public class GoldenGooseV2 : BaseExpert, IExpert
    {
        private int _atrBars;
        private bool _closeOnly;
        private int _takeProfit;

        private decimal _spreadLimit;
        private decimal _takeProfitNormalized;
        private decimal _hedgeStopLossNormalized;
        private decimal _atrCurrentValue;
        private int _hedgeMulti;
        private int _hedgeBars;
        private decimal _hedgeTPShift;
        private decimal _hedgeOCShift;
        private decimal _hedgeOrderMulti;
        private int _hedgeStopLoss;
        private double _calculatedSignalsLogSuspense = 60;
        private DateTime _lastCalculatedSignalsLogTimeStamp;
        private readonly IExpertParametersAdapter _expertParametersAdapter;
        private readonly IBarsHistoryAdapter _barsHistoryAdapter;

        private readonly Dictionary<string, IList<string>> _logInfo = new Dictionary<string, IList<string>>();

        public GoldenGooseV2(IExpertParametersAdapter expertParametersAdapter, IBarsHistoryAdapter barsHistoryAdapter)
        {
            _expertParametersAdapter = expertParametersAdapter;
            _barsHistoryAdapter = barsHistoryAdapter;
        }
        public GoldenGooseV2()
        {
            _expertParametersAdapter = new ExpertParametersAdapter();
            _barsHistoryAdapter = new BarsHistoryAdapter();
        }

        public override void InitializeParameters(IReadOnlyDictionary<string, object> parameters)
        {
            var parsedParams = _expertParametersAdapter.GetParameters<GgV2Parameters>(parameters);

            Risk = 0.0m;
            _calculatedSignalsLogSuspense = parsedParams.CalculatedSignalsLogSuspense == 0 ? 60 : parsedParams.CalculatedSignalsLogSuspense;
            Lots = parsedParams.Lots;
            Direction = parsedParams.Direction;
            Instrument = parsedParams.Instrument;
            _closeOnly = parsedParams.CloseOnly;
            _takeProfit = parsedParams.Profit;
            _atrBars = parsedParams.AtrBars;
            _hedgeMulti = parsedParams.HedgeMulti == 0 ? 5 : parsedParams.HedgeMulti;
            _hedgeBars = parsedParams.HedgeBars == 0 ? 4 : parsedParams.HedgeBars;
            _spreadLimit = parsedParams.SpreadLimit == 0.0m ? 0.7m * _takeProfit : parsedParams.SpreadLimit;
            _hedgeTPShift = parsedParams.HedgeTPShift == 0.0m ? 0.2m : parsedParams.HedgeTPShift;
            _hedgeStopLoss = parsedParams.HedgeStopLoss == 0 ? 8 : parsedParams.HedgeStopLoss;
            _hedgeOCShift = parsedParams.HedgeOCShift == 0.0m ? 0.3m : parsedParams.HedgeOCShift;
            _hedgeOrderMulti = parsedParams.HedgeOrderMulti == 0.0m ? 1.0m : parsedParams.HedgeOrderMulti;
        }

        public override void InitializeExpertStateIfNeeded(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            Initialized = true;
        }

        public override IList<TradingCommand> TradeEx(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders,
            Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            CalculateSignalsAsync(priceTick, historyBars).Wait();

            var message = "Failed to execute trade. GoldenGooseV2";
            message += $"{Environment.NewLine}ExpertId: {Id}";
            message += $"{Environment.NewLine}Server time: {priceTick.TickServerTime}";
            if (priceTick.Ask - priceTick.Bid > priceTick.PointSize * _spreadLimit)
            {
                Logger.Log.Error($"{message}{Environment.NewLine}Spread limit: {priceTick.PointSize * _spreadLimit}{Environment.NewLine}Spread: {priceTick.Ask - priceTick.Bid}");
                return null;
            }

            IList<TradingCommand> commands = null;

            var hedgeOrderLots = ShouldOpenHedgeOrder(priceTick, activeOrders, historyBars);
            if (hedgeOrderLots > 0)
            {
                if (Online)
                {
                    var logs = new List<string>
                    {
                        $"Amount: {hedgeOrderLots}"
                    };
                    BuildLogInfo("Trading. GoldenGooseV2. Open hedge order", logs);
                    LogCalculatedSignals(priceTick, true);
                }
                commands = TradeHedgeOrder(priceTick, hedgeOrderLots);
                if (commands != null && commands.Count > 0)
                {
                    return commands;
                }
            }

            if (activeOrders != null && activeOrders.Count == 0)
            {
                if (!Online || !_closeOnly)
                {
                    commands = TradeOpen(Lots);
                }
                if (Online)
                {
                    BuildLogInfo("Trading. GoldenGooseV2. Open first order", new List<string>());
                    LogCalculatedSignals(priceTick, true);
                }
                return commands;
            }

            commands = TradeClose(priceTick, activeOrders);
            if (commands == null || commands.Count == 0) return null;
            if (Online)
            {
                BuildLogInfo("Trading. GoldenGooseV2. Close order", new List<string>());
                LogCalculatedSignals(priceTick, true);
            }
            return commands;
        }

        private IList<TradingCommand> TradeHedgeOrder(PriceTick priceTick, int hedgeOrderLots)
        {
            var commands = new List<TradingCommand>();
            commands.AddRange(TradeOpen(hedgeOrderLots, OppositeDirection, _hedgeStopLossNormalized));
            return commands;
        }

        private int ShouldOpenHedgeOrder(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            if (activeOrders.Count < 1)
            {
                return 0;
            }

            var historyKey = new PriceBarsHistoryKey
            {
                Instrument = priceTick.Instrument,
                BarsCount = _hedgeBars,
                TimeFrame = TimeFrame.m1
            };

            var getPriceBarKeyTask = _barsHistoryAdapter.GetPriceBarKey(historyKey);
            getPriceBarKeyTask.Wait();
            var m1Key = getPriceBarKeyTask.Result;
            if (!historyBars.ContainsKey(m1Key))
            {
                return 0;
            }

            var m1HistoryBars = historyBars[m1Key];
            if (m1HistoryBars.Count < _hedgeBars)
            {
                return 0;
            }

            if (activeOrders.Any(x => x.BuySell == OppositeDirection)) 
            {
                return 0;
            }

            var priceDiff = 0.0m;
            var openPriceDiff = 0.0m;
            var currentBar = m1HistoryBars[0];
            var beforeCurrentBar = m1HistoryBars[1];
            var openCloseBeforeCurrent = 0.0m;
            var orders = activeOrders.OrderBy(x => x.OpenTime).ToList();
            var firstOrder = orders.First();
            switch (Direction)
            {
                case OrderBuySell.Buy:
                    priceDiff = priceTick.Bid - firstOrder.OpenPrice;
                    openPriceDiff = currentBar.BidOpen - priceTick.Bid;
                    openCloseBeforeCurrent = beforeCurrentBar.BidClose - beforeCurrentBar.BidOpen;
                    break;
                case OrderBuySell.Sell:
                    priceDiff = firstOrder.OpenPrice - priceTick.Ask;
                    openPriceDiff = priceTick.Ask - currentBar.BidOpen - currentBar.Spread;
                    openCloseBeforeCurrent = beforeCurrentBar.BidOpen - beforeCurrentBar.BidClose;
                    break;
            }

            var logs = new List<string>
            {
                $"Price diff: {priceDiff}",
                $"Open price diff: {openPriceDiff}",
                $"Multiplied hedge take profit: {_takeProfitNormalized * _hedgeMulti}",
                $"First order Id: {firstOrder.OrderId}",
                $"First condition: {priceDiff > -_takeProfitNormalized * _hedgeMulti}",
                $"Second condition: {openPriceDiff <= _takeProfitNormalized * _hedgeTPShift}",
                $"Third condition: {openPriceDiff <= openCloseBeforeCurrent * _hedgeOCShift}",
            };
            BuildLogInfo("Trading. GoldenGooseV2. Shouldn't open hedge order", logs);
            LogCalculatedSignals(priceTick);

            if (priceDiff > -_takeProfitNormalized * _hedgeMulti
                || openPriceDiff <= _takeProfitNormalized * _hedgeTPShift
                || openPriceDiff <= openCloseBeforeCurrent * _hedgeOCShift)
            {
                return 0;
            }

            var inTrend = true;
            foreach (var bar in m1HistoryBars.Skip(1))
            {
                if (!inTrend)
                {
                    break;
                }
                var openClose = 0.0m; 
                switch (Direction)
                {
                    case OrderBuySell.Sell:
                        openClose = bar.BidClose - bar.BidOpen;
                        break;
                    case OrderBuySell.Buy:
                        openClose = bar.BidOpen - bar.BidClose;
                        break;
                }
                if (openClose <= 0.0m)
                {
                    inTrend = false;
                }
            }
            if (!inTrend)
            {
                return 0;
            }

            var profit = 0.0m;
            foreach (var order in orders)
            {
                if (order.BuySell != Direction)
                {
                    continue;
                }
                switch (Direction)
                {
                    case OrderBuySell.Sell:
                        profit += order.Lots * (order.OpenPrice - priceTick.Ask - _takeProfitNormalized * _hedgeOrderMulti);
                        break;
                    case OrderBuySell.Buy:
                        profit += order.Lots * (priceTick.Bid - order.OpenPrice - _takeProfitNormalized * _hedgeOrderMulti);
                        break;
                }
            }

            logs = new List<string>
            {
                $"Total profit: {profit}",
                $"Lots: {-profit / (_takeProfitNormalized * _hedgeOrderMulti)}",
            };
            BuildLogInfo("Trading. GoldenGooseV2. Should open hedge order", logs);

            if (profit > 0.0m)
            {
                return 0;
            }

            var hedgeLots = -profit / (_takeProfitNormalized * _hedgeOrderMulti);

            return (int)hedgeLots + 1;
        }

        private async Task CalculateSignalsAsync(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            if (historyBars == null)
            {
                return;
            }

            await CalculateNormalized(priceTick, historyBars).ConfigureAwait(false);
            BuildCommonLogInfo(priceTick, historyBars);
            LogCalculatedSignals(priceTick);
        }

        private async Task CalculateNormalized(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            _takeProfitNormalized = priceTick.PointSize * _takeProfit;
            _hedgeStopLossNormalized = priceTick.PointSize * _hedgeStopLoss;
            var historyKey = new PriceBarsHistoryKey
            {
                Instrument = priceTick.Instrument,
                BarsCount = _atrBars,
                TimeFrame = TimeFrame.d1
            };

            var d1Key = await _barsHistoryAdapter.GetPriceBarKey(historyKey).ConfigureAwait(false);

            if (!historyBars.ContainsKey(d1Key)) return;

            var d1HistoryBars = historyBars[d1Key].Skip(1);
            if (d1HistoryBars.Count() < _atrBars - 1)
            {
                return;
            }
            var averageAtr = d1HistoryBars.Sum(item => item.BidHigh - item.BidLow);

            averageAtr /= d1HistoryBars.Count();
            _atrCurrentValue = Math.Round(averageAtr / priceTick.Bid * 100, 2);
            _takeProfitNormalized = _takeProfit * _atrCurrentValue * priceTick.PointSize;
            _hedgeStopLossNormalized = _hedgeStopLoss * _atrCurrentValue * priceTick.PointSize;
        }

        private IList<TradingCommand> TradeClose(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            var closeCommand1 = CloseOrderIfNeeded(priceTick, activeOrders);
            var closeCommand2 = CloseOrdersIfNeeded(priceTick, activeOrders);
            if (closeCommand1 == null && closeCommand2 == null)
            {
                return null;
            }

            var commands = new List<TradingCommand>();
            if (closeCommand1 != null)
            {
                commands.AddRange(closeCommand1);
            }
            if (closeCommand2 != null)
            {
                commands.AddRange(closeCommand2);
            }

            return commands;
        }

        private IList<TradingCommand> CloseOrderIfNeeded(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            if (activeOrders.Count == 1)
            {
                var order = activeOrders[0];

                var profit = 0.0m;
                switch (order.BuySell)
                {
                    case OrderBuySell.Sell:
                        profit += order.Lots * (order.OpenPrice - priceTick.Ask);
                        break;
                    case OrderBuySell.Buy:
                        profit += order.Lots * (priceTick.Bid - order.OpenPrice);
                        break;
                }

                if (profit / order.Lots < _takeProfitNormalized)
                {
                    return null;
                }

                var commands = new List<TradingCommand>();
                var closeCommand = new TradingCommand
                {
                    OpenClose = OrderOpenClose.Close,
                    OrderId = order.OrderId,
                    Instrument = order.Instrument,
                    Lots = order.Lots,
                    BuySell = order.BuySell
                };
                commands.Add(closeCommand);

                if (!Online || !_closeOnly)
                {
                    commands.AddRange(TradeOpen(Lots));
                }

                return commands;
            }

            return new List<TradingCommand>();
        }

        private IList<TradingCommand> CloseOrdersIfNeeded(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            if (activeOrders.Count > 1)
            {
                var commands = new List<TradingCommand>();
                var profit = 0.0m;
                var orders = activeOrders.OrderBy(x => x.OpenTime).ToList();
                var firstOrder = orders.First();
                foreach (var order in orders)
                {
                    var closeCommand = new TradingCommand
                    {
                        OpenClose = OrderOpenClose.Close,
                        OrderId = order.OrderId,
                        Instrument = order.Instrument,
                        Lots = order.Lots,
                        BuySell = order.BuySell
                    };
                    commands.Add(closeCommand);
                    switch (order.BuySell)
                    {
                        case OrderBuySell.Sell:
                            profit += order.Lots * (order.OpenPrice - priceTick.Ask);
                            break;
                        case OrderBuySell.Buy:
                            profit += order.Lots * (priceTick.Bid - order.OpenPrice);
                            break;
                    }
                }

                if (profit / firstOrder.Lots < _takeProfitNormalized)
                {
                    return null;
                }

                var risk = orders.Count - 1;
                Risk = risk > Risk ? risk : Risk;

                if (!Online || !_closeOnly)
                {
                    commands.AddRange(TradeOpen(Lots));
                }

                return commands;
            }

            return new List<TradingCommand>();
        }

        private IList<TradingCommand> TradeOpen(int lots)
        {
            var commands = TradeOpen(lots, Direction, 0.0m);
            return commands;
        }

        private IList<TradingCommand> TradeOpen(int lots, OrderBuySell direction, decimal stopLoss)
        {
            var commands = new List<TradingCommand> {
                new TradingCommand
                {
                    Lots = lots,
                    BuySell = direction,
                    OpenClose = OrderOpenClose.Open,
                    Instrument = Instrument,
                    StopLoss = stopLoss,
                }
            };
            return commands;
        }

        private void LogCalculatedSignals(PriceTick priceTick, bool force = false)
        {
            if (!Online) return;

            var current = priceTick.TickServerTime;

            if (!force && current < _lastCalculatedSignalsLogTimeStamp.AddSeconds(_calculatedSignalsLogSuspense))
            {
                return;
            }

            var logStr = string.Empty;
            foreach (var item in _logInfo)
            {
                if (!string.IsNullOrEmpty(logStr))
                {
                    logStr += Environment.NewLine;
                }
                logStr += item.Key;
                foreach (var log in item.Value)
                {
                    logStr += $"{Environment.NewLine}{log}";
                }
            }
            Logger.Log.Info(logStr);

            _logInfo.Clear();
            _lastCalculatedSignalsLogTimeStamp = current;
        }

        private void BuildLogInfo(string calculateSignalsKey, IList<string> logs)
        {
            if (_logInfo.ContainsKey(calculateSignalsKey))
            {
                _logInfo.Remove(calculateSignalsKey);
            }

            _logInfo.Add(calculateSignalsKey, logs);
        }

        private void BuildCommonLogInfo(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            var logs = new List<string>
            {
                $"Expert Id: {Id}",
                $"Instrument: {Instrument}",
                $"Point size: {priceTick.PointSize}",
                $"Take profit normalized: {_takeProfitNormalized}",
                $"Hedge stop loss normalized: {_hedgeStopLossNormalized}",
                $"ATR: {_atrCurrentValue}",
            };

            if (historyBars == null)
            {
                return;
            }

            foreach (var key in historyBars.Keys)
            {
                logs.Add($"Key in history: {key}");
            }

            const string calculateSignalsKey = "Trading. GoldenGooseV2. Calculate signals";
            if (_logInfo.ContainsKey(calculateSignalsKey))
            {
                _logInfo.Remove(calculateSignalsKey);
            }

            _logInfo.Add(calculateSignalsKey, logs);

        }
    }
}
