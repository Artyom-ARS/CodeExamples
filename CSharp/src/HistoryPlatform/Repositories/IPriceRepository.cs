using System;
using System.Threading.Tasks;
using Common.Models;

namespace HistoryPlatform.Repositories
{
    public interface IPriceRepository
    {
        Task Add(InstrumentForStore value);

        Task<InstrumentForStore> Get(string instrument, TimeFrame timeFrame, DateTime dateFrom, DateTime dateTo);

        Task<InstrumentForStore> Get(string instrument, TimeFrame timeFrame, DateTime dateTo, int depth);

        Task<InstrumentForStore> GetFlatInstrument(string instrument, TimeFrame timeFrameName);
    }
}
