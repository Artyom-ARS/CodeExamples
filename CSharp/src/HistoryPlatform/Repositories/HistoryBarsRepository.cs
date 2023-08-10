using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Adapters;
using Common.Models;
using Common.Providers;
using HistoryPlatform.Models;

namespace HistoryPlatform.Repositories
{
    public class HistoryBarsRepository : IHistoryBarsRepository
    {
        private readonly Dictionary<string, List<PriceBarForStore>> _priceHistoryCache = new Dictionary<string, List<PriceBarForStore>>();
        private readonly ConcurrentDictionary<string, (Dictionary<TimeFrame, DateTime>, Dictionary<string, int>, Dictionary<string, IList<PriceBarForStore>>)> _tupleDictionaries =
            new ConcurrentDictionary<string, (Dictionary<TimeFrame, DateTime>, Dictionary<string, int>, Dictionary<string, IList<PriceBarForStore>>)>();

        private readonly IConfigurationProvider _configurationProvider;
        private readonly IPriceRepository _priceRepository;
        private readonly IBarsHistoryAdapter _barsHistoryAdapter;
        private readonly ITradeAdapter _tradeAdapter;
        private readonly IPriceConfigurationAdapter _priceConfigurationAdapter;

        public HistoryBarsRepository(
            IPriceRepository priceRepository,
            IConfigurationProvider configurationProvider,
            IBarsHistoryAdapter barsHistoryAdapter,
            ITradeAdapter tradeAdapter,
            IPriceConfigurationAdapter priceConfigurationAdapter)
        {
            _configurationProvider = configurationProvider;
            _priceRepository = priceRepository;
            _barsHistoryAdapter = barsHistoryAdapter;
            _tradeAdapter = tradeAdapter;
            _priceConfigurationAdapter = priceConfigurationAdapter;
        }

        public async Task FillHistoryByInstrumentAndTimeFrame()
        {
            _priceHistoryCache.Clear();
            var configuration = _configurationProvider.GetConfiguration<HistoryConfigurationParameters>();
            var historyBarsConfiguration = _configurationProvider.GetConfiguration<HistoryBarsCount>();
            var priceConfigurationItems = configuration.PriceConfiguration;
            var historyItems = historyBarsConfiguration.History;
            foreach (var historyItem in historyItems)
            {
                var priceConfigurationItem = priceConfigurationItems.FirstOrDefault(x => x.Instrument == historyItem.Instrument
                    && x.TimeFrame.ToLower() == historyItem.TimeFrame.ToString().ToLower());
                if (priceConfigurationItem == null)
                {
                    continue;
                }

                var dtFrom = await _priceConfigurationAdapter.GetFromDateByPriceConfiguration(priceConfigurationItem.From, priceConfigurationItem.Year, priceConfigurationItem.PastDays).ConfigureAwait(false);
                var dtTo = await _priceConfigurationAdapter.GetToDateByPriceConfiguration(priceConfigurationItem.To, priceConfigurationItem.Year).ConfigureAwait(false);
                var dataSetForInstrument = await _priceRepository.Get(historyItem.Instrument, historyItem.TimeFrame, dtFrom, dtTo).ConfigureAwait(false);
                if (dataSetForInstrument.PriceBarList.Count == 0)
                {
                    continue;
                }

                var getPriceBarKey = await _barsHistoryAdapter.GetPriceBarKey(historyItem).ConfigureAwait(false);

                if (_priceHistoryCache.ContainsKey(getPriceBarKey))
                {
                    continue;
                }

                _priceHistoryCache.Add(getPriceBarKey, new List<PriceBarForStore>(dataSetForInstrument.PriceBarList.OrderByDescending(x => x.Time)));
            }
        }

        public async Task<string> InitTimeFrame()
        {
            var timeFrameExpiration = new Dictionary<TimeFrame, DateTime>();
            var currentBarIndex = new Dictionary<string, int>();
            var priceHistoryCacheByKey = new Dictionary<string, IList<PriceBarForStore>>();
            var newId = Guid.NewGuid().ToString();
            foreach (var allHistoryBars in _priceHistoryCache)
            {
                var (_, timeFrame, _) = await _barsHistoryAdapter.GetParametersFromKey(allHistoryBars).ConfigureAwait(false);
                timeFrameExpiration[timeFrame] = DateTime.MinValue;
                currentBarIndex[allHistoryBars.Key] = allHistoryBars.Value.Count - 1;
            }

            var tuple = (timeFrameExpiration, currentBarIndex, priceHistoryCacheByKey);
            _tupleDictionaries[newId] = tuple;
            return newId;
        }

