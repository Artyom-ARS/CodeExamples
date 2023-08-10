using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace OnlinePlatform.Services
{
    public interface ICalculationService
    {
        Task StartSync(IList<PriceBarsHistoryKey> history);
        Task StopSync();
    }
}