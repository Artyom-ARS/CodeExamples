using System.Threading.Tasks;
using Common.Facades;
using Common.Logging;
using Common.Models;
using Common.Providers;
using ExpertService.Models;
using ExpertService.Providers;
using ExpertService.Validators;
using OnlinePlatform.Models;
using OnlinePlatform.Providers;

namespace OnlinePlatform.Services
{
    public class OnlinePlatformService : IOnlinePlatformService
    {
        private readonly IConsole _console;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IPluginProvider _pluginProvider;
        private readonly IConfigurationValidator _configurationValidator;
        private readonly ITradeProvider _tradeProvider;
        private readonly ICalculationService _calculationService;


        public OnlinePlatformService(IConsole console, IConfigurationProvider configurationProvider,
                                     IPluginProvider pluginProvider, IConfigurationValidator configurationValidator,
                                     ITradeProvider tradeProvider, ICalculationService calculationService)
        {
            _console = console;
            _configurationProvider = configurationProvider;
            _pluginProvider = pluginProvider;
            _configurationValidator = configurationValidator;
            _tradeProvider = tradeProvider;
            _calculationService = calculationService;
        }

        public async Task Start()
        {
            Logger.SetLogger(LoggerType.OnlineLogger);

            Logger.Log.Info("Starting Online Platform");

            _console.WriteLine("Start working");

            var configuration = _configurationProvider.GetConfiguration<OnlineConfigurationParameters>();
            var historyConfiguration = _configurationProvider.GetConfiguration<HistoryConfigurationParameters>();

            await _tradeProvider.LoginAndSubscribe(configuration).ConfigureAwait(false);
            await _calculationService.StartSync(historyConfiguration.History).ConfigureAwait(false);
            await _configurationValidator.StartsExpertsRealTimeValidate().ConfigureAwait(false);

            var experts = await _pluginProvider.ListExperts().ConfigureAwait(false);
            var expertParameters = _configurationProvider.GetConfiguration<ExpertParametersForTrade>();
            await _tradeProvider.InitializeHistoryPrices().ConfigureAwait(false);
            await _tradeProvider.InitializeExperts(experts, expertParameters).ConfigureAwait(false);

            _configurationValidator.ConfigurationState += ConfigurationStateHandler;

            await _tradeProvider.HandleUpdates().ConfigureAwait(false);
        }

        public async Task Stop()
        {
            _console.WriteLine("Stop working");
            await _calculationService.StopSync().ConfigureAwait(false);
            await _configurationValidator.StopSync().ConfigureAwait(false);
            await _tradeProvider.Stop().ConfigureAwait(false);
        }

        private async void ConfigurationStateHandler(object sender, bool e)
        {
            var experts = await _pluginProvider.ListExperts().ConfigureAwait(false);
            var expertParameters = _configurationProvider.GetConfiguration<ExpertParametersForTrade>();
            await _tradeProvider.InitializeExperts(experts, expertParameters).ConfigureAwait(false);
        }
    }
}
