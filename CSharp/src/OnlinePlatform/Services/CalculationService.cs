using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Common.Adapters;
using Common.Models;
using PlatformCommon.Providers;

namespace OnlinePlatform.Services
{
    public class CalculationService : ICalculationService
    {
        private bool _sync;
        private const int SyncSpanSec = 10;
        private Timer _timer;
        private IList<PriceBarsHistoryKey> _history;
        private readonly IHistoryPricesCacheProvider _historyPricesCacheProvider;
        private readonly IBarsHistoryAdapter _barsHistoryAdapter;

        public CalculationService(IHistoryPricesCacheProvider historyPricesCacheProvider,
            IBarsHistoryAdapter barsHistoryAdapter)
        {
            _historyPricesCacheProvider = historyPricesCacheProvider;
            _barsHistoryAdapter = barsHistoryAdapter;
            _sync = true;
        }

        public async Task StartSync(IList<PriceBarsHistoryKey> history)
        {
            await Initialize(history).ConfigureAwait(false);
            _timer = new Timer(SyncSpanSec * 1000)
            {
                AutoReset = true
            };

            _timer.Elapsed += SyncData;
            _timer.Start();
        }

        public async Task StopSync()
        {
            _sync = false;
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }

        private async Task Initialize(IList<PriceBarsHistoryKey> history)
        {
            _history = history;
            foreach (var config in _history)
            {
                var historyKey = await _barsHistoryAdapter.GetInstrumentHistoryPricesKey(config.Instrument, config.TimeFrame).ConfigureAwait(false);
                await _historyPricesCacheProvider.InitializeInstrumentHistoryPrices(historyKey, config.TimeFrame).ConfigureAwait(false);
            }
        }

        private async void SyncData(object sender, ElapsedEventArgs e)
        {
            if (_sync)
            {
                var now = DateTime.Now;
                foreach (var config in _history)
                {
                    var historyKey = await _barsHistoryAdapter.GetInstrumentHistoryPricesKey(config.Instrument, config.TimeFrame).ConfigureAwait(false);
                    await _historyPricesCacheProvider.GetPrices(historyKey, config.Instrument, config.TimeFrame, config.BarsCount, now).ConfigureAwait(false);
                }
            }
        }
    }
}