        public async Task GetLastHistoryBars(DateTime currentTime, string historyId)
        {
            var (timeFrameExpiration, currentBarIndexKey, priceHistoryCacheByKey) = _tupleDictionaries[historyId];

            foreach (var allHistoryBars in _priceHistoryCache)
            {
                var filteredTime = new List<PriceBarForStore>();
                var (_, timeFrame, barsCount) = await _barsHistoryAdapter.GetParametersFromKey(allHistoryBars).ConfigureAwait(false);
                if (currentTime < timeFrameExpiration[timeFrame])
                {
                    continue;
                }

                var currentBarIndex = currentBarIndexKey[allHistoryBars.Key];
                var priceBarTime = currentBarIndex <= 0 ? currentTime : allHistoryBars.Value[currentBarIndex - 1].Time;
                while (priceBarTime < currentTime && currentBarIndex > 0)
                {
                    currentBarIndex--;
                    priceBarTime = currentBarIndex <= 0 ? currentTime : allHistoryBars.Value[currentBarIndex - 1].Time;
                }

                var borderIndex = await GetHistoryBarsBorderIndex(currentBarIndex, barsCount, allHistoryBars.Value.Count).ConfigureAwait(false);

                await FillListByFilteredTime(currentBarIndex, borderIndex, filteredTime, allHistoryBars).ConfigureAwait(false);
                currentBarIndexKey[allHistoryBars.Key] = currentBarIndex;

                priceHistoryCacheByKey[allHistoryBars.Key] = filteredTime;
            }

            foreach (var allHistoryBars in _priceHistoryCache)
            {
                var (_, timeFrame, _) = await _barsHistoryAdapter.GetParametersFromKey(allHistoryBars).ConfigureAwait(false);
                if (currentTime < timeFrameExpiration[timeFrame])
                {
                    continue;
                }

                timeFrameExpiration[timeFrame] = await GetPricesExpiration(currentTime, timeFrame).ConfigureAwait(false);
            }

            var tuple = (timeFrameExpiration, currentBarIndexKey, priceHistoryCacheByKey);
            _tupleDictionaries[historyId] = tuple;
        }

        public async Task<Dictionary<string, IList<PriceBarForStore>>> GetpriceHistoryCacheByKey(string historyId)
        {
            var tupleValues = _tupleDictionaries[historyId];
            var priceHistoryCacheByKey = tupleValues.Item3;
            return priceHistoryCacheByKey;
        }

        private async Task FillListByFilteredTime(
            int currentBarIndex,
            int valueLimitedByArray,
            List<PriceBarForStore> filteredTime,
            KeyValuePair<string, List<PriceBarForStore>> allHistoryBars)
        {
            for (var j = currentBarIndex; j <= valueLimitedByArray - 1; j++)
            {
                filteredTime.Add(allHistoryBars.Value[j]);
            }

            if (filteredTime.Count == 0)
            {
                return;
            }

            var firstBar = filteredTime.First();

            var newFirstBar = new PriceBarForStore
            {
                BidOpen = firstBar.BidOpen,
                Spread = firstBar.Spread,
                Volume = 0,
                Time = firstBar.Time,
            };
            newFirstBar.BidLow = newFirstBar.BidOpen;
            newFirstBar.BidHigh = newFirstBar.BidOpen;
            newFirstBar.BidClose = newFirstBar.BidOpen;
            filteredTime.RemoveAt(0);
            filteredTime.Insert(0, newFirstBar);
        }

        private async Task<int> GetHistoryBarsBorderIndex(
            int currentBarIndex,
            int barsCount,
            int countOfElementsInDict)
        {
            var valueLimitedByArray = currentBarIndex + barsCount > countOfElementsInDict
                ? countOfElementsInDict
                : currentBarIndex + barsCount;

            return valueLimitedByArray;
        }

        private async Task<DateTime> GetPricesExpiration(DateTime currentTime, TimeFrame tf)
        {
            var seconds = await _tradeAdapter.GetTimeFrameSeconds(tf).ConfigureAwait(false);
            var dayStart = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day);
            var totalSeconds = (currentTime - dayStart).TotalSeconds;
            var expiration = currentTime.AddSeconds(seconds - (totalSeconds % seconds));
            return expiration;
        }
    }
}
