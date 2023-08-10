using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Logging;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Adapters
{
    public class RequestAdapter : IRequestAdapter
    {
        public async Task AddSetSubscriptionStatusRequestToBatch(O2GSession session, O2GValueMap batchValuemap, string offerId, string status)
        {
            var requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            var valueMap = requestFactory.createValueMap();
            valueMap.setString(O2GRequestParamsEnum.Command, Constants.Commands.SetSubscriptionStatus);
            valueMap.setString(O2GRequestParamsEnum.SubscriptionStatus, status);
            valueMap.setString(O2GRequestParamsEnum.OfferID, offerId);
            batchValuemap.appendChild(valueMap);
        }

        public async Task<O2GRequest> CreateCloseStopOrderRequest(O2GSession session, string accountId, string offerId, int lots, string apiBuySell,
            string orderId, double stopLoss, string expertId)
        {
            var requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            var side = apiBuySell == Constants.Buy ? Constants.Sell : Constants.Buy;
            var valueMap = requestFactory.createValueMap();
            valueMap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valueMap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.Stop);
            valueMap.setString(O2GRequestParamsEnum.AccountID, accountId);
            valueMap.setString(O2GRequestParamsEnum.OfferID, offerId);
            valueMap.setString(O2GRequestParamsEnum.TradeID, orderId);
            valueMap.setString(O2GRequestParamsEnum.BuySell, side);
            valueMap.setInt(O2GRequestParamsEnum.Amount, lots);
            valueMap.setDouble(O2GRequestParamsEnum.Rate, stopLoss);
            valueMap.setString(O2GRequestParamsEnum.CustomID, expertId);

            var request = await CreateRequest(requestFactory, valueMap).ConfigureAwait(false);
            return request;
        }

        public async Task<O2GRequest> CreateCloseOrderRequest(O2GSession session, string accountId, string offerId, int amount, string buySell, string orderId, double minRate, double maxRate, string token)
        {
            var requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            var side = buySell == Constants.Buy ? Constants.Sell : Constants.Buy;
            var valueMap = requestFactory.createValueMap();
            valueMap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valueMap.setString(O2GRequestParamsEnum.TradeID, orderId);
            valueMap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.MarketCloseRange);
            valueMap.setString(O2GRequestParamsEnum.AccountID, accountId);
            valueMap.setString(O2GRequestParamsEnum.OfferID, offerId);
            valueMap.setString(O2GRequestParamsEnum.BuySell, side);
            valueMap.setInt(O2GRequestParamsEnum.Amount, amount);
            valueMap.setDouble(O2GRequestParamsEnum.RateMin, minRate);
            valueMap.setDouble(O2GRequestParamsEnum.RateMax, maxRate);
            valueMap.setString(O2GRequestParamsEnum.CustomID, token);

            var request = await CreateRequest(requestFactory, valueMap).ConfigureAwait(false);
            return request;
        }

        public async Task<O2GRequest> CreateHistoryPrices(O2GSession session, string instrument, string tf, int depth)
        {
            try
            {
                var factory = session.getRequestFactory();
                var timeFrame = factory.Timeframes[tf.ToLowerInvariant()];
                timeFrame = timeFrame ?? factory.Timeframes[tf.ToUpperInvariant()];
                if (timeFrame == null)
                {
                    var message = "GetHistoryPrices. Cannot create a request. Timeframe is incorrect!";
                    message += $"{Environment.NewLine}Instrument: {instrument}";
                    message += $"{Environment.NewLine}Timeframe: {tf}";
                    message += $"{Environment.NewLine}Depth: {depth}";
                    Logger.Log.Error(message);
                    return null;
                }

                var request = factory.createMarketDataSnapshotRequestInstrument(instrument, timeFrame, depth);

                return request;
            }
            catch (Exception e)
            {
                var message = "GetHistoryPrices. Cannot create a request.";
                message += $"{Environment.NewLine}Instrument: {instrument}";
                message += $"{Environment.NewLine}Timeframe: {tf}";
                message += $"{Environment.NewLine}Depth: {depth}";
                message += $"{Environment.NewLine}Exception: {e}";
                Logger.Log.Error(message);
                return null;
            }
        }

        public async Task<O2GRequest> CreateRequest(O2GSession session, O2GValueMap valueMap)
        {
            var requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            var request = await CreateRequest(requestFactory, valueMap).ConfigureAwait(false);
            return request;
        }

        public async Task<O2GValueMap> CreateSetSubscriptionStatusRequestBatchHeader(O2GSession session)
        {
            var requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            var valueMap = requestFactory.createValueMap();
            valueMap.setString(O2GRequestParamsEnum.Command, Constants.Commands.SetSubscriptionStatus);
            return valueMap;
        }

        public async Task<O2GRequest> CreateTrueMarketOrderRequest(O2GSession session, string offerId, string accountId, int amount, string buySellValue, 
            string token, double minRate, double maxRate)
        {
            var requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            var valueMap = requestFactory.createValueMap();
            valueMap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valueMap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.MarketOpenRange);
            valueMap.setString(O2GRequestParamsEnum.AccountID, accountId);
            valueMap.setString(O2GRequestParamsEnum.OfferID, offerId);
            valueMap.setString(O2GRequestParamsEnum.BuySell, buySellValue);
            valueMap.setInt(O2GRequestParamsEnum.Amount, amount);
            valueMap.setString(O2GRequestParamsEnum.CustomID, token);
            valueMap.setDouble(O2GRequestParamsEnum.RateMin, minRate);
            valueMap.setDouble(O2GRequestParamsEnum.RateMax, maxRate);

            var request = await CreateRequest(requestFactory, valueMap).ConfigureAwait(false);
            return request;
        }

        public async Task<IList<PriceBar>> GetPriceBarListFromResponse(O2GSession session, O2GResponse response)
        {
            var priceBars = new List<PriceBar>();
            var readerFactory = session.getResponseReaderFactory();
            if (readerFactory == null)
            {
                return priceBars;
            }

            var reader = readerFactory.createMarketDataSnapshotReader(response);
            if (!reader.isBar || reader.Count == 0)
            {
                return priceBars;
            }

            for (var ii = reader.Count - 1; ii >= 0; ii--)
            {
                var bar = new PriceBar
                {
                    Time = reader.getDate(ii),
                    Volume = reader.getVolume(ii),
                    BidClose = (decimal)reader.getBidClose(ii),
                    BidOpen = (decimal)reader.getBidOpen(ii),
                    BidHigh = (decimal)reader.getBidHigh(ii),
                    BidLow = (decimal)reader.getBidLow(ii),
                    AskClose = (decimal)reader.getAskClose(ii),
                    AskOpen = (decimal)reader.getAskOpen(ii),
                    AskHigh = (decimal)reader.getAskHigh(ii),
                    AskLow = (decimal)reader.getAskLow(ii),
                };

                priceBars.Add(bar);
            }

            return priceBars;
        }

        public async Task UpdateHistoryPrices(O2GSession session, O2GRequest request, DateTime dtFrom, DateTime dtTo)
        {
            var factory = session.getRequestFactory();
            factory.fillMarketDataSnapshotRequestTime(request, dtFrom, dtTo, false);
        }

        private async Task<O2GRequest> CreateRequest(O2GRequestFactory requestFactory, O2GValueMap valueMap)
        {
            var request = requestFactory.createOrderRequest(valueMap);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }

            return request;
        }
    }
}
