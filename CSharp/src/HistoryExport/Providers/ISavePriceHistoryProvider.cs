using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace HistoryExport.Providers
{
    public interface ISavePriceHistoryProvider
    {
        Task<bool> SaveToCsv(string folderPath, string fileName, List<PriceBarForStore> priceBarList);
    }
}
