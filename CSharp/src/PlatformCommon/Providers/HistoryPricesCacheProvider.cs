using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Adapters;
using Common.Models;

namespace PlatformCommon.Providers
{
    public class HistoryPricesCacheProvider : IHistoryPricesCacheProvider
    {
        private readonly IHistoryPricesProvider _historyPricesProvider;
        private readonly ITradeAdapter _tradeAdapter;
        private readonly Dictionary<string, IList<PriceBarForStore>> _instrumentHistoryPrices = new Dictionary<string, IList<PriceBarForStore>>();
        private readonly Dictionary<TimeFrame, DateTime> _timeFramesExpiration = new Dictionary<TimeFrame, DateTime>();

        public IReadOnlyDictionary<TimeFrame, DateTime> TimeframesExpiration => _timeFramesExpiration;

        public HistoryPricesCacheProvider(IHistoryPricesProvider historyPricesProvider, ITradeAdapter tradeAdapter)
        {
            _historyPricesProvider = historyPricesProvider;
            _tradeAdapter = tradeAdapter;
        }

        public async Task InitializeInstrumentHistoryPrices(string historyKey, TimeFrame tf)
        {
            var now = DateTime.MinValue;
            if (!_timeFramesExpiration.ContainsKey(tf))
            {
                _timeFramesExpiration.Add(tf, now);
            }

            await AddOrUpdateValue(historyKey, null).ConfigureAwait(false);
        }

        public async Task AddOrUpdateValue(string key, IList<PriceBarForStore> value)
        {
            if (!_instrumentHistoryPrices.ContainsKey(key))
            {
                _instrumentHistoryPrices.Add(key, value);
                return;
            }
            _instrumentHistoryPrices[key] = value;
        }

        public async Task<IList<PriceBarForStore>> GetPrices(string historyKey, string instrument, TimeFrame tf,
            int barsCount, DateTime now)
        {
            var expiration = _timeFramesExpiration[tf];
            var prices = _instrumentHistoryPrices[historyKey];
            if (prices == null || prices.Count < barsCount || (expiration < now && prices[0].Time < expiration))
            {
                prices = await _historyPricesProvider.RequestPrices(instrument, tf, barsCount, now).ConfigureAwait(false);
            }
            else
            {
                return prices.Take(barsCount).ToList();
            }
            if (prices == null || prices.Count == 0)
            {
                return prices;
            }
            await AddOrUpdateValue(historyKey, prices).ConfigureAwait(false);
            _timeFramesExpiration[tf] = await GetPricesExpiration(prices, tf).ConfigureAwait(false);
            return prices.Take(barsCount).ToList();
        }

        public async Task<IList<PriceBarForStore>> GetPricesFromCache(string historyKey, TimeFrame tf, int barsCount, DateTime now)
        {
            var expiration = _timeFramesExpiration[tf];
            var prices = _instrumentHistoryPrices[historyKey];
            if (prices == null || prices.Count < barsCount || (expiration < now && prices[0].Time < expiration))
            {
                return new List<PriceBarForStore>();
            }
            else
            {
                return prices.Take(barsCount).ToList();
            }
        }

        public async Task<DateTime> GetPricesExpiration(IList<PriceBarForStore> prices, TimeFrame tf)
        {
            var seconds = await _tradeAdapter.GetTimeFrameSeconds(tf).ConfigureAwait(false);
            var expiration = prices[0].Time.AddSeconds(seconds);
            return expiration;
        }
    }
}
