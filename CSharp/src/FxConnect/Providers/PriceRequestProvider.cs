using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using FxConnect.Adapters;
using FxConnect.Clients;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Providers
{
    public class PriceRequestProvider : IPriceRequestProvider
    {
        private readonly IRequestAdapter _requestAdapter;
        private readonly IFxConnectClient _fxConnectClient;

        public PriceRequestProvider(IRequestAdapter requestAdapter, IFxConnectClient fxConnectClient)
        {
            _requestAdapter = requestAdapter;
            _fxConnectClient = fxConnectClient;
        }

        public async Task<IList<PriceBar>> LoopThroughPrices(O2GSession session, O2GRequest request, DateTime dtFrom,
            DateTime dtTo)
        {
            var priceBars = new List<PriceBar>();
            var dtLast = dtTo;
            do
            {
                await _requestAdapter.UpdateHistoryPrices(session, request, dtFrom, dtLast).ConfigureAwait(false);
                var prices = await GetPrices(session, request).ConfigureAwait(false);
                if (prices.Count == 0)
                {
                    break;
                }

                dtLast = prices.LastOrDefault().Time.AddSeconds(-1);
                priceBars.AddRange(prices);
            }
            while (dtLast > dtFrom);
            return priceBars;
        }

        public async Task<IList<PriceBar>> GetPrices(O2GSession session, O2GRequest request)
        {
            try
            {
                await _fxConnectClient.StartRequest(session).ConfigureAwait(false);
                var response = await _fxConnectClient.SendRequest(session, request).ConfigureAwait(false);
                if (response != null && response.Type == O2GResponseType.MarketDataSnapshot)
                {
                    return await _requestAdapter.GetPriceBarListFromResponse(session, response).ConfigureAwait(false);
                }
                else
                {
                    return new List<PriceBar>();
                }
            }
            catch (Exception e)
            {
                Logger.Log.Error($"Failed to get prices. Exception: {e}");
                return new List<PriceBar>();
            }
            finally
            {
                await _fxConnectClient.EndRequest(session).ConfigureAwait(false);
            }
        }
    }
}
