using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace Common.Adapters
{
    public interface IBarsHistoryAdapter
    {
        Task<string> GetPriceBarKey(PriceBarsHistoryKey valueFromJsonHistory);

        Task<(string, TimeFrame, int)> GetParametersFromKey(KeyValuePair<string, List<PriceBarForStore>> prBarsForStore);

        Task<string> GetInstrumentHistoryPricesKey(string instrument, TimeFrame tf);
    }
}
