using System.Collections.Generic;
using Common.Models;

namespace Common.Experts
{
    public interface IExpertWithSignals
    {
        (IList<TradingCommand>, SignalValues) TradeAndReturnSignals(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders,
            Dictionary<string, IList<PriceBarForStore>> historyBars);
    }
}
