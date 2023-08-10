using System;

namespace Common.Models
{
    public struct PriceTick
    {
        public decimal Bid;

        public decimal Ask;

        public DateTime TickServerTime;

        public string Instrument;

        public decimal PointSize;
    }
}
