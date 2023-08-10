using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Constants;
using Common.Enums;
using Common.Models;
using PlatformCommon.Providers;

namespace HistoryPlatform.Providers
{
    public class TestResultsProvider : ITestResultsProvider
    {
        private readonly List<TestExecutionResults> _testResults = new List<TestExecutionResults>();
        private readonly ISaveToCsvProvider _saveToCsvProvider;

        public TestResultsProvider(ISaveToCsvProvider saveToCsvProvider)
        {
            _saveToCsvProvider = saveToCsvProvider;
        }

        public async Task<bool> MultiSaveClosedOrdersToCsvAsync(string pathClosedOrders, IEnumerable<TestExecutionResults> testResults, string folderName, DateTime datetime, bool saveSignalsToFile)
        {
            var path = await _saveToCsvProvider.CreateDirectory(pathClosedOrders, folderName, datetime).ConfigureAwait(false) + "\\" + folderName;
            var jj = 1;

            foreach (var result in testResults)
            {
                var (stream, tw) = _saveToCsvProvider.OpenFileToWrite(path + "\\closed" + jj + ".csv");
                foreach (var order in result.ListCloseOrders)
                {
                    var row = await BuildCsvRow(order, 0).ConfigureAwait(false);
                    await _saveToCsvProvider.WriteRow(tw, row).ConfigureAwait(false);
                }

                await _saveToCsvProvider.CloseFile(stream, tw).ConfigureAwait(false);

                if (!saveSignalsToFile || !result.SignalHistory.Any())
                {
                    jj++;
                    continue;
                }

                (stream, tw) = _saveToCsvProvider.OpenFileToWrite(path + "\\signals" + jj + ".csv");
                var signalKeysOrdered = result.SignalHistory.Keys.OrderBy(x => x);

                var headRow = await BuildCsvHead(result.SignalHistory.First().Value).ConfigureAwait(false);
                await _saveToCsvProvider.WriteRow(tw, headRow).ConfigureAwait(false);

                foreach (var signalKey in signalKeysOrdered)
                {
                    var signals = result.SignalHistory[signalKey];
                    var row = await BuildCsvRow(signalKey, signals, 0).ConfigureAwait(false);
                    await _saveToCsvProvider.WriteRow(tw, row).ConfigureAwait(false);
                }

                await _saveToCsvProvider.CloseFile(stream, tw).ConfigureAwait(false);

                jj++;
            }

            return true;
        }

        public async Task<bool> AllSaveClosedOrdersToCsvAsync(string pathClosedOrders, IEnumerable<TestExecutionResults> testResults, string folderName, DateTime datetime, decimal avgSpread)
        {
            if (!testResults.Any())
            {
                return false;
            }

            var path = await _saveToCsvProvider.CreateDirectory(pathClosedOrders, folderName, datetime).ConfigureAwait(false) + "\\" + folderName;
            var instrument = testResults.First().Parameters.First(k => k.Key == "instrument").Value.ToString();
            var (stream, tw) = _saveToCsvProvider.OpenFileToWrite($"{path}\\{instrument.Replace("/", "").ToLowerInvariant()}-profit-{datetime:yyMMdd-HHmm}.csv");
            var i = 0;
            var firstOrderdate = testResults.First().ListCloseOrders.First().OpenTime;
            var lastOrderdate = testResults.First().ListCloseOrders.Last().CloseTime;

            foreach (var result in testResults)
            {
                var combination = result.Parameters;

                if (!combination.Any())
                {
                    continue;
                }

                string row;
                if (i == 0)
                {
                    row = await BuildCsvHead(combination).ConfigureAwait(false);
                    row = $"{firstOrderdate:yyyy-MM-dd} - {lastOrderdate:yyyy-MM-dd}{Environment.NewLine}{row}";
                    await _saveToCsvProvider.WriteRow(tw, row).ConfigureAwait(false);
                    i = 1;
                }

                row = await BuildCsvRow(combination, result.BargainProfit, result.SmartProfit, result.ListCloseOrders.Count, result.Risk, avgSpread, result.OrderProfit).ConfigureAwait(false);
                await _saveToCsvProvider.WriteRow(tw, row).ConfigureAwait(false);
            }

            await _saveToCsvProvider.CloseFile(stream, tw).ConfigureAwait(false);

            return true;
        }

        public async Task SetTestResults(
            IReadOnlyCollection<ClosedOrder> closeOrders,
            IDictionary<DateTime, IDictionary<string, decimal>> signalHistory,
            IReadOnlyDictionary<string, object> parameters,
            decimal risk,
            int lots,
            bool saveSignalsToFile)
        {
            var bargain = closeOrders.Sum(order => order.Profit);
            var validProfitOrders = closeOrders.Where(order => order.Profit >= 0.0m && order.Lots == lots);
            decimal smartSum = 0;
            decimal orderProfit = 0;
            if (validProfitOrders.Any())
            {
                 smartSum = validProfitOrders.Sum(order => order.Profit);
                 orderProfit = validProfitOrders.Where(x => x.Profit > 0.0m).Min(order => order.Profit);
            }

            var profitOrder = new TestExecutionResults
            {
                ListCloseOrders = new List<ClosedOrder>(closeOrders),
                BargainProfit = Math.Round(bargain, 2),
                SmartProfit = Math.Round(smartSum, 2),
                OrderProfit = Math.Round(orderProfit, 1),
                Parameters = parameters,
                Risk = risk,
            };
            if (saveSignalsToFile)
            {
                profitOrder.SignalHistory = signalHistory;
            }

            _testResults.Add(profitOrder);
        }

