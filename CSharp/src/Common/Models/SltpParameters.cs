using Common.Enums;

namespace Common.Models
{
    public class SltpParameters
    {
        public string Instrument { get; set; }

        public int Lots { get; set; }

        public int Profit { get; set; }

        public int Loose { get; set; }

        public OrderBuySell Direction { get; set; }
    }
}
