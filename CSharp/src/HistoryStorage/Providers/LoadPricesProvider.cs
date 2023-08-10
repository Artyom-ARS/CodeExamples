using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common.Adapters;
using Common.Models;

namespace HistoryStorage.Providers
{
    public class LoadPricesProvider : ILoadPricesProvider
    {
        private readonly IPriceConfigurationAdapter _priceConfigurationAdapter;
        private readonly IPriceFileFormatAdapter _priceFileFormatAdapter;
        private readonly IAzureStorage _azureStorage;

        public LoadPricesProvider(IPriceConfigurationAdapter priceConfigurationAdapter, IPriceFileFormatAdapter priceFileFormatAdapter, IAzureStorage azureStorage)
        {
            _priceConfigurationAdapter = priceConfigurationAdapter;
            _priceFileFormatAdapter = priceFileFormatAdapter;
            _azureStorage = azureStorage;
        }

        public async Task<(DateTime, IList<PriceBarForStore>)> GetLastDateAndPricesFromStorage(bool saveToStorage, DateTime dtFrom, DateTime dtTo, string instrument, string timeFrameName)
        {
            if (dtFrom.Year != dtTo.Year || !saveToStorage)
            {
                return (dtFrom, null);
            }

            var year = dtFrom.Year;
            var filePath = await _priceConfigurationAdapter.GetStorageFilePath(instrument, timeFrameName, year).ConfigureAwait(false);

            var instrumentData = await _azureStorage.GetPriceDataAsStream(filePath, null).ConfigureAwait(false);
            if (instrumentData == null)
            {
                return (dtFrom, null);
            }

            DateTime lastDateInStorage;
            List<PriceBarForStore> priceList;
            using (var reader = new BinaryReader(instrumentData))
            {
                var _ = await _priceFileFormatAdapter.ReadInstrument(reader).ConfigureAwait(false);
                priceList = await _priceFileFormatAdapter.ReadPriceBars(reader).ConfigureAwait(false);
                lastDateInStorage = priceList.Max(x => x.Time);
            }

            DateTime resultDate = lastDateInStorage > dtFrom ? lastDateInStorage.AddMinutes(1) : dtFrom;
            return (resultDate, priceList);
        }
    }
}
