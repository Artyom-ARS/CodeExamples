using Common.Enums;

namespace Common.Models
{
    public class GgV1Parameters
    {
        public bool CloseOnly { get; set; }

        public string Instrument { get; set; }

        public int Lots { get; set; }

        public int Profit { get; set; }

        public int TurnMulti { get; set; }

        public OrderBuySell Direction { get; set; }

        public int AtrBars { get; set; }

        public int CalculatedSignalsLogSuspense { get; set; }

        public int TunnelBars { get; set; }

        public decimal TurnMultiDiscount { get; set; }

        public decimal TunnelShift { get; set; }

        public decimal TunnelSizeLimit { get; set; }

        public decimal SpreadLimit { get; set; }
    }
}
