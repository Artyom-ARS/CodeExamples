using System;
using System.Threading.Tasks;
using Common.Models;

namespace Common.Adapters
{
    public class TradeAdapter : ITradeAdapter
    {
        public async Task<int> GetTimeFrameSeconds(TimeFrame tf)
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

        public async Task<int> GetTimeFrameMinutes(TimeFrame tf)
        {
            var minutes = 0;
            switch (tf)
            {
                case TimeFrame.m1:
                    minutes = 1;
                    break;
                case TimeFrame.m5:
                    minutes = 1 * 5;
                    break;
                case TimeFrame.m15:
                    minutes = 1 * 15;
                    break;
                case TimeFrame.m30:
                    minutes = 1 * 30;
                    break;
                case TimeFrame.h1:
                    minutes = 1 * 60;
                    break;
                case TimeFrame.h4:
                    minutes = 1 * 60 * 4;
                    break;
                case TimeFrame.d1:
                    minutes = 1 * 60 * 24;
                    break;
                case TimeFrame.w1:
                    minutes = 1 * 60 * 24 * 7;
                    break;
            }

            return minutes;
        }

        public async Task<int> GetTimeFrameShiftForPriceEmulation(TimeFrame timeFrameValue)
        {
            switch (timeFrameValue)
            {
                case TimeFrame.m1:
                    return 15;
                case TimeFrame.m5:
                    return 75;
                default:
                    return 0;
            }
        }

        public async Task<int> TimeFrameShiftForPriceEmulation(string timeFrame)
        {
            var timeFrameShiftForPriceEmulation = 0;
            if (Enum.TryParse(timeFrame, out TimeFrame timeFrameValue))
            {
                timeFrameShiftForPriceEmulation = await GetTimeFrameShiftForPriceEmulation(timeFrameValue).ConfigureAwait(false);
            }

            return timeFrameShiftForPriceEmulation;
        }
    }
}
