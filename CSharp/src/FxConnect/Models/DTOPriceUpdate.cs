using System;

namespace FxConnect.Models
{
    public class DTOPriceUpdate
    {
        public double Bid { get; set; }

        public string Instrument { get; set; }

        public double Ask { get; set; }

        public DateTime ServerTime { get; set; }
    }
}
