using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace HistoryPlatform.Providers
{
    public interface IPriceTickProvider
    {
        Task<List<PriceTick>> CreatePriceTicketList(InstrumentForStore dataSetForInstrument);
    }
}
