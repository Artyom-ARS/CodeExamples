using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Adapters
{
    public interface IMarketDataAdapter
    {
        Task<int> GetBaseUnit(O2GSession session, O2GAccountRow account, string instrument);

        Task<DTOOrderUpdate> GetOrderUpdate(O2GOrderRow e);

        Task<IList<PriceBarForStore>> GetPriceBarList(IList<PriceBar> prices);
    }
}
