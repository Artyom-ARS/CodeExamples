using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using FxConnect.Models;

namespace OnlinePlatform.Providers
{
    public interface IPriceUpdateSuspenseProvider
    {
        Task<bool> ShouldWait(Dictionary<string, PriceUpdateSuspenseParameters> priceUpdateSuspense, DTOPriceUpdate priceUpdate);
    }
}