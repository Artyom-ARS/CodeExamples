using System.Threading.Tasks;

namespace HistoryPlatform.Services
{
    public interface IHistoryPlatformService
    {
        Task<bool> Start();
    }
}
