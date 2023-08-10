using System.Threading.Tasks;

namespace HistoryPlatform.Actions
{
    public interface IPlatformActions
    {
        Task<bool> LoadHistory();

        Task<bool> RunExpert();
    }
}
