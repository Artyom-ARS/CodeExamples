using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common.Adapters;
using Common.Facades;
using Common.Models;

namespace PlatformCommon.Providers
{
    public class PricesStoreProvider : IPricesStoreProvider
    {
        private readonly IConsole _console;
        private readonly IPriceFileFormatAdapter _priceFileFormatAdapter;
        private readonly IAzureStorage _azureStorage;

        public PricesStoreProvider(IConsole console, IPriceFileFormatAdapter priceFileFormatAdapter, IAzureStorage azureStorage)
        {
            _console = console;
            _priceFileFormatAdapter = priceFileFormatAdapter;
            _azureStorage = azureStorage;
        }

        public async Task<InstrumentForStore> LoadPricesFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _console.WriteLine("File doesn't exist {0}!", filePath);
                return null;
            }

            var instrumentData = File.Open(filePath, FileMode.Open);

            InstrumentForStore instrument;
            using (var reader = new BinaryReader(instrumentData))
            {
                instrument = await GetInstrument(reader).ConfigureAwait(false);
            }

            return instrument;
        }

        public async Task<InstrumentForStore> LoadPricesFromStorage(string fileName, string filePathDisk)
        {
            var instrumentData = await _azureStorage.GetPriceDataAsStream(fileName, filePathDisk).ConfigureAwait(false);
            if (instrumentData == null) return null;

            InstrumentForStore instrument;
            using (var reader = new BinaryReader(instrumentData))
            {
                instrument = await GetInstrument(reader).ConfigureAwait(false);
            }

            return instrument;
        }

        private async Task<InstrumentForStore> GetInstrument(BinaryReader reader)
        {
            var instrument = await _priceFileFormatAdapter.ReadInstrument(reader).ConfigureAwait(false);

            var priceList = await _priceFileFormatAdapter.ReadPriceBars(reader).ConfigureAwait(false);
            instrument.PriceBarList = priceList;

            await DisplayInstrumentInfo(instrument, priceList).ConfigureAwait(false);
            return instrument;
        }

        private async Task DisplayInstrumentInfo(InstrumentForStore instrument, List<PriceBarForStore> priceList)
        {
            _console.WriteLine(string.Empty);
            _console.WriteLine($"Instrument: {instrument.Instrument}, TimeFrame: {instrument.TimeFrame}, PointSize: {instrument.PointSize}");
            _console.WriteLine(
                "Start bar time: {0}, Spread: {1}, Bid open: {2}",
                priceList.FirstOrDefault().Time,
                Math.Round(priceList.FirstOrDefault().Spread, instrument.Digits),
                priceList.FirstOrDefault().BidOpen);
            _console.WriteLine(
                "End bar time: {0}, Spread: {1}, Bid close: {2}",
                priceList.LastOrDefault().Time,
                Math.Round(priceList.LastOrDefault().Spread, instrument.Digits),
                priceList.LastOrDefault().BidClose);

            var hl = priceList.Select(item => item.BidHigh - item.BidLow);
            var avgHl = Math.Round(hl.Average(), instrument.Digits);
            var maxHl = Math.Round(hl.Max(), instrument.Digits);
            var bigBars = priceList.Where(x => x.BidHigh - x.BidLow > maxHl / 4);

            var bigBarsCount = bigBars.Count();
            _console.WriteLine($"Max HL {maxHl}, Avg HL {avgHl}, Big bars count {bigBarsCount} / {priceList.Count}");
        }
    }
}
