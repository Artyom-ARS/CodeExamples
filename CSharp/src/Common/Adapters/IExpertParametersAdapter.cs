using System.Collections.Generic;

namespace Common.Adapters
{
    public interface IExpertParametersAdapter
    {
        T GetParameters<T>(IReadOnlyDictionary<string, object> parameters)
            where T : new();
    }
}
