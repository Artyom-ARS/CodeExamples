using System.Threading.Tasks;

namespace HistoryStorage.Services
{
    public interface IHistoryStorageService
    {
        Task Start();

        Task Stop();
    }
}
