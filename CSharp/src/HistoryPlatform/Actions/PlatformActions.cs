using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Adapters;
using Common.Builders;
using Common.Constants;
using Common.Decorators;
using Common.Logging;
using Common.Models;
using Common.Providers;
using ExpertService.Models;
using HistoryPlatform.Builders;
using HistoryPlatform.Factories;
using HistoryPlatform.Models;
using HistoryPlatform.Providers;
using HistoryPlatform.Repositories;
using PlatformCommon.Providers;

namespace HistoryPlatform.Actions
{
    public class PlatformActions : IPlatformActions
    {
        private readonly IPriceConfigurationAdapter _priceConfigurationAdapter;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IPriceRepository _priceRepository;
        private readonly IDataStorageProvider _instrumentBuilder;
        private readonly ITradeProvider _tradeProvider;
        private readonly ITestResultsProvider _testResultsProvider;
        private readonly ICombinationBuilder _combinationBuilder;
        private readonly ITestParametersBuilder _testParametersBuilder;
        private readonly IHistoryBarsRepository _historyBarsRepository;
        private readonly IExpertFactory _expertFactory;
        private readonly IConsoleDecorator _consoleDecorator;
        private readonly IPriceTickProvider _priceTickProvider;
        private readonly object _object = new object();

        public PlatformActions(
            IPriceConfigurationAdapter priceConfigurationAdapter,
            IConfigurationProvider configurationProvider,
            IPriceRepository priceRepository,
            IDataStorageProvider instrumentBuilder,
            ITradeProvider tradeProvider,
            ITestResultsProvider testResultsProvider,
            ICombinationBuilder combinationBuilder,
            ITestParametersBuilder testParametersBuilder,
            IHistoryBarsRepository historyBarsRepository,
            IExpertFactory expertFactory,
            IConsoleDecorator consoleDecorator,
            IPriceTickProvider priceTickProvider)
        {
            _priceConfigurationAdapter = priceConfigurationAdapter;
            _configurationProvider = configurationProvider;
            _priceRepository = priceRepository;
            _instrumentBuilder = instrumentBuilder;
            _tradeProvider = tradeProvider;
            _testResultsProvider = testResultsProvider;
            _combinationBuilder = combinationBuilder;
            _testParametersBuilder = testParametersBuilder;
            _historyBarsRepository = historyBarsRepository;
            _expertFactory = expertFactory;
            _consoleDecorator = consoleDecorator;
            _priceTickProvider = priceTickProvider;
        }

        public async Task<bool> LoadHistory()
        {
            var resultOfLoad = false;
            var configuration = _configurationProvider.GetConfiguration<HistoryConfigurationParameters>();
            var listInstances = configuration.PriceConfiguration;
            foreach (var priceConfiguration in listInstances)
            {
                if (priceConfiguration.Disabled.HasValue && priceConfiguration.Disabled.Value)
                {
                    continue;
                }

                var (timeFrameName, instrument, dtFrom, dtTo) = await GetPriceConfigurationParameters(priceConfiguration).ConfigureAwait(false);

                if (timeFrameName == PlatformParameters.T1TimeFrameName)
                {
                    continue;
                }

                var data = configuration.LoadFromStorage
                    ? await _instrumentBuilder.BuildInstrumentFromStorage(dtTo, configuration.Path, instrument, timeFrameName, dtFrom).ConfigureAwait(false)
                    : await _instrumentBuilder.BuildInstrumentFromFile(dtTo, configuration.Path, instrument, timeFrameName, dtFrom).ConfigureAwait(false);

                if (data != null)
                {
                    await _priceRepository.Add(data).ConfigureAwait(false);
                    resultOfLoad = true;
                }
            }

            return resultOfLoad;
        }

