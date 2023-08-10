using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Enums;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Providers
{
    public interface IRequestProvider
    {
        Task<bool> CloseOrder(O2GSession session, string accountId, O2GOfferTableRow offer, string orderId,
            int lots, OrderBuySell buySell, string token);

        Task<string> OpenOrder(O2GSession session, string accountId, O2GOfferTableRow offer, int amount, OrderBuySell buySell, string token);

        Task Subscribe(O2GSession session, List<string> needToUnSubscribeOffers, List<string> needToSubscribeOffers);

        Task<IList<PriceBar>> GetHistoryPrices(O2GSession session, string instrument, string tf, int depth);

        Task<IList<PriceBar>> GetHistoryPrices(O2GSession session, string instrument, string timeFrameName,
            DateTime dtFrom, DateTime dtTo);

        Task<bool> StopCloseOrder(O2GSession session, string accountId, O2GOfferTableRow offer, string orderId, int lots, OrderBuySell buySell, decimal stopLoss, string expertId);
    }
}
