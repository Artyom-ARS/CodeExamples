using System;

namespace FxConnect.Models
{
    public class PriceBar
    {
        public DateTime Time;
        public decimal AskClose;
        public decimal AskOpen;
        public decimal AskHigh;
        public decimal AskLow;
        public int Volume;
        public decimal BidClose;
        public decimal BidOpen;
        public decimal BidHigh;
        public decimal BidLow;
    }
}
