using System.Collections.Generic;
using Common.Enums;
using Common.Models;

namespace Common.Experts
{
    public interface IExpert
    {
        string Id { get; }

        decimal Risk { get; }

        int Lots { get; }

        string Instrument { get; }

        OrderBuySell Direction { get; }

        void InitializeId(string id);

        (IList<TradingCommand>, SignalValues) Trade(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders, Dictionary<string, IList<PriceBarForStore>> historyBars);

        IList<TradingCommand> TradeEx(PriceTick priceTick, IReadOnlyList<ActiveOrder> activeOrders, Dictionary<string, IList<PriceBarForStore>> historyBars);

        void InitializeParameters(IReadOnlyDictionary<string, object> parameters, bool online);
    }
}
