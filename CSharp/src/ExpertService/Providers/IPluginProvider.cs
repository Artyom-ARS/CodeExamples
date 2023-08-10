using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Experts;

namespace ExpertService.Providers
{
    public interface IPluginProvider
    {
        Task<List<IExpert>> ListExperts();
    }
}
