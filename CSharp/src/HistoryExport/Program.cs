using System;
using System.Threading.Tasks;
using HistoryExport.DI;
using HistoryExport.Services;

namespace HistoryExport
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var container = IoCRegistrator.RegisterContainer();
            var historyPlatform = container.GetInstance<IHistoryExportService>();
            await historyPlatform.Start().ConfigureAwait(false);

            Console.WriteLine("Press any key to continue");
            while (!Console.KeyAvailable)
            {
            }
        }
    }
}
