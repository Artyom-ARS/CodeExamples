using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common.Adapters;
using Common.Models;

namespace PlatformCommon.Providers
{
    public class DataStorageProvider : IDataStorageProvider
    {
        private readonly IPriceConfigurationAdapter _priceConfigurationAdapter;
        private readonly IPricesStoreProvider _priceStoreProvider;
        private readonly IAzureStorage _azureStorage;

        public DataStorageProvider(
            IPriceConfigurationAdapter priceConfigurationAdapter,
            IPricesStoreProvider priceStoreProvider,
            IAzureStorage azureStorage)
        {
            _priceConfigurationAdapter = priceConfigurationAdapter;
            _priceStoreProvider = priceStoreProvider;
            _azureStorage = azureStorage;
        }

        public async Task<InstrumentForStore> BuildInstrumentFromFile(
            DateTime dtTo,
            string path,
            string instrument,
            string timeFrameName,
            DateTime dtFrom)
        {
            var year = dtTo.Year;
            InstrumentForStore dataFromFile;
            var priceBarList = new List<PriceBarForStore>();
            do
            {
                var filePathDisk = await _priceConfigurationAdapter.GetFilePath(path, instrument, timeFrameName, year).ConfigureAwait(false);
                dataFromFile = await _priceStoreProvider.LoadPricesFromFile(filePathDisk).ConfigureAwait(false);
                if (dataFromFile != null)
                {
                    priceBarList.AddRange(dataFromFile.PriceBarList);
                }

                year--;
            }
            while (dtFrom.Year <= year);
            dataFromFile.PriceBarList = priceBarList;
            return dataFromFile;
        }

        public async Task<InstrumentForStore> BuildInstrumentFromStorage(
            DateTime dtTo,
            string path,
            string instrument,
            string timeFrameName,
            DateTime dtFrom)
        {
            var year = dtTo.Year;
            InstrumentForStore dataFromStorage;
            var priceBarList = new List<PriceBarForStore>();
            do
            {
                var filePathDisk = await _priceConfigurationAdapter.GetFilePath(path, instrument, timeFrameName, year).ConfigureAwait(false);
                var fileName = await _priceConfigurationAdapter.GetStorageFilePath(instrument, timeFrameName, year).ConfigureAwait(false);
                var blob = _azureStorage.GetBlob(fileName);
                var blobLocalDateTime = _azureStorage.GetBlobAttributes(blob);
                var fileTime = File.GetCreationTime(filePathDisk);

                if (File.Exists(filePathDisk) & blobLocalDateTime == fileTime)
                {
                    dataFromStorage = await _priceStoreProvider.LoadPricesFromFile(filePathDisk).ConfigureAwait(false);
                }
                else
                {
                    dataFromStorage = await _priceStoreProvider.LoadPricesFromStorage(fileName, filePathDisk).ConfigureAwait(false);
                }

                if (dataFromStorage != null)
                {
                    priceBarList.AddRange(dataFromStorage.PriceBarList);
                }

                year--;
            }
            while (dtFrom.Year <= year);
            dataFromStorage.PriceBarList = priceBarList.Where(x => x.Time.Date < dtTo && x.Time.Date > dtFrom).OrderBy(x => x.Time.Date).ToList();
            dataFromStorage.FromTime = dataFromStorage.PriceBarList.First().Time;
            dataFromStorage.ToTime = dataFromStorage.PriceBarList.Last().Time;
            return dataFromStorage;
        }
    }
}
