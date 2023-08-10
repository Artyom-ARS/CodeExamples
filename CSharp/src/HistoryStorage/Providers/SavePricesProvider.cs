using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common.Adapters;
using Common.Facades;
using Common.Models;
using HistoryStorage.Models;

namespace HistoryStorage.Providers
{
    public class SavePricesProvider : ISavePricesProvider
    {
        private readonly IAzureStorage _azureStorage;
        private readonly IPriceConfigurationAdapter _priceConfigurationAdapter;
        private readonly IPriceFileFormatAdapter _priceFileFormatAdapter;
        private readonly IConsole _console;

        public SavePricesProvider(IAzureStorage azureStorage, IPriceConfigurationAdapter priceConfigurationAdapter,
            PriceFileFormatAdapter priceFileFormatAdapter, IConsole console)
        {
            _azureStorage = azureStorage;
            _priceConfigurationAdapter = priceConfigurationAdapter;
            _priceFileFormatAdapter = priceFileFormatAdapter;
            _console = console;
        }

        public async Task Save(HistoryConfigurationParameters configuration, PriceConfiguration priceConfiguration,
            InstrumentForStoreBase instrumentToSave, IList<PriceBarForStore> prices)
        {
            if (configuration.SaveToStorage)
            {
                var timeFrameName = priceConfiguration.TimeFrame;
                var instrument = priceConfiguration.Instrument;
                instrumentToSave.TimeFrame = timeFrameName;
                var groupedByYear = prices.GroupBy(
                    item => item.Time.Year,
                    (key, group) => new { Year = key, Prices = group });
                Parallel.ForEach(groupedByYear, async (grouped) =>
                {
                    var priceListSorted = grouped.Prices.OrderBy(x => x.Time);
                    instrumentToSave.FromTime = priceListSorted.FirstOrDefault().Time;
                    instrumentToSave.ToTime = priceListSorted.LastOrDefault().Time;
                    var filePath = await _priceConfigurationAdapter.GetStorageFilePath(instrument, timeFrameName, grouped.Year).ConfigureAwait(false);
                    using (var stream = new MemoryStream())
                    {
                        using (var bw = new BinaryWriter(stream))
                        {
                            await _priceFileFormatAdapter.Write(instrumentToSave, bw).ConfigureAwait(false);
                            foreach (var priceBar in priceListSorted)
                            {
                                await _priceFileFormatAdapter.Write(priceBar, bw).ConfigureAwait(false);
                            }
                        }

                        await _azureStorage.UploadData(filePath, stream).ConfigureAwait(false);
                    }

                    await LoadPricesFromStorage(filePath).ConfigureAwait(false);
                });
            }
        }

        public async Task Save(HistoryConfigurationParameters configuration, PriceConfiguration priceConfiguration,
            InstrumentForStoreBase instrumentToSave, IList<PriceBarForStore> prices, IList<PriceBarForStore> oldPrices, int year)
        {
            if (configuration.SaveToStorage)
            {
                var timeFrameName = priceConfiguration.TimeFrame;
                var instrument = priceConfiguration.Instrument;
                instrumentToSave.TimeFrame = timeFrameName;
                var allPrices = new List<PriceBarForStore>(oldPrices);
                allPrices.AddRange(prices);
                var priceListSorted = allPrices.OrderBy(x => x.Time);
                instrumentToSave.FromTime = priceListSorted.FirstOrDefault().Time;
                instrumentToSave.ToTime = priceListSorted.LastOrDefault().Time;
                var filePath = await _priceConfigurationAdapter.GetStorageFilePath(instrument, timeFrameName, year).ConfigureAwait(false);
                using (var stream = new MemoryStream())
                {
                    using (var bw = new BinaryWriter(stream))
                    {
                        await _priceFileFormatAdapter.Write(instrumentToSave, bw).ConfigureAwait(false);
                        foreach (var priceBar in priceListSorted)
                        {
                            await _priceFileFormatAdapter.Write(priceBar, bw).ConfigureAwait(false);
                        }
                    }

                    await _azureStorage.UploadData(filePath, stream).ConfigureAwait(false);
                }

                await LoadPricesFromStorage(filePath).ConfigureAwait(false);
            }
        }

        private async Task LoadPricesFromStorage(string fileName)
        {
            InstrumentForStore instrument = null;
            var instrumentData = await _azureStorage.GetPriceDataAsBytes(fileName).ConfigureAwait(false);
            if (instrumentData != null)
            {
                using (var stream = new MemoryStream(instrumentData))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        instrument = await _priceFileFormatAdapter.ReadInstrument(reader).ConfigureAwait(false);
                        var priceList = await _priceFileFormatAdapter.ReadPriceBars(reader).ConfigureAwait(false);
                        instrument.PriceBarList = priceList;
                        await DisplayInstrumentInfo(instrument, priceList).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task DisplayInstrumentInfo(InstrumentForStoreBase instrument, IReadOnlyCollection<PriceBarForStore> priceList)
        {
            _console.WriteLine($"Instrument: {instrument.Instrument}, " +
                                    $"TimeFrame: {instrument.TimeFrame}, " +
                                    $"PointSize: {instrument.PointSize}");
            _console.WriteLine($"Start bar time: {priceList.FirstOrDefault().Time}, " +
                                    $"Spread: {priceList.FirstOrDefault().Spread}, " +
                                    $"Bid open: {priceList.FirstOrDefault().BidOpen}");
            _console.WriteLine($"End bar time: {priceList.LastOrDefault().Time}, " +
                                    $"Spread: {priceList.LastOrDefault().Spread}, " +
                                    $"Bid close: {priceList.LastOrDefault().BidClose}");
        }
    }
}
