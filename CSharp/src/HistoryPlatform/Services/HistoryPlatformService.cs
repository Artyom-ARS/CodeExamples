using System.Threading.Tasks;
using Common.Facades;
using Common.Logging;
using Common.Models;
using HistoryPlatform.Actions;

namespace HistoryPlatform.Services
{
    public class HistoryPlatformService : IHistoryPlatformService
    {
        private readonly IPlatformActions _platformActions;
        private readonly IConsole _console;

        public HistoryPlatformService(IPlatformActions platformActions, IConsole console)
        {
            _platformActions = platformActions;
            _console = console;
        }

        public async Task<bool> Start()
        {
            Logger.SetLogger(LoggerType.HistoryLogger);

            _console.WriteLine("Press any key to start loading history");
            _console.ReadKey();

            Logger.Log.Info("Beginning to load history");

            var status = await _platformActions.LoadHistory().ConfigureAwait(false);

            if (!status)
            {
                return false;
            }

            Logger.Log.Info("History Loaded Successfully");
            _console.WriteLine("History Loaded Successfully");
            Logger.Log.Info("Expert has been launched");
            _console.WriteLine("Expert has been launched");
            status = await _platformActions.RunExpert().ConfigureAwait(false);
            if (!status)
            {
                return false;
            }

            Logger.Log.Info("Expert work completed");
            _console.WriteLine("Expert work completed");

            return status;
        }
    }
}
