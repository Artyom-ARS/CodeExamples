using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;

namespace HistoryPlatform.Providers
{
    public interface ITestResultsProvider
    {
        Task<bool> MultiSaveClosedOrdersToCsvAsync(string pathClosedOrders, IEnumerable<TestExecutionResults> testResults, string folderName, DateTime datetime, bool saveSignalsToFile);

        Task<bool> AllSaveClosedOrdersToCsvAsync(string pathClosedOrders, IEnumerable<TestExecutionResults> testResults, string folderName, DateTime datetime, decimal avgSpread);

        Task SetTestResults(IReadOnlyCollection<ClosedOrder> closeOrders, IDictionary<DateTime, IDictionary<string, decimal>> signalHistory, IReadOnlyDictionary<string, object> parameters, decimal risk, int lots, bool saveSignalsToFile);

        Task<IEnumerable<TestExecutionResults>> BestResults(int riskLimit, int bestProfit);

        Task<IEnumerable<TestExecutionResults>> BestResultsInPairs(int bestProfit);

        Task<IEnumerable<TestExecutionResults>> AllResults(int riskLimit);

        Task<IEnumerable<TestExecutionResults>> AllResultsInPairs();
    }
}
