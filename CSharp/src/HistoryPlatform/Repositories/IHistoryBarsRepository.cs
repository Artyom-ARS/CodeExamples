using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace HistoryPlatform.Repositories
{
    public interface IHistoryBarsRepository
    {
        Task FillHistoryByInstrumentAndTimeFrame();

        Task GetLastHistoryBars(DateTime currentTime, string historyId);

        Task<string> InitTimeFrame();

        Task<Dictionary<string, IList<PriceBarForStore>>> GetpriceHistoryCacheByKey(string historyId);
    }
}
