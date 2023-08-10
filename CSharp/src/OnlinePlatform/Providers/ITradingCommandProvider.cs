using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace OnlinePlatform.Providers
{
    public interface ITradingCommandProvider
    {
        Task<bool> ProcessTradingCommand(string accountId, string expertId, IList<TradingCommand> commands, List<ActiveOrder> activeOrders);
    }
}