using System;

namespace HistoryCommon.Models
{
    public struct PriceBarForStore
    {
        public decimal Spread;
        public decimal BidOpen;
        public decimal BidHigh;
        public decimal BidLow;
        public decimal BidClose;
        public int Volume;
        public DateTime Time;
    }
}
