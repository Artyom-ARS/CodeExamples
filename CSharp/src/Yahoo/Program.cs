using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Yahoo.Models;
using Yahoo.Providers;
using YahooQuotesApi;

namespace Yahoo
{
    class Program
    {
        private static readonly List<Security> ScreenerCache = new();
        private static readonly List<string> SymbolsList = new();
        private static readonly DateTime Today = DateTime.Today;

        static async Task Main()
        {
            var conf = new ConfigurationProvider();
            var configuration = conf.GetConfiguration<HistoryConfigurationParameters>();

            await ReadCsvAsync(configuration.NameFile);
            var timer = new Stopwatch();
            timer.Start();
            await GetScreenerDataAsync(configuration.StopFor);
            timer.Stop();
            await SaveScreenerDataToCsvAsync(configuration.Path);
            var timeTaken = timer.Elapsed;
            Console.Write( "Time taken: " + timeTaken.ToString(@"m\:ss\.fff"));
        }

        private static async Task ReadCsvAsync(string nameFile)
        {
            using var reader = new StreamReader(Environment.CurrentDirectory + @"\" + nameFile);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) continue;
                var values = line.Split(',');

                SymbolsList.Add(values[0]);
            }
        }

        private static async Task GetScreenerDataAsync(int stopFor)
        {
            for (var i = 1; i < SymbolsList.Count; i++)
            {
                var symbol = SymbolsList[i];
                try
                {
                    var task = await GetDataAsync(symbol);
                    ScreenerCache.Add(task);
                }
                catch
                {
                    continue;
                }

                if (i == stopFor) break;
            }
        }

        private static async Task<Security> GetDataAsync(string symbol)
        {
            var security = await new YahooQuotesBuilder().Build().GetAsync(symbol);
            if (security is null) throw new ArgumentException("Unknown symbol:" + symbol);
            return security;
        }

        public static async Task<bool> SaveScreenerDataToCsvAsync(string folderPath)
        {
            var saveToCsvProvider = new SaveToCsvProvider();
            var path = saveToCsvProvider.CreateDirectory(folderPath, "", Today);
            var (stream, tw) = saveToCsvProvider.OpenFileToWrite(path + "\\screener.csv");

            saveToCsvProvider.WriteRow(tw, saveToCsvProvider.BuildCsvRow(ScreenerCache[0], 0));
            foreach (var value in ScreenerCache)
            {
                var row = saveToCsvProvider.BuildCsvRow(value, 1);
                saveToCsvProvider.WriteRow(tw, row);
            }
            saveToCsvProvider.CloseFile(stream, tw);
            return true;
        }
    }
}
