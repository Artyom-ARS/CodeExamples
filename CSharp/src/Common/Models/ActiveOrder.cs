using System;
using Common.Enums;

namespace Common.Models
{
    public class ActiveOrder
    {
        public string OrderId { get; set; }

        public int Lots { get; set; }

        public OrderBuySell BuySell { get; set; }

        public string Instrument { get; set; }

        public DateTime OpenTime { get; set; }

        public decimal OpenPrice { get; set; }

        public decimal StopPrice { get; set; }

        public decimal LimitPrice { get; set; }

        public string Tag { get; set; }

        public decimal StopLossPrice { get; set; }

        public decimal TakeProfitPrice { get; set; }
    }
}
