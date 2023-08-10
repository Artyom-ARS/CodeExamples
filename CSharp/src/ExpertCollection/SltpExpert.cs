using System.Collections.Generic;
using Common.Adapters;
using Common.Enums;
using Common.Experts;
using Common.Models;

namespace ExpertCollection
{
    public class SltpExpert : BaseExpert, IExpert
    {
        private readonly IExpertParametersAdapter _expertParametersAdapter;

        private int _stopLoss;
        private int _takeProfit;

        public SltpExpert(IExpertParametersAdapter expertParametersAdapter)
        {
            _expertParametersAdapter = expertParametersAdapter;
        }

        public SltpExpert()
        {
            _expertParametersAdapter = new ExpertParametersAdapter();
        }

        public override void InitializeParameters(IReadOnlyDictionary<string, object> parameters)
        {
            var parsedParams = _expertParametersAdapter.GetParameters<SltpParameters>(parameters);

            Instrument = parsedParams.Instrument;
            Lots = parsedParams.Lots;
            Direction = parsedParams.Direction;
            _takeProfit = parsedParams.Profit;
            _stopLoss = parsedParams.Loose;
        }

        public override IList<TradingCommand> TradeEx(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders,
            Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            if (activeOrders.Count == 0)
            {
                return new List<TradingCommand>
                {
                    new TradingCommand
                    {
                        Lots = Lots,
                        BuySell = Direction,
                        OpenClose = OrderOpenClose.Open,
                        Instrument = priceTick.Instrument,
                    }
                };
            }
            else
            {
                var order = activeOrders[0];
                var closeCommand = new TradingCommand
                {
                    OpenClose = OrderOpenClose.Close,
                    OrderId = order.OrderId,
                    Instrument = order.Instrument,
                    Lots = Lots,
                    BuySell = order.BuySell
                };
                var commands = new List<TradingCommand>
                {
                    closeCommand
                };
                commands.AddRange(TradeOpen(priceTick, null));

                if (order.StopLossPrice == 0.0m)
                {
                    var stopLossPrice = order.BuySell == OrderBuySell.Buy ? order.OpenPrice - 1.0m * _stopLoss * priceTick.PointSize : order.OpenPrice + 1.0m * _stopLoss * priceTick.PointSize;
                    var updateCommand = new TradingCommand
                    {
                        OpenClose = OrderOpenClose.Update,
                        OrderId = order.OrderId,
                        Instrument = order.Instrument,
                        Lots = Lots,
                        BuySell = order.BuySell,
                        StopLoss = stopLossPrice
                    };
                    return new List<TradingCommand>
                    {
                        updateCommand
                    };
                }

                if (order.BuySell == OrderBuySell.Buy)
                {
                    if (priceTick.Bid - order.OpenPrice >= _takeProfit * priceTick.PointSize)
                    {
                        return commands;
                    }
                }

                if (order.BuySell == OrderBuySell.Sell)
                {
                    if (order.OpenPrice - priceTick.Ask >= _takeProfit * priceTick.PointSize)
                    {
                        return commands;
                    }
                }
            }
            return null;
        }

        private IList<TradingCommand> TradeOpen(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
            if (activeOrders != null && activeOrders.Count == 0)
            {
                return new List<TradingCommand> {
                    new TradingCommand
                    {
                        Lots = Lots,
                        BuySell = Direction,
                        OpenClose = OrderOpenClose.Open,
                        Instrument = priceTick.Instrument,
                    }
                };
            }
            return new List<TradingCommand>();
        }
    }
}

