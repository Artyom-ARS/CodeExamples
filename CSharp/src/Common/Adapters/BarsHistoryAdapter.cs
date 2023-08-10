using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace Common.Adapters
{
    public class BarsHistoryAdapter : IBarsHistoryAdapter
    {
        public async Task<string> GetPriceBarKey(PriceBarsHistoryKey priceBarsHistoryKey)
        {
            return priceBarsHistoryKey.Instrument + "-" + priceBarsHistoryKey.TimeFrame + "-" + priceBarsHistoryKey.BarsCount;
        }

        public async Task<(string, TimeFrame, int)> GetParametersFromKey(KeyValuePair<string, List<PriceBarForStore>> prBarsForStore)
        {
            var parts = prBarsForStore.Key.Split('-');
            var instrument = parts[0];
            Enum.TryParse(parts[1].ToLower(), out TimeFrame timeFrame);
            int.TryParse(parts[2], out int barsCount);

            return (instrument, timeFrame, barsCount);
        }

        public async Task<string> GetInstrumentHistoryPricesKey(string instrument, TimeFrame tf)
        {
            return $"{instrument}-{tf}";
        }
    }
}
