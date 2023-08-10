using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Adapters
{
    public interface IRequestAdapter
    {
        Task<O2GRequest> CreateCloseOrderRequest(O2GSession session, string accountId, string offerId, int amount, string buySell, string orderId, double minRate, double maxRate, string token);

        Task<O2GRequest> CreateTrueMarketOrderRequest(O2GSession session, string offerId, string accountId, int amount, string buySellValue, string token, double minRate, double maxRate);

        Task<O2GRequest> CreateRequest(O2GSession session, O2GValueMap valueMap);

        Task<O2GValueMap> CreateSetSubscriptionStatusRequestBatchHeader(O2GSession session);

        Task AddSetSubscriptionStatusRequestToBatch(O2GSession session, O2GValueMap batchValuemap, string offerId, string status);

        Task<O2GRequest> CreateHistoryPrices(O2GSession session, string instrument, string tf, int depth);

        Task<IList<PriceBar>> GetPriceBarListFromResponse(O2GSession session, O2GResponse response);

        Task UpdateHistoryPrices(O2GSession session, O2GRequest request, DateTime dtFrom, DateTime dtTo);

        Task<O2GRequest> CreateCloseStopOrderRequest(O2GSession session, string accountId, string offerId, int lots, string apiBuySell, string orderId, double stopLoss, string expertId);
    }
}
