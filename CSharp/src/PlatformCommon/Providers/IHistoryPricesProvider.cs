using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace PlatformCommon.Providers
{
    public interface IHistoryPricesProvider
    {
        Task<IList<PriceBarForStore>> RequestPrices(string instrument, TimeFrame tf, int depth, DateTime now);
    }
}