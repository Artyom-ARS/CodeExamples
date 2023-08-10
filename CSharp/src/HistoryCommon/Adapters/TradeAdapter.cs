using Common.Models;

namespace HistoryCommon.Adapters
{
    public class TradeAdapter : ITradeAdapter
    {
        public int GetTimeFrameSeconds(TimeFrame tf)
        {
            var seconds = 0;
            switch (tf)
            {
                case TimeFrame.m1:
                    seconds = 60;
                    break;
                case TimeFrame.m5:
                    seconds = 60 * 5;
                    break;
                case TimeFrame.m15:
                    seconds = 60 * 15;
                    break;
                case TimeFrame.m30:
                    seconds = 60 * 30;
                    break;
                case TimeFrame.h1:
                    seconds = 60 * 60;
                    break;
                case TimeFrame.h4:
                    seconds = 60 * 60 * 4;
                    break;
                case TimeFrame.d1:
                    seconds = 60 * 60 * 24;
                    break;
                case TimeFrame.w1:
                    seconds = 60 * 60 * 24 * 7;
                    break;
            }

            return seconds;
        }

        public int GetTimeFrameShiftForPriceEmultion(TimeFrame timeframeValue)
        {
            switch (timeframeValue)
            {
                case TimeFrame.m1:
                    return 15;
                case TimeFrame.m5:
                    return 75;
                default:
                    return 0;
            }
        }
    }
}