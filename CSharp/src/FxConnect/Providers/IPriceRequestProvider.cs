using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Providers
{
    public interface IPriceRequestProvider
    {
        Task<IList<PriceBar>> GetPrices(O2GSession session, O2GRequest request);

        Task<IList<PriceBar>> LoopThroughPrices(O2GSession session, O2GRequest request, DateTime dtFrom, DateTime dtTo);
    }
}
