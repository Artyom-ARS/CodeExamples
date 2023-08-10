using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Common.Enums;
using Common.Facades;
using Common.Models;
using FxConnect.Models;

namespace OnlinePlatform.Providers
{
    public class MarketStatusProvider : IMarketStatusProvider
    {
        private readonly IConsole _console;

        public MarketStatusProvider(IConsole console)
        {
            _console = console;
        }

        public async Task PrintStatus(DTOAccount account, IList<ActiveOrder> activeOrders, IList<ClosedOrder> closedOrders,
            Dictionary<string, PriceTick> prices)
        {
            var ao = new ReadOnlyCollection<ActiveOrder>(activeOrders.ToList());
            var co = new ReadOnlyCollection<ClosedOrder>(closedOrders.ToList());
            _console.Clear();
            _console.WriteLine("Account Id: {0}", account.Id);
            _console.WriteLine("Active orders:");
            foreach (var order in ao)
            {
                var orderStatus = $"Instrument: {order.Instrument}, Direction: {(order.BuySell == OrderBuySell.Buy ? "B" : "S")}, Order ID: {order.OrderId}";
                orderStatus += $", Amount: {order.Lots}, Price: {order.OpenPrice}, Time: {order.OpenTime}, Tag: {order.Tag}";
                _console.WriteLine(orderStatus);
                if (!prices.ContainsKey(order.Instrument))
                {
                    _console.WriteLine("  Profit in points: unknown");
                }
                else
                {
                    decimal profit;
                    var currentPrice = prices[order.Instrument];
                    if (order.BuySell == OrderBuySell.Buy)
                    {
                        profit = currentPrice.Bid - order.OpenPrice;
                    }
                    else
                    {
                        profit = order.OpenPrice - currentPrice.Ask;
                    }
                    profit /= currentPrice.PointSize;
                    _console.WriteLine($"  Profit in points: {profit}");
                }

            }
            _console.WriteLine("");
            _console.WriteLine("Closed orders:");
            var lastOrders = co.OrderByDescending(x => x.CloseTime).Take(8);
            foreach (var order in lastOrders)
            {
                _console.WriteLine("Order ID: {0}, Direction: {1}, Amount: {2}, Open Price: {3}, Open Time: {4}, Close Price: {5}, Close Time: {6}, Instrument: {7}, Tag: {8}",
                    order.OrderId, order.BuySell == OrderBuySell.Buy ? "buy" : "sell", order.Lots, order.OpenPrice,
                    order.OpenTime, order.ClosePrice, order.CloseTime, order.Instrument, order.Tag);

                var profit = order.ClosePrice - order.OpenPrice;
                profit *= order.Lots;
                if (order.BuySell == OrderBuySell.Sell)
                {
                    profit *= -1;
                }
                _console.WriteLine($"  Profit: {profit}");
            }
        }
    }
}
