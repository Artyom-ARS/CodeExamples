using System.Threading.Tasks;

namespace OnlinePlatform.Providers
{
    public interface ILockProvider
    {
        Task<bool> Lock(string expertId);
        Task UnLock(string expertId);
    }
}