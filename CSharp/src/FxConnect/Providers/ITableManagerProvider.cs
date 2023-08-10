using System.Collections.Generic;
using System.Threading.Tasks;
using fxcore2;

namespace FxConnect.Providers
{
    public interface ITableManagerProvider
    {
        Task<O2GTableManager> GetTableManager(O2GSession session);

        Task<O2GAccountRow> GetAccount(O2GTableManager tableManager);

        Task<IList<O2GTradeTableRow>> GetActiveOrders(O2GTableManager tableManager);

        Task<O2GOfferTableRow> GetOffer(O2GTableManager tableManager, string instrument);

        Task<O2GAccountRow> GetAccount(O2GTableManager tableManager, string accountId);

        Task<O2GOfferTableRow> GetOfferById(O2GTableManager tableManager, string offerID);

        Task<IList<O2GOfferTableRow>> GetSubscribedOffers(O2GTableManager tableManager);
    }
}
