﻿using System;
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
    public class GoldenGooseV1 : BaseExpert, IExpert
    {
        private int _atrBars;
        private bool _closeOnly;
        private int _takeProfit;

        private decimal _tunnelSizeLimit;
        private decimal _spreadLimit;
        private decimal _takeProfitNormalized;
        private decimal _atrCurrentValue;
        private decimal _tunnelSize;
        private decimal _tunnelMedian;
        private int _turnMulti;
        private int _tunnelBars;
        private double _calculatedSignalsLogSuspense = 60;
        private DateTime _lastCalculatedSignalsLogTimeStamp;
        private decimal _turnMultiDiscount;
        private decimal _turnMultiTunnelShift;
        private readonly IExpertParametersAdapter _expertParametersAdapter;
        private readonly IBarsHistoryAdapter _barsHistoryAdapter;

        private readonly Dictionary<string, IList<string>> _logInfo = new Dictionary<string, IList<string>>();

        public GoldenGooseV1(IExpertParametersAdapter expertParametersAdapter, IBarsHistoryAdapter barsHistoryAdapter)
        {
            _expertParametersAdapter = expertParametersAdapter;
            _barsHistoryAdapter = barsHistoryAdapter;
        }
        public GoldenGooseV1()
        {
            _expertParametersAdapter = new ExpertParametersAdapter();
            _barsHistoryAdapter = new BarsHistoryAdapter();
        }

        public override void InitializeParameters(IReadOnlyDictionary<string, object> parameters)
        {
            var parsedParams = _expertParametersAdapter.GetParameters<GgV1Parameters>(parameters);

            Risk = 0.0m;
            _calculatedSignalsLogSuspense = parsedParams.CalculatedSignalsLogSuspense == 0 ? 60 : parsedParams.CalculatedSignalsLogSuspense;
            Lots = parsedParams.Lots;
            Direction = parsedParams.Direction;
            Instrument = parsedParams.Instrument;
            _closeOnly = parsedParams.CloseOnly;
            _takeProfit = parsedParams.Profit;
            _atrBars = parsedParams.AtrBars;
            _turnMulti = parsedParams.TurnMulti == 0 ? 5 : parsedParams.TurnMulti;
            _tunnelBars = parsedParams.TunnelBars == 0 ? 21 : parsedParams.TunnelBars;
            _turnMultiDiscount = parsedParams.TurnMultiDiscount == 0.0m ? 0.8m : parsedParams.TurnMultiDiscount;
            _turnMultiTunnelShift = parsedParams.TunnelShift == 0.0m ? 0.2m : parsedParams.TunnelShift;
            _tunnelSizeLimit = parsedParams.TunnelSizeLimit == 0.0m ? 2.0m : parsedParams.TunnelSizeLimit;
            _spreadLimit = parsedParams.SpreadLimit == 0.0m ? 0.7m * _takeProfit : parsedParams.SpreadLimit;
        }

        public override void InitializeExpertStateIfNeeded(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            Initialized = true;
        }

        public override IList<TradingCommand> TradeEx(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders,
            Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            CalculateSignalsAsync(priceTick, historyBars);

            var message = "Failed to execute trade. GoldenGooseV1";
            message += $"{Environment.NewLine}ExpertId: {Id}";
            if (priceTick.Ask - priceTick.Bid > priceTick.PointSize * _spreadLimit)
            {
                Logger.Log.Error($"{message}{Environment.NewLine}Spread limit: {priceTick.PointSize * _spreadLimit}{Environment.NewLine}Spread: {priceTick.Ask - priceTick.Bid}");
                return null;
            }

            IList<TradingCommand> commands = null;

            var turnOrderLots = ShouldOpenTurnOrder(priceTick, activeOrders);
            if (turnOrderLots > 0)
            {
                if (Online)
                {
                    var logs = new List<string>
                    {
                        $"Amount: {turnOrderLots}"
                    };
                    BuildLogInfo("Trading. GoldenGooseV1. Open turn order", logs);
                    LogCalculatedSignals(priceTick, true);
                }
                commands = TradeTurnOrder(priceTick, turnOrderLots);
                if (commands != null && commands.Count > 0)
                {
                    return commands;
                }
            }

            if (activeOrders != null && activeOrders.Count == 0)
            {
                if (!Online || !_closeOnly)
                {
                    commands = TradeOpen(priceTick, Lots);
                }
                if (Online)
                {
                    BuildLogInfo("Trading. GoldenGooseV1. Open first order", new List<string>());
                    LogCalculatedSignals(priceTick, true);
                }
                return commands;
            }

            commands = TradeClose(priceTick, activeOrders);
            if (commands == null || commands.Count == 0) return null;
            if (Online)
            {
                BuildLogInfo("Trading. GoldenGooseV1. Close order", new List<string>());
                LogCalculatedSignals(priceTick, true);
            }
            return commands;
        }

        private IList<TradingCommand> TradeTurnOrder(PriceTick priceTick, int turnOrderLots)
        {
            var commands = new List<TradingCommand>();
            commands.AddRange(TradeOpen(priceTick, turnOrderLots));
            return commands;
        }

        private int ShouldOpenTurnOrder(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            if (activeOrders.Count == 0)
            {
                return 0;
            }

            var orders = activeOrders.OrderBy(x => x.OpenTime).ToList();
            var lastOrder = orders.Last();
            var priceDiff = 0.0m;
            var openPriceDiff = 0.0m;
            var priceMedianDistance = 0.0m;

            switch (Direction)
            {
                case OrderBuySell.Buy:
                    priceDiff = priceTick.Bid - lastOrder.OpenPrice;
                    priceMedianDistance = _tunnelMedian - _tunnelSize * _turnMultiTunnelShift - priceTick.Bid;
                    openPriceDiff = lastOrder.OpenPrice - priceTick.Ask;
                    break;
                case OrderBuySell.Sell:
                    priceDiff = lastOrder.OpenPrice - priceTick.Ask;
                    priceMedianDistance = priceTick.Ask - _tunnelMedian - _tunnelSize * _turnMultiTunnelShift;
                    openPriceDiff = priceTick.Bid - lastOrder.OpenPrice;
                    break;
            }

            var logs = new List<string>
            {
                $"Price median distance: {priceMedianDistance}",
                $"Price diff: {priceDiff}",
                $"Open price diff: {openPriceDiff}",
                $"Multiplied take profit: {_takeProfitNormalized * _turnMulti}",
                $"Last order Id: {lastOrder.OrderId}",
                $"First condition: {_tunnelSize < _takeProfitNormalized * _tunnelSizeLimit}",
                $"Second condition: {_tunnelMedian > 0.0m && priceMedianDistance < 0}",
                $"Third condition: {-priceDiff > _takeProfitNormalized * _turnMulti}",
                $"Lots: {lastOrder.Lots * (int)(-_turnMultiDiscount * priceDiff / _takeProfitNormalized)}"
            };
            BuildLogInfo("Trading. GoldenGooseV1. Should open turn order", logs);
            LogCalculatedSignals(priceTick);

            if (openPriceDiff <= 0.0m)
            {
                return 0;
            }

            if (_tunnelMedian > 0.0m && priceMedianDistance < 0)
            {
                return 0;
            }

            if (_tunnelSize > 0.0m && _tunnelSize < _takeProfitNormalized * _tunnelSizeLimit
                && -priceDiff > _takeProfitNormalized * _turnMulti)
            {
                var resultLots = lastOrder.Lots * (int)(-_turnMultiDiscount * priceDiff / _takeProfitNormalized);
                return resultLots;
            }
            return 0;
        }

        private async Task CalculateSignalsAsync(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            if (historyBars == null)
            {
                return;
            }

            await CalculateNormalizedTakeProfit(priceTick, historyBars).ConfigureAwait(false);
            await CalculateTunnelSize(priceTick, historyBars).ConfigureAwait(false);
            BuildCommonLogInfo(priceTick, historyBars);
            LogCalculatedSignals(priceTick);
        }

        private async Task CalculateTunnelSize(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            var historyKey = new PriceBarsHistoryKey
            {
                Instrument = priceTick.Instrument,
                BarsCount = _tunnelBars,
                TimeFrame = TimeFrame.m1
            };

            var m1Key = await _barsHistoryAdapter.GetPriceBarKey(historyKey).ConfigureAwait(false);
            if (!historyBars.ContainsKey(m1Key)) return;

            var minTunnel = decimal.MaxValue;
            var maxTunnel = decimal.MinValue;
            var m1HistoryBars = historyBars[m1Key].Skip(1);
            if (m1HistoryBars.Count() < _tunnelBars-1)
            {
                return;
            }

            foreach (var item in m1HistoryBars)
            {
                if (item.BidHigh > maxTunnel)
                {
                    maxTunnel = item.BidHigh;
                }
                if (item.BidLow < minTunnel)
                {
                    minTunnel = item.BidLow;
                }
            }
            _tunnelSize = maxTunnel - minTunnel;
            _tunnelMedian = (maxTunnel + minTunnel) / 2;
        }

        private async Task CalculateNormalizedTakeProfit(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            _takeProfitNormalized = priceTick.PointSize * _takeProfit;
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
                    commands.AddRange(TradeOpen(priceTick, Lots));
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
                var lastOrder = orders.Last();
                switch (lastOrder.BuySell)
                {
                    case OrderBuySell.Sell:
                        if (lastOrder.OpenPrice - priceTick.Ask < 0.0m)
                        {
                            return null;
                        }
                        break;
                    case OrderBuySell.Buy:
                        if (priceTick.Bid - lastOrder.OpenPrice < 0.0m)
                        {
                            return null;
                        }
                        break;
                }
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
                    commands.AddRange(TradeOpen(priceTick, Lots));
                }

                return commands;
            }

            return new List<TradingCommand>();
        }

        private IList<TradingCommand> TradeOpen(PriceTick priceTick, int lots)
        {
            var commands = new List<TradingCommand> {
                new TradingCommand
                {
                    Lots = lots,
                    BuySell = Direction,
                    OpenClose = OrderOpenClose.Open,
                    Instrument = priceTick.Instrument,
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
                $"ATR: {_atrCurrentValue}",
                $"Tunnel size: {_tunnelSize}",
                $"Tunnel median: {_tunnelMedian}"
            };

            if (historyBars == null)
            {
                return;
            }

            foreach (var key in historyBars.Keys)
            {
                logs.Add($"Key in history: {key}");
            }

            const string calculateSignalsKey = "Trading. GoldenGooseV1. Calculate signals";
            if (_logInfo.ContainsKey(calculateSignalsKey))
            {
                _logInfo.Remove(calculateSignalsKey);
            }

            _logInfo.Add(calculateSignalsKey, logs);

        }
    }
}
