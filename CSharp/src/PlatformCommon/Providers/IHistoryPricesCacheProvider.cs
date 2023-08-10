using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace PlatformCommon.Providers
{
    public interface IHistoryPricesCacheProvider
    {
        IReadOnlyDictionary<TimeFrame, DateTime> TimeframesExpiration { get; }
        Task AddOrUpdateValue(string key, IList<PriceBarForStore> value);
        Task InitializeInstrumentHistoryPrices(string historyKey, TimeFrame tf);
        Task<IList<PriceBarForStore>> GetPrices(string historyKey, string instrument, TimeFrame tf, int depth, DateTime now);
        Task<IList<PriceBarForStore>> GetPricesFromCache(string historyKey, TimeFrame tf, int barsCount, DateTime now);
        Task<DateTime> GetPricesExpiration(IList<PriceBarForStore> prices, TimeFrame tf);
    }
}