        public async Task<IEnumerable<TestExecutionResults>> BestResults(int riskLimit, int bestProfit)
        {
            var result = await AllResults(riskLimit).ConfigureAwait(false);
            return result.Take(bestProfit);
        }

        public async Task<IEnumerable<TestExecutionResults>> BestResultsInPairs(int bestProfit)
        {
            var result = await AllResultsInPairs().ConfigureAwait(false);
            return result.Take(bestProfit);
        }

        public async Task<IEnumerable<TestExecutionResults>> AllResults(int riskLimit)
        {
            var result = _testResults.Where(x => x.Risk < riskLimit).OrderByDescending(x => x.SmartProfit).ToList();
            result.AddRange(_testResults.Where(x => x.Risk >= riskLimit).OrderByDescending(x => x.SmartProfit));
            return result;
        }

        public async Task<IEnumerable<TestExecutionResults>> AllResultsInPairs()
        {
            var tempSortedOrdersBuy = _testResults.Where(x => (string)x.Parameters["direction"] == PlatformParameters.Buy).OrderByDescending(x => x.BargainProfit).ToList();
            var tempSortedOrdersSell = _testResults.Where(x => (string)x.Parameters["direction"] == PlatformParameters.Sell).OrderByDescending(x => x.BargainProfit).ToList();

            var buyOrdersParams = tempSortedOrdersBuy.Select((value, index) => new { Value = value, StringParameters = ParametersToString(value.Parameters) }).ToList();
            var sellOrdersParams = tempSortedOrdersSell.Select((value, index) => new { Value = value, StringParameters = ParametersToString(value.Parameters) }).ToList();

            var resultList = new List<TestExecutionResults>();
            var primaryOrdersParams = buyOrdersParams;
            var secondaryOrdersParams = sellOrdersParams;

            while (primaryOrdersParams.Any())
            {
                resultList.Add(primaryOrdersParams[0].Value);
                var secondary = secondaryOrdersParams.FirstOrDefault(x => x.StringParameters == primaryOrdersParams[0].StringParameters);
                primaryOrdersParams.RemoveAt(0);
                if (secondary == null && secondaryOrdersParams.Any())
                {
                    secondary = secondaryOrdersParams[0];
                }

                if (secondary != null)
                {
                    resultList.Add(secondary.Value);
                    secondaryOrdersParams.Remove(secondary);
                    (primaryOrdersParams, secondaryOrdersParams) = (secondaryOrdersParams, primaryOrdersParams);
                }
            }

            if (!primaryOrdersParams.Any() && secondaryOrdersParams.Any())
            {
                resultList.Add(secondaryOrdersParams[0].Value);
            }

            return resultList;
        }

        private string ParametersToString(IReadOnlyDictionary<string, object> parameters)
        {
            var result = string.Join(";", parameters.Where(x => x.Key != "Id" && x.Key != "direction" && x.Key != "disabled").Select(x => x.Value));
            return result;
        }

        private async Task<string> BuildCsvRow(IReadOnlyDictionary<string, object> combination, decimal profit, decimal smartProfit, int count, decimal risk, decimal avgSpread, decimal avgProfit)
        {
            var row = string.Empty;
            foreach (var keyValuePair in combination)
            {
                if (keyValuePair.Key == "Id" || keyValuePair.Key == "disabled" || keyValuePair.Key == "spreadLimit" || keyValuePair.Key == "AssemblyName")
                {
                    continue;
                }

                if (keyValuePair.Value != null)
                {
                    row += $"{keyValuePair.Value};";
                }
            }

            row = $"{profit};" +
                  $"{smartProfit};" +
                  $"{count};" +
                  $"{risk};" +
                  $"{avgSpread};" +
                  $"{avgProfit};{row}";

            return row;
        }

        private async Task<string> BuildCsvHead(IReadOnlyDictionary<string, object> combination)
        {
            var row = string.Empty;
            foreach (var keyValuePair in combination)
            {
                if (keyValuePair.Key == "Id" || keyValuePair.Key == "disabled" || keyValuePair.Key == "spreadLimit" || keyValuePair.Key == "AssemblyName")
                {
                    continue;
                }

                if (keyValuePair.Value != null)
                {
                    row += $"{keyValuePair.Key};";
                }
            }

            row = $"Profit;ProfitExt;Count;Risk;Spread;Order profit;{row}";

            return row;
        }

        private async Task<string> BuildCsvRow(ClosedOrder order, int addHours)
        {
            var row = $"{order.Instrument};" +
                $"{order.OpenTime.AddHours(addHours):yyyy.MM.dd HH:mm:ss};" +
                $"{order.CloseTime.AddHours(addHours):yyyy.MM.dd HH:mm:ss};" +
                $"{order.OpenPrice};" +
                $"{order.ClosePrice};" +
                $"{(order.BuySell == OrderBuySell.Buy ? PlatformParameters.Buy : PlatformParameters.Sell)};" +
                $"{order.Lots};" +
                $"{order.Profit}";

            return row;
        }

        private async Task<string> BuildCsvRow(DateTime signalKey, IDictionary<string, decimal> signals, int addHours)
        {
            var row = $"{signalKey.AddHours(addHours):yyyy.MM.dd HH:mm:ss};";
            foreach (var item in signals)
            {
                row += $"{item.Value:0.00000};";
            }

            return row;
        }

        private async Task<string> BuildCsvHead(IDictionary<string, decimal> signals)
        {
            var row = "DateTime;";
            foreach (var key in signals.Keys)
            {
                row += $"{key};";
            }

            return row;
        }
    }
}
