using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Experts;
using Common.Models;

namespace OnlinePlatform.Providers
{
    public interface ITradeProvider
    {
        Task LoginAndSubscribe(OnlineConfigurationParameters configuration);
        Task HandleUpdates();
        Task Stop();
        Task InitializeExperts(IList<IExpert> experts, ExpertService.Models.ExpertParametersForTrade expertParameters);
        Task InitializeHistoryPrices();
    }
}