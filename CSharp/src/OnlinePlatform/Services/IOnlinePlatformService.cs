using System.Threading.Tasks;

namespace OnlinePlatform.Services
{
    public interface IOnlinePlatformService
    {
        Task Start();

        Task Stop();
    }
}
