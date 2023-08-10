using System;
using System.Threading.Tasks;
using OnlinePlatform.DI;
using OnlinePlatform.Services;

namespace OnlinePlatform
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var container = IoCRegistrator.RegisterContainer();

            var service = container.GetInstance<IOnlinePlatformService>();
            await service.Start().ConfigureAwait(false);
            while (!Console.KeyAvailable)
            {
            }

            await service.Stop().ConfigureAwait(false);
        }
    }
}
