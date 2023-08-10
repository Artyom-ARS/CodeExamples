using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Adapters
{
    public interface ITableManagerAdapter
    {
        Task<DTOAccount> GetAccount(O2GAccountRow account);

        Task<IList<ActiveOrder>> GetActiveOrders(IList<O2GTradeTableRow> activeOrders);

        Task<InstrumentForStoreBase> GetInstrument(O2GOfferTableRow offer);
    }
}
