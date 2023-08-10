using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Adapters;
using Common.Constants;
using Common.Facades;
using Common.Logging;
using Common.Models;
using Common.Providers;
using HistoryExport.Models;
using HistoryExport.Providers;
using PlatformCommon.Providers;

namespace HistoryExport.Services
{
    public class HistoryExportService : IHistoryExportService
    {
        private readonly IConsole _console;
        private readonly ISavePriceHistoryProvider _savePriceHistoryProvider;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IPriceConfigurationAdapter _priceConfigurationAdapter;
        private readonly IDataStorageProvider _instrumentBuilder;

        private readonly Dictionary<(string, string), List<PriceBarForStore>> _data = new Dictionary<(string, string), List<PriceBarForStore>>();

        public HistoryExportService(IConsole console,
            ISavePriceHistoryProvider savePriceHistoryProvider,
            IConfigurationProvider configurationProvider,
            IPriceConfigurationAdapter priceConfigurationAdapter,
            IDataStorageProvider instrumentBuilder)
        {
            _console = console;
            _savePriceHistoryProvider = savePriceHistoryProvider;
            _configurationProvider = configurationProvider;
            _priceConfigurationAdapter = priceConfigurationAdapter;
            _instrumentBuilder = instrumentBuilder;
        }

        public async Task<bool> Start()
        {
            Logger.SetLogger(LoggerType.ExportLogger);

            _console.WriteLine("Press any key to start loading history");
            _console.ReadKey();

            Logger.Log.Info("Beginning to load history");

            var configuration = _configurationProvider.GetConfiguration<HistoryConfigurationParameters>();
            var status = await LoadHistory(configuration).ConfigureAwait(false);

            if (!status)
            {
                return false;
            }

            Logger.Log.Info("History Loaded Successfully");
            _console.WriteLine("History Loaded Successfully");

            await SaveHistory(configuration.Path).ConfigureAwait(false);

            Logger.Log.Info("Save history completed");
            _console.WriteLine("Save history completed");

            return status;
        }

        private async Task<bool> LoadHistory(HistoryConfigurationParameters configuration)
        {
            var resultOfLoad = false;
            var listInstances = configuration.PriceConfiguration;
            foreach (var priceConfiguration in listInstances)
            {
                if (priceConfiguration.Disabled.HasValue && priceConfiguration.Disabled.Value)
                {
                    continue;
                }

                var (timeFrameName, instrument, dtFrom, dtTo) = await GetPriceConfigurationParameters(priceConfiguration).ConfigureAwait(false);

                if (timeFrameName == PlatformParameters.T1TimeFrameName)
                {
                    continue;
                }

                var data = configuration.LoadFromStorage
                    ? await _instrumentBuilder.BuildInstrumentFromStorage(dtTo, configuration.Path, instrument, timeFrameName, dtFrom).ConfigureAwait(false)
                    : await _instrumentBuilder.BuildInstrumentFromFile(dtTo, configuration.Path, instrument, timeFrameName, dtFrom).ConfigureAwait(false);

                if (data != null)
                {
                    var sortedAndFilteredPrice = data.PriceBarList.Where(x => x.Time <= dtTo && x.Time >= dtFrom).OrderBy(x => x.Time).ToList();
                    _data.Add((instrument, timeFrameName), sortedAndFilteredPrice);
                    resultOfLoad = true;
                }
            }

            return resultOfLoad;
        }

        private async Task SaveHistory(string path)
        {
            foreach (var item in _data)
            {
                var (instrument, timeFrame) = item.Key;
                var priceBarList = item.Value;
                if (priceBarList.Count == 0)
                {
                    return;
                }

                var fileName = $"{instrument.Replace("/", "").ToLowerInvariant()}_{timeFrame.ToLower()}_history.csv";
                await _savePriceHistoryProvider.SaveToCsv(path, fileName, priceBarList).ConfigureAwait(false);
            }
        }

        private async Task<(string, string, DateTime, DateTime)> GetPriceConfigurationParameters(PriceConfiguration priceConfiguration)
        {
            var timeFrameName = priceConfiguration.TimeFrame;
            var instrument = priceConfiguration.Instrument;
            var dtFrom = await _priceConfigurationAdapter.GetFromDateByPriceConfiguration(priceConfiguration.From, priceConfiguration.Year, priceConfiguration.PastDays).ConfigureAwait(false);
            var dtTo = await _priceConfigurationAdapter.GetToDateByPriceConfiguration(priceConfiguration.To, priceConfiguration.Year).ConfigureAwait(false);
            return (timeFrameName, instrument, dtFrom, dtTo);
        }
    }
}
