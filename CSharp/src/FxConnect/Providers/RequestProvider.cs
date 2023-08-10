using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Enums;
using Common.Logging;
using FxConnect.Adapters;
using FxConnect.Clients;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Providers
{
    public class RequestProvider : IRequestProvider
    {
        private const int BarsCountInRequest = 200;
        private readonly IFxConnectClient _fxConnectClient;
        private readonly IRequestAdapter _requestAdapter;
        private readonly IPriceRequestProvider _priceRequestProvider;

        public RequestProvider(IFxConnectClient fxConnectClient, IRequestAdapter requestAdapter, IPriceRequestProvider priceRequestProvider)
        {
            _fxConnectClient = fxConnectClient;
            _requestAdapter = requestAdapter;
            _priceRequestProvider = priceRequestProvider;
        }

        public async Task<bool> CloseOrder(O2GSession session, string accountId, O2GOfferTableRow offer, string orderId,
            int lots, OrderBuySell buySell, string token)
        {
            O2GResponse response = null;
            var offerId = offer.OfferID;
            try
            {
                await _fxConnectClient.StartRequest(session).ConfigureAwait(false);
                var apiBuySell = buySell == OrderBuySell.Buy ? Constants.Buy : Constants.Sell;
                var spread = offer.Ask - offer.Bid;
                var minRate = buySell == OrderBuySell.Buy ? offer.Bid - spread : offer.Ask + spread;
                var maxRate = buySell == OrderBuySell.Buy ? offer.Bid + 10 * spread : offer.Ask - 10 * spread;
                var request = await _requestAdapter.CreateCloseOrderRequest(session, accountId, offerId, lots, apiBuySell, orderId, minRate, maxRate, token).ConfigureAwait(false);
                if (request == null)
                {
                    throw new Exception("Cannot create request");
                }

                response = await _fxConnectClient.SendRequest(session, request).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var message = "Failed to close order.";
                message += $"{Environment.NewLine}OfferId: {offerId}, OrderId: {orderId}";
                message += $"{Environment.NewLine}Exception: {e}";
                Logger.Log.Error(message);
            }
            finally
            {
                await _fxConnectClient.EndRequest(session).ConfigureAwait(false);
            }

            return response != null && response.Type == O2GResponseType.CommandResponse;
        }

        public async Task<IList<PriceBar>> GetHistoryPrices(O2GSession session, string instrument, string tf, int depth)
        {
            var priceBars = new List<PriceBar>();

            var request = await _requestAdapter.CreateHistoryPrices(session, instrument, tf, depth).ConfigureAwait(false);
            if (request == null)
            {
                var message = "GetHistoryPrices. Cannot create a request.";
                message += $"{Environment.NewLine}Instrument: {instrument}";
                message += $"{Environment.NewLine}Timeframe: {tf}";
                message += $"{Environment.NewLine}Depth: {depth}";
                Logger.Log.Error(message);
                return new List<PriceBar>();
            }

            priceBars.AddRange(await _priceRequestProvider.GetPrices(session, request).ConfigureAwait(false));

            return priceBars.OrderByDescending(x => x.Time).ToList();
        }

        public async Task<IList<PriceBar>> GetHistoryPrices(O2GSession session, string instrument, string timeFrameName,
            DateTime dtFrom, DateTime dtTo)
        {
            var request = await _requestAdapter.CreateHistoryPrices(session, instrument, timeFrameName, BarsCountInRequest).ConfigureAwait(false);
            if (request == null)
            {
                throw new Exception("Cannot create request");
            }

            var priceBars = await _priceRequestProvider.LoopThroughPrices(session, request, dtFrom, dtTo).ConfigureAwait(false);

            return priceBars.OrderByDescending(x => x.Time).ToList();
        }

        public async Task<string> OpenOrder(O2GSession session, string accountId, O2GOfferTableRow offer, int amount, OrderBuySell buySell, string token)
        {
            O2GResponse response = null;
            try
            {
                await _fxConnectClient.StartRequest(session).ConfigureAwait(false);
                var buySellValue = buySell == OrderBuySell.Buy ? Constants.Buy : Constants.Sell;
                var spread = offer.Ask - offer.Bid;
                var minRate = buySell == OrderBuySell.Buy ? offer.Ask + spread : offer.Bid - spread;
                var maxRate = buySell == OrderBuySell.Buy ? offer.Ask - 10 * spread : offer.Bid + 10 * spread;
                var request = await _requestAdapter.CreateTrueMarketOrderRequest(session, offer.OfferID, accountId, amount, buySellValue,
                    token, minRate, maxRate).ConfigureAwait(false);
                if (request == null)
                {
                    throw new Exception("Cannot create request");
                }

                response = await _fxConnectClient.SendRequest(session, request).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var message = "Failed to open order.";
                message += $"{Environment.NewLine}Token: {token}, Amount: {amount}";
                message += $"{Environment.NewLine}Exception: {e}";
                Logger.Log.Error(message);
            }
            finally
            {
                await _fxConnectClient.EndRequest(session).ConfigureAwait(false);
            }

            if (response != null && response.Type == O2GResponseType.CreateOrderResponse)
            {
                var readerFactory = session.getResponseReaderFactory();
                var ordersResponseReader = readerFactory.createOrderResponseReader(response);
                return ordersResponseReader.OrderID;
            }

            return null;
        }

        public async Task<bool> StopCloseOrder(O2GSession session, string accountId, O2GOfferTableRow offer, string orderId, int lots, OrderBuySell buySell, decimal stopLoss, string expertId)
        {
            O2GResponse response = null;
            var offerId = offer.OfferID;
            try
            {
                await _fxConnectClient.StartRequest(session).ConfigureAwait(false);
                var apiBuySell = buySell == OrderBuySell.Buy ? Constants.Buy : Constants.Sell;
                var request = await _requestAdapter.CreateCloseStopOrderRequest(session, accountId, offerId, lots, apiBuySell, orderId, (double)stopLoss, expertId).ConfigureAwait(false);
                if (request == null)
                {
                    throw new Exception("Cannot create request");
                }

                response = await _fxConnectClient.SendRequest(session, request).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var message = "Failed to set stop close order.";
                message += $"{Environment.NewLine}OfferId: {offerId}, OrderId: {orderId}";
                message += $"{Environment.NewLine}Exception: {e}";
                Logger.Log.Error(message);
            }
            finally
            {
                await _fxConnectClient.EndRequest(session).ConfigureAwait(false);
            }

            return response != null && response.Type == O2GResponseType.CommandResponse;
        }

        public async Task Subscribe(O2GSession session, List<string> needToUnSubscribeOffers,
            List<string> needToSubscribeOffers)
        {
            if (!needToSubscribeOffers.Any() && !needToUnSubscribeOffers.Any())
            {
                return;
            }

            var batchValueMap = await _requestAdapter.CreateSetSubscriptionStatusRequestBatchHeader(session).ConfigureAwait(false);
            foreach (var offer in needToUnSubscribeOffers)
            {
                await _requestAdapter.AddSetSubscriptionStatusRequestToBatch(session, batchValueMap, offer, Constants.SubscriptionStatuses.Disable).ConfigureAwait(false);
            }

            foreach (var offer in needToSubscribeOffers)
            {
                await _requestAdapter.AddSetSubscriptionStatusRequestToBatch(session, batchValueMap, offer, Constants.SubscriptionStatuses.Tradable).ConfigureAwait(false);
            }

            await SubscribeBatch(session, batchValueMap).ConfigureAwait(false);
        }

        private async Task SubscribeBatch(O2GSession session, O2GValueMap batchValuemap)
        {
            try
            {
                await _fxConnectClient.StartRequest(session).ConfigureAwait(false);
                var request = await _requestAdapter.CreateRequest(session, batchValuemap).ConfigureAwait(false);
                await _fxConnectClient.SendRequestWithoutResponse(session, request).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Log.Error($"Failed to subscribe instruments. Exception: {e}");
            }
            finally
            {
                await _fxConnectClient.EndRequest(session).ConfigureAwait(false);
            }
        }
    }
}
