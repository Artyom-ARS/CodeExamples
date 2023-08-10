using System;
using Common.Enums;

namespace FxConnect.Models
{
    public class DTOOrderUpdate
    {
        public string OrderId { get; set; }

        public string Tag { get; set; }

        public double Price { get; set; }

        public DateTime Time { get; set; }

        public OrderBuySell Direction { get; set; }

        public OrderOpenClose Status { get; set; }

        public int Amount { get; set; }

        public string Instrument { get; set; }
    }
}
