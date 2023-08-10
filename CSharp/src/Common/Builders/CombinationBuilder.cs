using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Builders
{
    public class CombinationBuilder : ICombinationBuilder
    {
        public async Task<List<Dictionary<string, object>>> MakeCombinations(IReadOnlyDictionary<string, List<object>> parametersSet)
        {
            var input = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>(),
            };
            foreach (var parameterValues in parametersSet)
            {
                var tempList = new List<Dictionary<string, object>>();
                foreach (var combination in input)
                {
                    foreach (var parameterValue in parameterValues.Value)
                    {
                        var newValue = new Dictionary<string, object>(combination)
                        {
                            { parameterValues.Key, parameterValue },
                        };
                        tempList.Add(newValue);
                    }
                }

                input = tempList;
            }

            return input;
        }
    }
}
