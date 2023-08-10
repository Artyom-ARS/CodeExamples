using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Logging;
using Common.Models;
using FxConnect.Models;
using OnlinePlatform.Models;

namespace OnlinePlatform.Providers
{
    public class PriceUpdateSuspenseProvider : IPriceUpdateSuspenseProvider
    {
        private readonly ConcurrentDictionary<string, PriceSuspense> _currentPriceTimestamp = new ConcurrentDictionary<string, PriceSuspense>();

        public async Task<bool> ShouldWait(Dictionary<string, PriceUpdateSuspenseParameters> priceUpdateSuspense, DTOPriceUpdate priceUpdate)
        {
            string log;
            var instrument = priceUpdate.Instrument;
            PriceSuspense currentPriceTimestamp;
            currentPriceTimestamp.Time = DateTime.UtcNow;
            currentPriceTimestamp.Price = (decimal)priceUpdate.Bid;

            if (priceUpdateSuspense == null)
            {
                log = "PriceUpdateSuspenseProvider. No price update parameters.";
                log += $"{Environment.NewLine}Instrument: {instrument}";
                Logger.Log.Debug(log);
                return false;
            }

            var priceUpdateParameters = priceUpdateSuspense.ContainsKey(instrument) ? priceUpdateSuspense[instrument] : new PriceUpdateSuspenseParameters();

            if (priceUpdateParameters.Price <= 0.0m && priceUpdateParameters.Milliseconds <= 0)
            {
                log = "PriceUpdateSuspenseProvider. No price update parameters.";
                log += $"{Environment.NewLine}Instrument: {instrument}";
                Logger.Log.Debug(log);
                return false;
            }

            if (!_currentPriceTimestamp.ContainsKey(instrument))
            {
                log = "PriceUpdateSuspenseProvider. No price update timestamp.";
                log += $"{Environment.NewLine}Instrument: {instrument}";
                Logger.Log.Debug(log);
                _currentPriceTimestamp.AddOrUpdate(instrument, currentPriceTimestamp, (key, value) => currentPriceTimestamp);
                return false;
            }

            var _lastPriceTimestamp = _currentPriceTimestamp[instrument];

            if (priceUpdateParameters.Milliseconds > 0 && currentPriceTimestamp.Time > _lastPriceTimestamp.Time.AddMilliseconds(priceUpdateParameters.Milliseconds))
            {
                log = "PriceUpdateSuspenseProvider. Go forward by time.";
                log += $"{Environment.NewLine}Instrument: {instrument}";
                log += $"{Environment.NewLine}Parameters.Price: {priceUpdateParameters.Price}";
                log += $"{Environment.NewLine}Parameters.Milliseconds: {priceUpdateParameters.Milliseconds}";
                log += $"{Environment.NewLine}Current.Time: {currentPriceTimestamp.Time}";
                log += $"{Environment.NewLine}Last.Time: {_lastPriceTimestamp.Time}";
                log += $"{Environment.NewLine}Current.Price: {currentPriceTimestamp.Price}";
                log += $"{Environment.NewLine}Last.Price: {_lastPriceTimestamp.Price}";
                Logger.Log.Debug(log);
                _currentPriceTimestamp[instrument] = currentPriceTimestamp;
                return false;
            }

            if (!(priceUpdateParameters.Price > 0.0m && Math.Abs(currentPriceTimestamp.Price - _lastPriceTimestamp.Price) > priceUpdateParameters.Price))
            {
                log = "PriceUpdateSuspenseProvider. Go forward by price.";
                log += $"{Environment.NewLine}Instrument: {instrument}";
                log += $"{Environment.NewLine}Parameters.Price: {priceUpdateParameters.Price}";
                log += $"{Environment.NewLine}Parameters.Milliseconds: {priceUpdateParameters.Milliseconds}";
                log += $"{Environment.NewLine}Current.Time: {currentPriceTimestamp.Time}";
                log += $"{Environment.NewLine}Last.Time: {_lastPriceTimestamp.Time}";
                log += $"{Environment.NewLine}Current.Price: {currentPriceTimestamp.Price}";
                log += $"{Environment.NewLine}Last.Price: {_lastPriceTimestamp.Price}";
                Logger.Log.Debug(log);
                _currentPriceTimestamp[instrument] = currentPriceTimestamp;
                return true;
            }

            log = "PriceUpdateSuspenseProvider. Suspense.";
            log += $"{Environment.NewLine}Instrument: {instrument}";
            log += $"{Environment.NewLine}Parameters.Price: {priceUpdateParameters.Price}";
            log += $"{Environment.NewLine}Parameters.Milliseconds: {priceUpdateParameters.Milliseconds}";
            log += $"{Environment.NewLine}Current.Time: {currentPriceTimestamp.Time}";
            log += $"{Environment.NewLine}Last.Time: {_lastPriceTimestamp.Time}";
            log += $"{Environment.NewLine}Current.Price: {currentPriceTimestamp.Price}";
            log += $"{Environment.NewLine}Last.Price: {_lastPriceTimestamp.Price}";
            Logger.Log.Debug(log);
            return false;

        }
    }
}
