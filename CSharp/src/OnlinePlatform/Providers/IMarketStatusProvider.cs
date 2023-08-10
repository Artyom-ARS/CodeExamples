using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using FxConnect.Models;

namespace OnlinePlatform.Providers
{
    public interface IMarketStatusProvider
    {
        Task PrintStatus(DTOAccount account, IList<ActiveOrder> activeOrders, IList<ClosedOrder> closedOrders, Dictionary<string, PriceTick> prices);
    }
}