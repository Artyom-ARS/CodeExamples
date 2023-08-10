using System;
using System.Collections.Generic;

namespace Common.Models
{
    public class TestExecutionResults
    {
        public List<ClosedOrder> ListCloseOrders;
        public decimal BargainProfit;
        public IReadOnlyDictionary<string, object> Parameters;
        public decimal Risk;
        public decimal SmartProfit;
        public decimal OrderProfit;
        public IDictionary<DateTime, IDictionary<string, decimal>> SignalHistory;
    }
}
