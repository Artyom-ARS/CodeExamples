using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Models;
using PlatformCommon.Providers;

namespace HistoryExport.Providers
{
    public class SavePriceHistoryProvider : ISavePriceHistoryProvider
    {
        private readonly ISaveToCsvProvider _saveToCsvProvider;

        public SavePriceHistoryProvider(ISaveToCsvProvider saveToCsvProvider)
        {
            _saveToCsvProvider = saveToCsvProvider;
        }

        public async Task<bool> SaveToCsv(string folderPath, string fileName, List<PriceBarForStore> priceBarList)
        {
            var exists = Directory.Exists(folderPath);

            if (!exists)
            {
                Directory.CreateDirectory(folderPath);
            }

            var (stream, tw) = _saveToCsvProvider.OpenFileToWrite($"{folderPath}\\{fileName}");
            foreach (var item in priceBarList)
            {
                var row = await BuildCsvRow(item, 0).ConfigureAwait(false);
                await _saveToCsvProvider.WriteRow(tw, row).ConfigureAwait(false);
            }

            await _saveToCsvProvider.CloseFile(stream, tw).ConfigureAwait(false);

            return true;
        }

        private async Task<string> BuildCsvRow(PriceBarForStore item, int addHours)
        {
            var row =
                $"{item.Time.AddHours(addHours):yyyy.MM.dd};" +
                $"{item.Time.AddHours(addHours):HH:mm};" +
                $"{item.BidOpen};" +
                $"{item.BidHigh};" +
                $"{item.BidLow};" +
                $"{item.BidClose};" +
                $"{item.Volume}";

            return row;
        }
    }
}
