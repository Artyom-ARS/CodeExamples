using System.Threading.Tasks;
using Common.Models;

namespace Common.Adapters
{
    public interface ITradeAdapter
    {
        Task<int> GetTimeFrameShiftForPriceEmulation(TimeFrame timeFrameValue);

        Task<int> GetTimeFrameSeconds(TimeFrame tf);

        Task<int> GetTimeFrameMinutes(TimeFrame tf);

        Task<int> TimeFrameShiftForPriceEmulation(string timeFrame);
    }
}
