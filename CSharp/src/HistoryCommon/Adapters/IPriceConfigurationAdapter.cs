using System;

namespace HistoryCommon.Adapters
{
    public interface IPriceConfigurationAdapter
    {
        DateTime GetFromDateByPriceConfiguration(string fromDate, int year, int pastDays);

        DateTime GetToDateByPriceConfiguration(string toDate, int year);

        string GetFilePath(string path, string instrumentName, string timeFrame, int year);

        string GetStorageFilePath(string instrumentName, string timeFrame, int year);
    }
}
