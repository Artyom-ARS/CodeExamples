using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using HistoryStorage.Models;

namespace HistoryStorage.Providers
{
    public interface ISavePricesProvider
    {
        Task Save(HistoryConfigurationParameters configuration, PriceConfiguration priceConfiguration, InstrumentForStoreBase instrumentToSave, IList<PriceBarForStore> prices, IList<PriceBarForStore> oldPrices, int year);

        Task Save(HistoryConfigurationParameters configuration, PriceConfiguration priceConfiguration, InstrumentForStoreBase instrumentToSave, IList<PriceBarForStore> prices);
    }
}
