using System;
using System.Collections.Generic;
using Common.Enums;
using Common.Logging;
using Common.Models;

namespace ExpertCollection
{
    public class BaseExpert
    {
        private int _breakIndex;
        private int _breakAfterSteps;
        private DateTime _breakTime;
        protected const int Suspense = 900;
        protected bool Online { get; set; }
        protected bool Initialized { get; set; }
        protected DateTime _lastOrderOpenTmstmp;

        public string Id { get; private set; }
        public decimal Risk { get; protected set; }
        public int Lots { get; protected set; }
        public string Instrument { get; protected set; }
        public OrderBuySell Direction { get; protected set; }
        protected OrderBuySell OppositeDirection => Direction == OrderBuySell.Buy ? OrderBuySell.Sell : OrderBuySell.Buy;

        public virtual void InitializeId(string id)
        {
            Id = id;
        }

        public virtual void InitializeParameters(IReadOnlyDictionary<string, object> parameters)
        {
        }

        public void InitializeParameters(IReadOnlyDictionary<string, object> parameters, bool online)
        {
            InitializeParameters(parameters);
            Online = online;
        }

        public virtual void InitializeExpertStateIfNeeded(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders)
        {
        }

        public bool ShouldBreak(PriceTick priceTick)
        {
            if (Online)
            {
                var current = priceTick.TickServerTime;

                if (current < _lastOrderOpenTmstmp.AddMilliseconds(Suspense))
                {
                    return true;
                }
                _lastOrderOpenTmstmp = current;
            }

            return false;
        }

        public (IList<TradingCommand>, SignalValues) Trade(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders,
            Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            if (priceTick.Instrument != Instrument)
            {
                return (null, null);
            }

            var message = "Failed to execute trade. Invalid price tick.";
            message += $"{Environment.NewLine}ExpertId: {Id}";
            if (priceTick.Bid == 0.0m || priceTick.Ask == 0.0m)
            {
                Logger.Log.Error(message);
                return (null, null);
            }

            if (ShouldBreak(priceTick))
            {
                return (null, null);
            }

            InitializeExpertStateIfNeeded(priceTick, activeOrders);

            if (_breakAfterSteps != 0 && _breakIndex > _breakAfterSteps)
            {
                _breakAfterSteps = 0;
                _breakIndex = 0;
            }

            if (_breakAfterSteps != 0)
            {
                _breakIndex++;
            }

            if (_breakTime != DateTime.MinValue && _breakTime <= priceTick.TickServerTime)
            {
                _breakTime = DateTime.MinValue;
                // _breakTime = new DateTime(2020, 9, 1, 16, 25, 0);
            }

            return TradeAndReturnSignals(priceTick, activeOrders, historyBars);
        }

        public virtual IList<TradingCommand> TradeEx(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders,
            Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            return null;
        }

        public virtual (IList<TradingCommand>, SignalValues) TradeAndReturnSignals(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders,
            Dictionary<string, IList<PriceBarForStore>> historyBars)
        {
            var commands = TradeEx(priceTick, activeOrders, historyBars);
            return (commands, null);
        }
    }
}