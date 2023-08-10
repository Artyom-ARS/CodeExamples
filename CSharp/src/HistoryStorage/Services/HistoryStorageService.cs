using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Adapters;
using Common.Facades;
using Common.Models;
using Common.Providers;
using FxConnect.Providers;
using HistoryStorage.Models;
using HistoryStorage.Providers;

namespace HistoryStorage.Services
{
    public class HistoryStorageService : IHistoryStorageService
    {
        private readonly IFxconnectProvider _fxconnectProvider;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IPriceConfigurationAdapter _priceConfigurationAdapter;
        private readonly IConsole _console;
        private readonly ISavePricesProvider _savePricesProvider;
        private readonly ILoadPricesProvider _loadPricesProvider;

        public HistoryStorageService(
            IFxconnectProvider fxconnectProvider,
            IConsole console,
            IConfigurationProvider configurationProvider,
            IPriceConfigurationAdapter priceConfigurationAdapter,
            ISavePricesProvider savePricesProvider,
            ILoadPricesProvider loadPricesProvider)
        {
            _fxconnectProvider = fxconnectProvider;
            _configurationProvider = configurationProvider;
            _priceConfigurationAdapter = priceConfigurationAdapter;
            _console = console;
            _savePricesProvider = savePricesProvider;
            _loadPricesProvider = loadPricesProvider;
        }

        public async Task Start()
        {
            _console.WriteLine("Start working");
            var account = await _fxconnectProvider.Login().ConfigureAwait(false);
            if (account == null)
            {
                throw new Exception("The account is not valid");
            }

            var configuration = _configurationProvider.GetConfiguration<HistoryConfigurationParameters>();
            Parallel.ForEach(configuration.PriceConfiguration, async (priceConfiguration) =>
            {
                if (priceConfiguration.Disabled.HasValue && priceConfiguration.Disabled.Value)
                {
                    return;
                }

                var timeFrameName = priceConfiguration.TimeFrame;
                var instrument = priceConfiguration.Instrument;
                var dtFrom = await _priceConfigurationAdapter.GetFromDateByPriceConfiguration(priceConfiguration.From, priceConfiguration.Year, priceConfiguration.PastDays).ConfigureAwait(false);
                var dtTo = await _priceConfigurationAdapter.GetToDateByPriceConfiguration(priceConfiguration.To, priceConfiguration.Year).ConfigureAwait(false);

                DateTime newFrom;
                IList<PriceBarForStore> oldPrices;
                newFrom = dtFrom;
                oldPrices = null;
                if (!configuration.ForceReplace)
                {
                    try
                    {
                        (newFrom, oldPrices) = await _loadPricesProvider.GetLastDateAndPricesFromStorage(configuration.SaveToStorage, dtFrom, dtTo, instrument, timeFrameName).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        newFrom = dtFrom;
                        oldPrices = null;
                    }
                }

                var shouldAppend = dtFrom != newFrom;
                dtFrom = newFrom;

                var (instrumentForStore, prices) = await _fxconnectProvider.RequestHistoryPrices(instrument, timeFrameName, dtFrom, dtTo).ConfigureAwait(false);
                prices = prices.Where(x => x.Time >= dtFrom).ToList();
                if (shouldAppend && prices.Any())
                {
                    await _savePricesProvider.Save(configuration, priceConfiguration, instrumentForStore, prices.ToList(), oldPrices, dtFrom.Year).ConfigureAwait(false);
                    return;
                }

                await _savePricesProvider.Save(configuration, priceConfiguration, instrumentForStore, prices).ConfigureAwait(false);
            });
        }

        public async Task Stop()
        {
            _console.WriteLine("Stop working");
            await _fxconnectProvider.Logout().ConfigureAwait(false);
        }
    }
}
