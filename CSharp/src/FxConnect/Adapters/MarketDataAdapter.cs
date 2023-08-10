using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Constants;
using Common.Enums;
using Common.Logging;
using Common.Models;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Adapters
{
    public class MarketDataAdapter : IMarketDataAdapter
    {
        public async Task<int> GetBaseUnit(O2GSession session, O2GAccountRow account, string instrument)
        {
            var loginRules = session.getLoginRules();
            if (loginRules == null)
            {
                throw new Exception("Cannot get login rules");
            }

            var tradingSettingsProvider = loginRules.getTradingSettingsProvider();
            var baseUnitSize = tradingSettingsProvider.getBaseUnitSize(instrument, account);

            return baseUnitSize;
        }

        public async Task<DTOOrderUpdate> GetOrderUpdate(O2GOrderRow order)
        {
            var propertyInfos = order.GetType().GetProperties();

            var sb = new StringBuilder();

            foreach (var info in propertyInfos)
            {
                var value = info.GetValue(order, null) ?? "(null)";
                sb.Append(Environment.NewLine + info.Name + ": " + value);
            }

            var objectProperties = sb.ToString();
            Logger.Log.Info($"MarketDataAdapter.GetOrderUpdate. Order: {objectProperties}");

            if (order.Type == Constants.Orders.Limit || order.Type == Constants.Orders.Stop)
            {
                DTOOrderUpdate update;
                if (order.Status == PlatformParameters.OrderUpdateStatus)
                {
                    update = new DTOOrderUpdate
                    {
                        OrderId = order.TradeID,
                        Tag = order.RequestTXT,
                        Status = order.Type == Constants.Orders.Stop ? OrderOpenClose.UpdateSL : OrderOpenClose.UpdateTP,
                        Price = order.Rate,
                        Time = order.StatusTime,
                        Direction = order.BuySell == Constants.Buy ? OrderBuySell.Buy : OrderBuySell.Sell,
                        Amount = order.FilledAmount,
                    };
                    return update;
                }

                if (order.Status == PlatformParameters.OrderCloseStatus)
                {
                    update = new DTOOrderUpdate
                    {
                        OrderId = order.TradeID,
                        Tag = order.RequestTXT,
                        Status = order.Type == Constants.Orders.Stop ? OrderOpenClose.UpdateSL : OrderOpenClose.UpdateTP,
                        Price = 0.0,
                        Time = order.StatusTime,
                        Direction = order.BuySell == Constants.Buy ? OrderBuySell.Buy : OrderBuySell.Sell,
                        Amount = order.FilledAmount,
                    };
                    return update;
                }
            }

            if (order.Status == PlatformParameters.OrderFinalStatus)
            {
                var result = new DTOOrderUpdate
                {
                    OrderId = order.TradeID,
                    Tag = order.RequestTXT,
                    Status = order.Stage == "O" ? OrderOpenClose.Open : OrderOpenClose.Close,
                    Price = order.ExecutionRate,
                    Time = order.StatusTime,
                    Direction = order.BuySell == Constants.Buy ? OrderBuySell.Buy : OrderBuySell.Sell,
                    Amount = order.FilledAmount,
                };

                return result;
            }

            return null;
        }

        public async Task<IList<PriceBarForStore>> GetPriceBarList(IList<PriceBar> prices)
        {
            var newPrices = new List<PriceBarForStore>();
            var hours = (int)Math.Ceiling(TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalHours);

            foreach (var price in prices)
            {
                var spread = (price.AskClose - price.BidClose + price.AskOpen - price.BidOpen + price.AskHigh - price.BidHigh +
                    price.AskLow - price.BidLow) / 4;
                var newPrice = new PriceBarForStore()
                {
                    Time = price.Time.AddHours(hours),
                    Volume = price.Volume,
                    BidOpen = price.BidOpen,
                    BidClose = price.BidClose,
                    BidHigh = price.BidHigh,
                    BidLow = price.BidLow,
                    Spread = spread,
                };

                newPrices.Add(newPrice);
            }

            return newPrices;
        }
    }
}
