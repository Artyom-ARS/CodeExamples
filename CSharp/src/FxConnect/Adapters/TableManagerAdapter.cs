using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Enums;
using Common.Models;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Adapters
{
    public class TableManagerAdapter : ITableManagerAdapter
    {
        public async Task<DTOAccount> GetAccount(O2GAccountRow account)
        {
            var dtoAccount = new DTOAccount { Id = account.AccountID };
            return dtoAccount;
        }

        public async Task<IList<ActiveOrder>> GetActiveOrders(IList<O2GTradeTableRow> activeOrders)
        {
            var orders = new List<ActiveOrder>();
            foreach (var activeOrder in activeOrders)
            {
                var order = new ActiveOrder
                {
                    OrderId = activeOrder.TradeID,
                    BuySell = activeOrder.BuySell == Constants.Buy ? OrderBuySell.Buy : OrderBuySell.Sell,
                    Lots = activeOrder.Amount,
                    OpenPrice = (decimal)activeOrder.OpenRate,
                    StopPrice = (decimal)activeOrder.Stop,
                    LimitPrice = (decimal)activeOrder.Limit,
                    StopLossPrice = (decimal)activeOrder.Stop,
                    TakeProfitPrice = (decimal)activeOrder.Limit,
                    OpenTime = activeOrder.OpenTime,
                    Instrument = activeOrder.Instrument,
                    Tag = activeOrder.OpenOrderRequestTXT,
                };
                orders.Add(order);
            }

            return orders;
        }

        public async Task<InstrumentForStoreBase> GetInstrument(O2GOfferTableRow offer)
        {
            var instrument = new InstrumentForStoreBase
            {
                Digits = offer.Digits,
                PointSize = (decimal)offer.PointSize,
                Instrument = offer.Instrument,
                OfferId = offer.OfferID,
            };
            return instrument;
        }
    }
}
