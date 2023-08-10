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
    public class GoldenGooseV6 : BaseExpert, IExpert, IExpertWithSignals
    {
        private bool _isReadyToFreeze;
        private DateTime _lastCalculatedSignalsLogTimeStamp;

        private int _atrBars;
        private bool _closeOnly;
        private int _takeProfit;
        private int _stopLoss;
        private int _maxRisk;
        private int _freezeAfterStopLossMinutes;
        private int _profitSequence;
        private decimal _profitSequenceTakeProfit;
        private DateTime _freezeExpiration = DateTime.MinValue;
        private decimal _spreadLimit;
        private decimal _takeProfitNormalized;
        private decimal _stopLossNormalized;
        private decimal _profitSequenceTakeProfitNormalized;
        private decimal _atrCurrentValue;
        private int _turnMulti;
        private double _calculatedSignalsLogSuspense;
        private decimal _turnMultiDiscount;
        private TimeFrame _zZHighTimeFrame;
        private TimeFrame _zZLowTimeFrame;
        private int _zZHighBeforeBarsCount;
        private int _zZHighAfterBarsCount;
        private int _zZHighBarsCount;
        private int _zZLowBeforeBarsCount;
        private int _zZLowAfterBarsCount;
        private int _zZLowBarsCount;
        private int _cciPeriod;
        private decimal _cciThreshold;
        private bool _shouldOpenTurnOrderHighTF;
        private decimal _turnOrderBreakdownPrice;
        private DateTime _signalTime;
        private bool _shouldOpenTurnOrderLowTF;
        private decimal _cciValue;
        private bool _shouldOpenTurnOrderCci;
        private readonly IExpertParametersAdapter _expertParametersAdapter;
        private readonly IBarsHistoryAdapter _barsHistoryAdapter;

        private readonly Dictionary<string, IList<string>> _logInfo = new Dictionary<string, IList<string>>();

        public GoldenGooseV6(IExpertParametersAdapter expertParametersAdapter, IBarsHistoryAdapter barsHistoryAdapter)
        {
            _expertParametersAdapter = expertParametersAdapter;
            _barsHistoryAdapter = barsHistoryAdapter;
        }
        public GoldenGooseV6()
        {
            _expertParametersAdapter = new ExpertParametersAdapter();
            _barsHistoryAdapter = new BarsHistoryAdapter();
        }

        public override void InitializeParameters(IReadOnlyDictionary<string, object> parameters)
        {
            var parsedParams = _expertParametersAdapter.GetParameters<GgV6Parameters>(parameters);

            Risk = 0.0m;
            _calculatedSignalsLogSuspense = parsedParams.CalculatedSignalsLogSuspense == 0 ? 60 : parsedParams.CalculatedSignalsLogSuspense;
            Lots = parsedParams.Lots;
            Direction = parsedParams.Direction;
            Instrument = parsedParams.Instrument;
            _closeOnly = parsedParams.CloseOnly;
            _takeProfit = parsedParams.Profit;
            _atrBars = parsedParams.AtrBars;
            _profitSequence = parsedParams.ProfitSequence;
            _profitSequenceTakeProfit = parsedParams.ProfitSequenceTakeProfit;
            _turnMulti = parsedParams.TurnMulti == 0 ? 5 : parsedParams.TurnMulti;
            _turnMultiDiscount = parsedParams.TurnMultiDiscount == 0.0m ? 0.8m : parsedParams.TurnMultiDiscount;
            _spreadLimit = parsedParams.SpreadLimit == 0.0m ? 0.7m * _takeProfit : parsedParams.SpreadLimit;
            _stopLoss = parsedParams.StopLoss == 0 ? 5 : parsedParams.StopLoss;
            _maxRisk = parsedParams.MaxRisk == 0 ? 150 : parsedParams.MaxRisk;
            _freezeAfterStopLossMinutes = parsedParams.FreezeAfterStopLossMinutes == 0 ? 720 : parsedParams.FreezeAfterStopLossMinutes;
            _ = Enum.TryParse(parsedParams.ZZHighTimeFrame, out _zZHighTimeFrame);
            _ = Enum.TryParse(parsedParams.ZZLowTimeFrame, out _zZLowTimeFrame);
            _zZHighBeforeBarsCount = parsedParams.ZZHighBeforeBarsCount;
            _zZHighAfterBarsCount = parsedParams.ZZHighAfterBarsCount;
            _zZHighBarsCount = parsedParams.ZZHighBarsCount;
            _zZLowBeforeBarsCount = parsedParams.ZZLowBeforeBarsCount;
            _zZLowAfterBarsCount = parsedParams.ZZLowAfterBarsCount;
            _zZLowBarsCount = parsedParams.ZZLowBarsCount;
            _cciPeriod = parsedParams.CciPeriod;
            _cciThreshold = parsedParams.CciThreshold;
        }

        public override void InitializeExpertStateIfNeeded(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            Initialized = true;
        }

        public override (IList<TradingCommand>, SignalValues) TradeAndReturnSignals(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders,
            Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            PreCalculateSignalsAsync(priceTick, historyBars, activeOrders).Wait();
            CalculateSignalsAsync(priceTick, historyBars).Wait();
            var signals = GetSignals();

            var message = "Failed to execute trade. GoldenGooseV6";
            message += $"{Environment.NewLine}ExpertId: {Id}";
            if (priceTick.Ask - priceTick.Bid > priceTick.PointSize * _spreadLimit)
            {
                Logger.Log.Error($"{message}{Environment.NewLine}Spread limit: {priceTick.PointSize * _spreadLimit}{Environment.NewLine}Spread: {priceTick.Ask - priceTick.Bid}");
                return (null, signals);
            }

            if (_isReadyToFreeze && activeOrders != null && activeOrders.Count == 1)
            {
                StartFreeze(priceTick);
                _isReadyToFreeze = false;
                return (null, signals);
            }

            IList<TradingCommand> commands = CreateOrderCommands(priceTick, activeOrders);

            PostCalculateSignalsAsync(priceTick, historyBars, activeOrders).Wait();

            return (commands, signals);
        }

        private async Task PreCalculateSignalsAsync(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars, IReadOnlyList<ActiveOrder> activeOrders)
        {
            if (!activeOrders.Any())
            {
                _shouldOpenTurnOrderCci = false;
            }

            return;
        }

        private async Task PostCalculateSignalsAsync(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars, IReadOnlyList<ActiveOrder> activeOrders)
        {

            return;
        }

        private IList<TradingCommand> CreateOrderCommands(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
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
                    BuildLogInfo("Trading. GoldenGooseV6. Open turn order", logs);
                    LogCalculatedSignals(priceTick, true);
                }

                if (activeOrders.Any() && priceTick.TickServerTime < _freezeExpiration)
                {
                    var logs = new List<string>
                    {
                        $"TickServerTime: {priceTick.TickServerTime}",
                        $"FreezeExpiration: {_freezeExpiration}"
                    };
                    BuildLogInfo("Trading. GoldenGooseV6. Freeze", logs);
                    return null;
                }

                commands = TradeTurnOrder(priceTick, turnOrderLots);
                if (commands != null && commands.Count > 0)
                {
                    return commands;
                }
            }

            if (activeOrders.Any())
            {
                if (!Online || !_closeOnly)
                {
                    commands = TradeOpenProfitSequenceIfNeeded(priceTick, activeOrders);
                    if (commands != null && commands.Count > 0)
                    {
                        return commands;
                    }
                }
            }

            if (activeOrders.Count == 0)
            {
                if (!Online || !_closeOnly)
                {
                    commands = TradeOpen(priceTick, Lots);
                }
                if (Online)
                {
                    BuildLogInfo("Trading. GoldenGooseV6. Open first order", new List<string>());
                    LogCalculatedSignals(priceTick, true);
                }
                _freezeExpiration = DateTime.MinValue;
                return commands;
            }

            commands = TradeClose(priceTick, activeOrders);
            if (commands != null && commands.Count > 0)
            {
                if (Online)
                {
                    BuildLogInfo("Trading. GoldenGooseV6. Close order", new List<string>());
                    LogCalculatedSignals(priceTick, true);
                }
                _freezeExpiration = DateTime.MinValue;
                return commands;
            }

            commands = TradeUpdate(activeOrders);
            if (commands != null && commands.Count > 0)
            {
                if (Online)
                {
                    BuildLogInfo("Trading. GoldenGooseV6. Update order", new List<string>());
                    LogCalculatedSignals(priceTick, true);
                }
                return commands;
            }

            return null;
        }

        private IList<TradingCommand> TradeOpenProfitSequenceIfNeeded(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            if (activeOrders.Any() && activeOrders.Count < _profitSequence && _profitSequence > 0 && _profitSequenceTakeProfit > 0.0m)
            {
                var orders = activeOrders.OrderBy(x => x.OpenTime).ToList();
                var firstOrder = orders.First();
                var lastOrder = orders.Last();

                if (firstOrder.Lots == lastOrder.Lots)
                {
                    switch (lastOrder.BuySell)
                    {
                        case OrderBuySell.Sell:
                            if (lastOrder.OpenPrice - priceTick.Ask > _takeProfitNormalized)
                            {
                                var commands = new List<TradingCommand>();
                                commands.AddRange(TradeOpen(priceTick, lastOrder.Lots));
                                return commands;
                            }
                            break;
                        case OrderBuySell.Buy:
                            if (priceTick.Bid - lastOrder.OpenPrice > _takeProfitNormalized)
                            {
                                var commands = new List<TradingCommand>();
                                commands.AddRange(TradeOpen(priceTick, lastOrder.Lots));
                                return commands;
                            }
                            break;
                    }
                }
            }

            return null;
        }

        private IList<TradingCommand> TradeUpdate(IReadOnlyList<ActiveOrder> activeOrders)
        {
            if (activeOrders.Count < 2)
            {
                return null;
            }

            var orders = activeOrders.OrderBy(x => x.OpenTime).ToList();
            var lastOrder = orders.Last();
            if (HasMaxRisk(lastOrder) && lastOrder.StopLossPrice == 0.0m)
            {
                var commands = new List<TradingCommand>();
                foreach (var order in activeOrders)
                {
                    var stopLossPrice = lastOrder.BuySell == OrderBuySell.Buy ? lastOrder.OpenPrice - _stopLossNormalized
                        : lastOrder.OpenPrice + _stopLossNormalized;
                    var updateCommand = new TradingCommand
                    {
                        OpenClose = OrderOpenClose.Update,
                        OrderId = order.OrderId,
                        Instrument = order.Instrument,
                        Lots = Lots,
                        BuySell = order.BuySell,
                        StopLoss = stopLossPrice
                    };
                    commands.Add(updateCommand);
                }
                _isReadyToFreeze = true;
                return commands;
            }

            return null;
        }

        private IList<TradingCommand> TradeTurnOrder(PriceTick priceTick, int turnOrderLots)
        {
            var commands = new List<TradingCommand>();
            commands.AddRange(TradeOpen(priceTick, turnOrderLots));
            return commands;
        }

        private bool HasMaxRisk(ActiveOrder lastOrder)
        {
            var minLots = lastOrder.Lots * _turnMulti * _turnMultiDiscount;
            if (minLots > Lots * _maxRisk)
            {
                return true;
            }

            return false;
        }

        private int ShouldOpenTurnOrder(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            if (activeOrders.Count == 0)
            {
                return 0;
            }

            if (!_shouldOpenTurnOrderCci)
            {
                return 0;
            }

            var orders = activeOrders.OrderBy(x => x.OpenTime).ToList();
            var lastOrder = orders.Last();
            var priceDiff = 0.0m;
            var openPriceDiff = 0.0m;

            //var message = "Trading. GoldenGooseV6. Should open turn order";
            //message += $"{Environment.NewLine}Turn Order Breakdown Price: {_turnOrderBreakdownPrice}";
            //message += $"{Environment.NewLine}Has Max Risk: {HasMaxRisk(lastOrder)}";
            //Logger.Log.Debug(message);

            if (HasMaxRisk(lastOrder))
            {
                return 0;
            }

            switch (Direction)
            {
                case OrderBuySell.Buy:
                    priceDiff = priceTick.Bid - lastOrder.OpenPrice;
                    openPriceDiff = lastOrder.OpenPrice - priceTick.Ask;
                    break;
                case OrderBuySell.Sell:
                    priceDiff = lastOrder.OpenPrice - priceTick.Ask;
                    openPriceDiff = priceTick.Bid - lastOrder.OpenPrice;
                    break;
            }

            var logs = new List<string>
            {
                $"Price diff: {priceDiff}",
                $"Open price diff: {openPriceDiff}",
                $"Multiplied take profit: {_takeProfitNormalized * _turnMulti}",
                $"Last order Id: {lastOrder.OrderId}",
                $"First condition: {_shouldOpenTurnOrderCci}",
                $"Second condition: {-priceDiff >= _takeProfitNormalized * _turnMulti}",
                $"Lots: {lastOrder.Lots * (int)(-_turnMultiDiscount * priceDiff / _takeProfitNormalized)}"
            };
            BuildLogInfo("Trading. GoldenGooseV6. Should open turn order", logs);
            LogCalculatedSignals(priceTick);

            if (openPriceDiff <= 0.0m)
            {
                return 0;
            }

            if (-priceDiff < _takeProfitNormalized * _turnMulti)
            {
                return 0;
            }

            if (_shouldOpenTurnOrderCci)
            {
                var resultLots = lastOrder.Lots * (int)(-_turnMultiDiscount * priceDiff / _takeProfitNormalized);
                var openOrdersCount = orders.Count(x => x.Lots == lastOrder.Lots);
                return lastOrder.Lots * (openOrdersCount - 1) + (resultLots < _maxRisk ? resultLots : _maxRisk);
            }
            return 0;
        }

        private SignalValues GetSignals()
        {
            var result = new SignalValues
            {
                SignalTime = _signalTime
            };

            var signals = new Dictionary<string, decimal>
            {
                { "atr", _atrCurrentValue },
                { "turnOrderHighTF", _shouldOpenTurnOrderHighTF ? 1.0m : 0.0m },
                { "turnOrderBreakdownPrice", _turnOrderBreakdownPrice },
                { "cci", _cciValue },
                { "turnOrderLowTF", _shouldOpenTurnOrderLowTF ? 1.0m : 0.0m },
                { "turnOrderCci", _shouldOpenTurnOrderCci ? 1.0m : 0.0m },
            };

            result.Signals = signals;

            return result;
        }

        private async Task CalculateSignalsAsync(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            if (historyBars == null)
            {
                return;
            }

            await CalculateNormalized(priceTick, historyBars).ConfigureAwait(false);
            await CalculateZigZag(priceTick, historyBars).ConfigureAwait(false);
            await CalculateZigZagPriceBreakdown(priceTick).ConfigureAwait(false);
            await CalculateCci(priceTick, historyBars).ConfigureAwait(false);
            await CalculateCciBreakdown(priceTick).ConfigureAwait(false);
            BuildCommonLogInfo(priceTick, historyBars);
            LogCalculatedSignals(priceTick);
        }

        private async Task CalculateCciBreakdown(PriceTick priceTick)
        {
            if (_cciPeriod == 0 || _cciThreshold == 0.0m)
            {
                return;
            }

            if (!_shouldOpenTurnOrderLowTF)
            {
                return;
            }

            var cciDiff = 0.0m;

            switch (Direction)
            {
                case OrderBuySell.Buy:
                    cciDiff = -_cciThreshold - _cciValue;
                    break;
                case OrderBuySell.Sell:
                    cciDiff = _cciValue - _cciThreshold;
                    break;
                default:
                    break;
            }

            if (cciDiff >= 0.0m)
            {
                _shouldOpenTurnOrderCci = true;
                _shouldOpenTurnOrderLowTF = false;
            }

        }

        private async Task CalculateZigZagPriceBreakdown(PriceTick priceTick)
        {
            if (_turnOrderBreakdownPrice == 0.0m)
            {
                return;
            }

            var breakdownPriceDiff = 0.0m;

            switch (Direction)
            {
                case OrderBuySell.Buy:
                    breakdownPriceDiff = priceTick.Bid - _turnOrderBreakdownPrice;
                    break;
                case OrderBuySell.Sell:
                    breakdownPriceDiff = _turnOrderBreakdownPrice - priceTick.Bid;
                    break;
                default:
                    break;
            }

            if (breakdownPriceDiff > 0.0m)
            {
                _shouldOpenTurnOrderLowTF = _shouldOpenTurnOrderHighTF;
                _shouldOpenTurnOrderHighTF = false;
                _shouldOpenTurnOrderCci = false;
                _turnOrderBreakdownPrice = 0.0m;
            }
        }

        private async Task CalculateCci(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            if (_cciPeriod == 0 || _cciThreshold == 0.0m)
            {
                return;
            }

            var historyKeyLow = new PriceBarsHistoryKey
            {
                Instrument = priceTick.Instrument,
                BarsCount = _zZLowBarsCount,
                TimeFrame = _zZLowTimeFrame
            };
            var lowKey = await _barsHistoryAdapter.GetPriceBarKey(historyKeyLow).ConfigureAwait(false);
            if (!historyBars.ContainsKey(lowKey))
            {
                return;
            }

            var cciBars = historyBars[lowKey].Skip(1).Take(_cciPeriod).ToList();
            if (cciBars.Count() != _cciPeriod)
            {
                return;
            }

            var lastBar = cciBars[0];
            var tpValue = lastBar.BidHigh + lastBar.BidLow + lastBar.BidClose;
            tpValue /= 3;
            var tpSmaValue = cciBars.Sum(x => x.BidHigh + x.BidLow + x.BidClose);
            tpSmaValue /= _cciPeriod * 3;
            var tpSmaDeviationValue = cciBars.Sum(x => Math.Abs((x.BidHigh + x.BidLow + x.BidClose)/3 - tpSmaValue)) / _cciPeriod;
            tpSmaDeviationValue *= 0.015m;
            _cciValue = tpSmaDeviationValue == 0.0m ? 0.0m : (tpValue - tpSmaValue) / tpSmaDeviationValue;
        }

        private async Task CalculateZigZag(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            var historyKeyHigh = new PriceBarsHistoryKey
            {
                Instrument = priceTick.Instrument,
                BarsCount = _zZHighBarsCount,
                TimeFrame = _zZHighTimeFrame
            };

            var historyKeyLow = new PriceBarsHistoryKey
            {
                Instrument = priceTick.Instrument,
                BarsCount = _zZLowBarsCount,
                TimeFrame = _zZLowTimeFrame
            };

            var highKey = await _barsHistoryAdapter.GetPriceBarKey(historyKeyHigh).ConfigureAwait(false);
            if (!historyBars.ContainsKey(highKey))
            {
                return;
            }

            var lowKey = await _barsHistoryAdapter.GetPriceBarKey(historyKeyLow).ConfigureAwait(false);
            if (!historyBars.ContainsKey(lowKey))
            {
                return;
            }

            _signalTime = historyBars[lowKey][0].Time;

            var highHistoryBars = historyBars[highKey].Skip(1);
            if (highHistoryBars.Count() < _zZHighBarsCount - 1)
            {
                return;
            }

            if (_zZHighAfterBarsCount + _zZHighBeforeBarsCount > _zZHighBarsCount
                || _zZHighBeforeBarsCount == 0)
            {
                return;
            }

            var barsWindowHigh = highHistoryBars.Take(_zZHighAfterBarsCount + _zZHighBeforeBarsCount);

            var maxIndexHigh = barsWindowHigh.IndexOfMaximumElement();
            if (Direction == OrderBuySell.Sell && maxIndexHigh == _zZHighAfterBarsCount)
            {
                _shouldOpenTurnOrderHighTF = true;
            }

            var minIndexHigh = barsWindowHigh.IndexOfMinimumElement();
            if (Direction == OrderBuySell.Buy && minIndexHigh == _zZHighAfterBarsCount)
            {
                _shouldOpenTurnOrderHighTF = true;
            }

            var lowHistoryBars = historyBars[lowKey].Skip(1);
            if (lowHistoryBars.Count() < _zZLowBarsCount - 1)
            {
                return;
            }

            if (_zZLowAfterBarsCount + _zZLowBeforeBarsCount > _zZLowBarsCount
                || _zZLowBeforeBarsCount == 0)
            {
                return;
            }

            var barsWindowLow = lowHistoryBars.Take(_zZLowAfterBarsCount + _zZLowBeforeBarsCount);
            switch (Direction)
            {
                case OrderBuySell.Buy:
                    var maxIndexLow = barsWindowLow.IndexOfMaximumElement();
                    var localHigh = barsWindowLow.ElementAt(maxIndexLow).BidHigh;
                    if (maxIndexLow == _zZLowAfterBarsCount && (_turnOrderBreakdownPrice > localHigh || _turnOrderBreakdownPrice == 0.0m))
                    {
                        _turnOrderBreakdownPrice = localHigh;
                        _shouldOpenTurnOrderCci = false;
                    }
                    break;
                case OrderBuySell.Sell:
                    var minIndexLow = barsWindowLow.IndexOfMinimumElement();
                    var localLow = barsWindowLow.ElementAt(minIndexLow).BidLow;
                    if (minIndexLow == _zZLowAfterBarsCount && (_turnOrderBreakdownPrice < localLow || _turnOrderBreakdownPrice == 0.0m))
                    {
                        _turnOrderBreakdownPrice = localLow;
                        _shouldOpenTurnOrderCci = false;
                    }
                    break;
                default:
                    _turnOrderBreakdownPrice = 0.0m;
                    break;
            }
        }

        private async Task CalculateNormalized(PriceTick priceTick, Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            _takeProfitNormalized = priceTick.PointSize * _takeProfit;
            _stopLossNormalized = priceTick.PointSize * _stopLoss;
            _profitSequenceTakeProfitNormalized = priceTick.PointSize * _profitSequenceTakeProfit;
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
            _stopLossNormalized = _stopLoss * _atrCurrentValue * priceTick.PointSize;
            _profitSequenceTakeProfitNormalized = _profitSequenceTakeProfit * _atrCurrentValue * priceTick.PointSize;
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
            if (activeOrders.Count == 1 && !(_profitSequence > 1 && _profitSequenceTakeProfit > 0.0m))
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

        private void StartFreeze(PriceTick priceTick)
        {
            _freezeExpiration = priceTick.TickServerTime;
            _freezeExpiration = _freezeExpiration.AddMinutes(_freezeAfterStopLossMinutes);
        }

        private bool ShouldCloseProfitSequenceByTakeProfit(PriceTick priceTick, ActiveOrder lastOrder)
        {
            switch (lastOrder.BuySell)
            {
                case OrderBuySell.Sell:
                    if (lastOrder.OpenPrice - priceTick.Ask > _profitSequenceTakeProfitNormalized)
                    {
                        return true;
                    }
                    break;
                case OrderBuySell.Buy:
                    if (priceTick.Bid - lastOrder.OpenPrice > _profitSequenceTakeProfitNormalized)
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        private IList<TradingCommand> CloseWithProfitIfNeeded(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            if (activeOrders.Count > 1)
            {
                var commands = new List<TradingCommand>();
                var profit = 0.0m;
                var orders = activeOrders.OrderBy(x => x.OpenTime).ToList();
                var firstOrder = orders.First();
                var lastOrder = orders.Last();

                if (firstOrder.Lots == lastOrder.Lots && _profitSequence > 0 && _profitSequenceTakeProfit > 0.0m)
                {
                    if (orders.Count == _profitSequence && ShouldCloseProfitSequenceByTakeProfit(priceTick, lastOrder))
                    {
                        CloseAllOpenOrders(orders, commands);
                        commands.AddRange(TradeOpen(priceTick, Lots));
                        return commands;
                    }
                    return null;
                }

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

                CloseAllOpenOrders(orders, commands);
                if (!Online || !_closeOnly)
                {
                    commands.AddRange(TradeOpen(priceTick, Lots));
                }

                return commands;
            }

            return null;
        }

        private IList<TradingCommand> CloseOrdersIfNeeded(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            var closeCommand2 = CloseWithProfitIfNeeded(priceTick, activeOrders);
            if (closeCommand2 != null)
            {
                var commands = new List<TradingCommand>();
                commands.AddRange(closeCommand2);
                return commands;
            }

            return null;
        }

        private void CloseAllOpenOrders(List<ActiveOrder> orders, List<TradingCommand> commands)
        {
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
            }
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
                $"Take profit normalized: {_takeProfitNormalized:.2f}",
                $"Stop loss normalized: {_stopLossNormalized:.2f}",
                $"ATR: {_atrCurrentValue:.4f}",
                $"Should open turn order high TF: {_shouldOpenTurnOrderHighTF}",
                $"Turn order breakdown price: {_turnOrderBreakdownPrice:.6f}",
                $"CCI value: {_cciValue:.2f}"
            };

            if (historyBars == null)
            {
                return;
            }

            foreach (var key in historyBars.Keys)
            {
                logs.Add($"Key in history: {key}");
            }

            const string calculateSignalsKey = "Trading. GoldenGooseV6. Calculate signals";
            if (_logInfo.ContainsKey(calculateSignalsKey))
            {
                _logInfo.Remove(calculateSignalsKey);
            }

            _logInfo.Add(calculateSignalsKey, logs);

        }
    }
}
