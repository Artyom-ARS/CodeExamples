using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Enums;
using Common.Models;
using FxConnect.Models;

namespace FxConnect.Providers
{
    public interface IFxconnectProvider
    {
        event EventHandler<DTOPriceUpdate> PriceUpdate;

        event EventHandler<DTOOrderUpdate> OrderUpdate;

        Task<DTOAccount> Login();

        Task Logout();

        Task<IList<ActiveOrder>> GetActiveOrders(string accountId);

        Task<bool> CloseOrder(string orderId, OrderBuySell buySell, string id, string instrument, int lots, string expertId);

        Task<ActiveOrder> OpenOrder(string accountId, string instrument, int lots, OrderBuySell buySell, string token);

        Task StartWatchingPriceUpdates();

        Task Subscribe(List<string> instruments);

        Task<IList<PriceBarForStore>> RequestHistoryPrices(string instrument, string tf, int depth);

        Task<(InstrumentForStoreBase, IList<PriceBarForStore>)> RequestHistoryPrices(string instrument, string timeFrameName, DateTime dtFrom, DateTime dtTo);

        Task<InstrumentForStoreBase> GetInstrument(string instrument);

        Task<bool> UpdateOrder(string orderId, OrderBuySell buySell, string accountId, string instrument, int lots, decimal stopLoss, decimal takeProfit, string expertId);
    }
}
