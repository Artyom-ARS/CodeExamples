using System;
using System.Threading.Tasks;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Providers
{
    public interface IMarketUpdateProvider
    {
        event EventHandler<DTOPriceUpdate> PriceUpdate;

        event EventHandler<O2GOrderRow> OrderUpdate;

        Task StartWatchingPriceUpdates(O2GSession session);
    }
}
