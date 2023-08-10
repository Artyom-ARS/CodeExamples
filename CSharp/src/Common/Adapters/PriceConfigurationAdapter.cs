using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Constants;

namespace Common.Adapters
{
    public class PriceConfigurationAdapter : IPriceConfigurationAdapter
    {
        public async Task<DateTime> GetFromDateByPriceConfiguration(string fromDate, int year, int pastDays)
        {
            var dtToday = DateTime.Today;
            if (fromDate != null)
            {
                if (DateTime.TryParseExact(fromDate, PlatformParameters.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    return date;
                }
            }

            if (year != 0)
            {
                var dtStartYear = new DateTime(year, 1, 1).AddMinutes(1);
                return dtStartYear;
            }

            if (pastDays != 0)
            {
                var date = dtToday.AddDays(-1 * pastDays);
                return date;
            }

            return dtToday;
        }

        public async Task<DateTime> GetToDateByPriceConfiguration(string toDate, int year)
        {
            var dtToday = DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59);
            if (toDate != null)
            {
                if (DateTime.TryParseExact(toDate, PlatformParameters.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    return date.AddHours(23).AddMinutes(59).AddSeconds(59);
                }
            }

            if (year != 0)
            {
                var dtEndYear = new DateTime(year + 1, 1, 1).AddMinutes(-1);
                return dtEndYear > dtToday ? dtToday : dtEndYear;
            }

            return dtToday;
        }

        public async Task<string> GetFilePath(string path, string instrumentName, string timeFrame, int year)
        {
            string filename;
            if (timeFrame != PlatformParameters.T1TimeFrameName)
            {
                filename = $"{instrumentName}_{timeFrame}_{year}";
            }
            else
            {
                path = $"{path}\\tick";
                filename = $"{instrumentName}_{timeFrame}";
            }

            filename = await RemoveIllegalSymbols(filename).ConfigureAwait(false);

            var filePath = $"{path}\\{filename}.pbh";
            return filePath;
        }

        public async Task<string> GetStorageFilePath(string instrumentName, string timeFrame, int year)
        {
            instrumentName = await RemoveIllegalSymbols(instrumentName).ConfigureAwait(false);

            var filename = $"{instrumentName}/{timeFrame.ToUpper()}/{instrumentName}_{timeFrame.ToUpper()}_{year.ToString()}.pbh";

            return filename;
        }

        private async Task<string> RemoveIllegalSymbols(string filename)
        {
            var illegalInFileName = new Regex(@"[\\/:*?""<>|]");
            filename = illegalInFileName.Replace(filename, string.Empty);
            return filename;
        }
    }
}
