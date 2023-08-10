using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Builders
{
    public interface ICombinationBuilder
    {
        Task<List<Dictionary<string, object>>> MakeCombinations(IReadOnlyDictionary<string, List<object>> parametersSet);
    }
}
