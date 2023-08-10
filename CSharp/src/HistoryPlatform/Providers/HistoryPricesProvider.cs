using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using HistoryPlatform.Repositories;
using PlatformCommon.Providers;

namespace HistoryPlatform.Providers
{
    public class HistoryPricesProvider : IHistoryPricesProvider
    {
        private readonly IPriceRepository _priceRepository;

        public HistoryPricesProvider(IPriceRepository priceRepository)
        {
            _priceRepository = priceRepository;
        }

        public async Task<IList<PriceBarForStore>> RequestPrices(string instrument, TimeFrame tf, int depth, DateTime now)
        {
            var data = await _priceRepository.Get(instrument, tf, now, depth).ConfigureAwait(false);
            return data.PriceBarList;
        }
    }
}
