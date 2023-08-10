using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Experts;
using Common.Models;

namespace HistoryPlatform.Providers
{
    public interface ITradeProvider
    {
        IReadOnlyCollection<IExpert> Experts { get; }

        Task<(List<ClosedOrder>, IDictionary<DateTime, IDictionary<string, decimal>>)> ExecuteExpert(IExpert expert, TimeFrame timeFrame, string objGuid, bool closeAllOpen, List<PriceTick> priceTickList);

        Task LoadExperts();
    }
}
