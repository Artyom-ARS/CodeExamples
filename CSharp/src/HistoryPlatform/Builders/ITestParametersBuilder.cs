using System.Collections.Generic;
using System.Threading.Tasks;

namespace HistoryPlatform.Builders
{
    public interface ITestParametersBuilder
    {
        Task<Dictionary<string, List<object>>> PrepareCombinations(IReadOnlyDictionary<string, object> expert);
    }
}
