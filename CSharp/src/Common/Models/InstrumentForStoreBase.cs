using System;

namespace Common.Models
{
    public class InstrumentForStoreBase
    {
        public int Digits;
        public string Instrument;
        public string OfferId;
        public string TimeFrame;
        public decimal PointSize;
        [Obsolete]
        public DateTime FromTime;
        [Obsolete]
        public DateTime ToTime;
    }
}
