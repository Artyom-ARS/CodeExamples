using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using FxConnect.Providers;
using PlatformCommon.Providers;

namespace OnlinePlatform.Providers
{
    public class OnlinePricesProvider : IHistoryPricesProvider
    {
        private readonly IFxconnectProvider _fxconnectProvider;

        public OnlinePricesProvider(IFxconnectProvider fxconnectProvider)
        {
            _fxconnectProvider = fxconnectProvider;
        }

        public async Task<IList<PriceBarForStore>> RequestPrices(string instrument, TimeFrame tf, int depth, DateTime now)
        {
            return await _fxconnectProvider.RequestHistoryPrices(instrument, tf.ToString().ToLowerInvariant(), depth).ConfigureAwait(false);
        }
    }
}
