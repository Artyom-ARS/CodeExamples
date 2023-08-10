using System;
using System.Threading.Tasks;
using HistoryStorage.DI;
using HistoryStorage.Services;

namespace HistoryStorage
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var container = IoCRegistrator.RegisterContainer();
            var service = container.GetInstance<IHistoryStorageService>();
            await service.Start().ConfigureAwait(false);

            Console.WriteLine("Press any key to continue");
            while (!Console.KeyAvailable)
            {
            }

            await service.Stop().ConfigureAwait(false);
        }
    }
}
