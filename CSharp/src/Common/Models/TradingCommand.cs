using Common.Enums;

namespace Common.Models
{
    public class TradingCommand
    {
        public OrderOpenClose OpenClose { get; set; }

        public int Lots { get; set; }

        public OrderBuySell BuySell { get; set; }

        public string OrderId { get; set; }

        public string Instrument { get; set; }

        public decimal StopLoss { get; set; }

        public decimal TakeProfit { get; set; }
    }
}
