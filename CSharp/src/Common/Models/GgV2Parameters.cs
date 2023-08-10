using Common.Enums;

namespace Common.Models
{
    public class GgV2Parameters
    {
        public bool CloseOnly { get; set; }

        public string Instrument { get; set; }

        public int Lots { get; set; }

        public int Profit { get; set; }

        public int HedgeMulti { get; set; }

        public OrderBuySell Direction { get; set; }

        public int AtrBars { get; set; }

        public int CalculatedSignalsLogSuspense { get; set; }

        public int HedgeBars { get; set; }

        public decimal SpreadLimit { get; set; }

        public decimal HedgeOCShift { get; set; }

        public decimal HedgeOrderMulti { get; set; }

        public decimal HedgeTPShift { get; set; }

        public int HedgeStopLoss { get; set; }
    }
}
