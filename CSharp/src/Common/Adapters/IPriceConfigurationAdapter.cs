using System;
using System.Threading.Tasks;

namespace Common.Adapters
{
    public interface IPriceConfigurationAdapter
    {
        Task<DateTime> GetFromDateByPriceConfiguration(string fromDate, int year, int pastDays);

        Task<DateTime> GetToDateByPriceConfiguration(string toDate, int year);

        Task<string> GetFilePath(string path, string instrumentName, string timeFrame, int year);

        Task<string> GetStorageFilePath(string instrumentName, string timeFrame, int year);
    }
}
