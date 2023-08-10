using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace HistoryPlatform.Builders
{
    public class TestParametersBuilder : ITestParametersBuilder
    {
        public async Task<Dictionary<string, List<object>>> PrepareCombinations(IReadOnlyDictionary<string, object> expert)
        {
            var parametersSet = new Dictionary<string, List<object>>();

            foreach (var param in expert)
            {
                var value = param.Value;
                var name = param.Key;

                switch (value)
                {
                    case string str:
                        {
                            parametersSet.Add(name, new List<object>
                        {
                            str,
                        });
                            break;
                        }

                    case JObject obj:
                        {
                            var arr = new List<object>();
                            var step = (decimal)obj["step"];
                            var start = (decimal)obj["start"];
                            var end = (decimal)obj["end"];
                            for (var i = start; i <= end; i += step)
                            {
                                arr.Add(i.ToString());
                            }

                            parametersSet.Add(name, arr);
                            break;
                        }

                    case JArray arr:
                        {
                            var list = arr.Select(x => (object)x.Value<string>()).ToList();
                            parametersSet.Add(name, list);
                            break;
                        }
                }
            }

            return parametersSet;
        }
    }
}
