using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace HistoryStorage.Providers
{
    public interface ILoadPricesProvider
    {
        Task<(DateTime, IList<PriceBarForStore>)> GetLastDateAndPricesFromStorage(bool saveToStorage, DateTime dtFrom, DateTime dtTo, string instrument, string timeFrameName);
    }
}
