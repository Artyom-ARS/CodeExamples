using System;
using System.Threading.Tasks;
using HistoryPlatform.DI;
using HistoryPlatform.Services;

namespace HistoryPlatform
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var container = IoCRegistrator.RegisterContainer();
            var historyPlatform = container.GetInstance<IHistoryPlatformService>();
            await historyPlatform.Start().ConfigureAwait(false);

            Console.WriteLine("Press any key to continue");
            while (!Console.KeyAvailable)
            {
            }
        }
    }
}
