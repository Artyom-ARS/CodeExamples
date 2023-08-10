using Common.Models;

namespace HistoryCommon.Adapters
{
    public interface ITradeAdapter
    {
        int GetTimeFrameShiftForPriceEmultion(TimeFrame timeframeValue);
        int GetTimeFrameSeconds(TimeFrame tf);
    }
}