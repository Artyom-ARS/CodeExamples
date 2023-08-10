using System;

namespace Common.Models
{
    public class ClosedOrder : ActiveOrder
    {
        public decimal ClosePrice;
        public DateTime CloseTime;
        public decimal Profit;
    }
}