        public async Task<bool> RunExpert()
        {
            var configuration = _configurationProvider.GetConfiguration<PriceForTradeParameters>();
            var priceConfiguration = configuration.PriceForTrade;
            var testsExecutionParameters = _configurationProvider.GetConfiguration<TestsExecutionParameters>();
            var maxDegreeOfParallelism = testsExecutionParameters.MaxDegreeOfParallelism == 0 ? Environment.ProcessorCount : testsExecutionParameters.MaxDegreeOfParallelism;
            var riskLimit = testsExecutionParameters.RiskLimit == 0 ? 3 : testsExecutionParameters.RiskLimit;
            var bestProfitParam = testsExecutionParameters.BestProfit == 0 ? PlatformParameters.BestProfit : testsExecutionParameters.BestProfit;
            var testResultsInPairs = testsExecutionParameters.TestResultsInPairs;
            var closeAllOpen = testsExecutionParameters.CloseAllOpen;
            var saveSignalsToFile = testsExecutionParameters.SaveSignalsToFile;

            var (timeFrameName, instrument, dtFrom, dtTo) = await GetPriceConfigurationParameters(priceConfiguration).ConfigureAwait(false);
            Enum.TryParse(timeFrameName, out TimeFrame timeFrame);
            await _historyBarsRepository.FillHistoryByInstrumentAndTimeFrame().ConfigureAwait(false);
            await _tradeProvider.LoadExperts().ConfigureAwait(false);
            var expertList = _configurationProvider.GetConfiguration<ExpertParametersForTrade>();
            foreach (var expert in _tradeProvider.Experts)
            {
                var expertsConfiguration = expertList.Experts.FirstOrDefault(x => x["Id"].ToString() == expert.Id);
                var disabled = expertsConfiguration.ContainsKey("disabled") && bool.Parse(expertsConfiguration["disabled"].ToString());
                if (disabled)
                {
                    continue;
                }

                var expertParameters = expertList.Experts.FirstOrDefault(x => x["Id"].ToString() == expert.Id);
                var values = await _testParametersBuilder.PrepareCombinations(expertParameters).ConfigureAwait(false);
                var combinations = await _combinationBuilder.MakeCombinations(values).ConfigureAwait(false);
                var percentStep = 100 / (double)combinations.Count;
                var gap = percentStep;
                var percent = 0D;
                _consoleDecorator.WriteProgressBar(0);
                var dataSetForInstrument = await _priceRepository.Get(instrument, timeFrame, Convert.ToDateTime(dtFrom), Convert.ToDateTime(dtTo)).ConfigureAwait(false);
                if (dataSetForInstrument.PriceBarList.Count == 0) { continue; }

                var priceTickList = await _priceTickProvider.CreatePriceTicketList(dataSetForInstrument).ConfigureAwait(false);

                Parallel.ForEach(combinations, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, async parameters =>
                {
                    var objGuid = await _historyBarsRepository.InitTimeFrame().ConfigureAwait(false);
                    var expertName = values.Where(k => k.Key == "ClassName").ElementAt(0).Value.ElementAt(0).ToString();
                    var newExpert = _expertFactory.Switcher(expertName);

                    if (newExpert == null)
                    {
                        Logger.Log.Error($"Expert {expertName} ot found");
                        return;
                    }

                    newExpert.InitializeParameters(parameters, false);
                    newExpert.InitializeId(expert.Id);

                    var (closedOrders, signalHistory) = await _tradeProvider.ExecuteExpert(newExpert, timeFrame, objGuid, closeAllOpen, priceTickList).ConfigureAwait(false);
                    if (closedOrders != null)
                    {
                        await _testResultsProvider.SetTestResults(closedOrders, signalHistory, parameters, newExpert.Risk, newExpert.Lots, saveSignalsToFile).ConfigureAwait(false);
                    }

                    if (percent != gap)
                    {
                        lock (_object)
                        {
                            percent = gap;
                            _consoleDecorator.WriteProgressBar((int)Math.Round(percent), true);
                        }
                    }

                    gap += percentStep;
                });
            }

            var now = DateTime.Now;
            var transformedInstrument = instrument.Replace("/", "-");
            var allResults = testResultsInPairs ? await _testResultsProvider.AllResultsInPairs().ConfigureAwait(false) :
                await _testResultsProvider.AllResults(riskLimit).ConfigureAwait(false);
            var instrumentData = await _priceRepository.GetFlatInstrument(instrument, timeFrame).ConfigureAwait(false);
            await AllSaveOrdersToCsv(allResults, timeFrame + "-" + transformedInstrument, now, instrumentData).ConfigureAwait(false);

            var bestProfit = testResultsInPairs ? await _testResultsProvider.BestResultsInPairs(bestProfitParam).ConfigureAwait(false) :
                await _testResultsProvider.BestResults(riskLimit, bestProfitParam).ConfigureAwait(false);
            await MultiSaveOrdersToCsv(bestProfit, timeFrame + "-" + transformedInstrument, now, saveSignalsToFile).ConfigureAwait(false);

            return true;
        }

        private async Task MultiSaveOrdersToCsv(IEnumerable<TestExecutionResults> bestProfit, string folderName, DateTime datetime, bool saveSignalsToFile)
        {
            var configuration = _configurationProvider.GetConfiguration<HistoryConfigurationParameters>();
            await _testResultsProvider.MultiSaveClosedOrdersToCsvAsync(configuration.PathClosedOrders, bestProfit, folderName, datetime, saveSignalsToFile).ConfigureAwait(false);
        }

        private async Task AllSaveOrdersToCsv(IEnumerable<TestExecutionResults> allResults, string folderName, DateTime datetime, InstrumentForStore instrumentData)
        {
            var configuration = _configurationProvider.GetConfiguration<HistoryConfigurationParameters>();
            var avgSpread = 0.0m;
            if (instrumentData.PriceBarList.Any())
            {
                avgSpread = Math.Round(instrumentData.PriceBarList.Select(x => x.Spread).Average() / instrumentData.PointSize, 1);
            }

            await _testResultsProvider.AllSaveClosedOrdersToCsvAsync(configuration.PathClosedOrders, allResults, folderName, datetime, avgSpread).ConfigureAwait(false);
        }

        private async Task<(string, string, DateTime, DateTime)> GetPriceConfigurationParameters(PriceConfiguration priceConfiguration)
        {
            var timeFrameName = priceConfiguration.TimeFrame;
            var instrument = priceConfiguration.Instrument;
            var dtFrom = await _priceConfigurationAdapter.GetFromDateByPriceConfiguration(priceConfiguration.From, priceConfiguration.Year, priceConfiguration.PastDays).ConfigureAwait(false);
            var dtTo = await _priceConfigurationAdapter.GetToDateByPriceConfiguration(priceConfiguration.To, priceConfiguration.Year).ConfigureAwait(false);
            return (timeFrameName, instrument, dtFrom, dtTo);
        }
    }
}
