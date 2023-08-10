using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using YahooQuotesApi;

namespace Yahoo.Providers
{
    public class SaveToCsvProvider
    {
        public (Stream stream, TextWriter tw) OpenFileToWrite(string filePath)
        {
            Stream stream = new FileStream(filePath, FileMode.Create);
            TextWriter tw = new StreamWriter(stream);
            return (stream, tw);
        }

        public void CloseFile(Stream stream, TextWriter tw)
        {
            tw.Dispose();
            stream.Close();
            stream.Dispose();
        }

        public void WriteRow(TextWriter tw, string row)
        {
            tw.WriteLine(row);
        }

        public string CreateDirectory(string pathClosedOrders, string folderName, DateTime datetime)
        {
            var folderPath = $"{Path.GetDirectoryName(pathClosedOrders)}\\screener-results-{datetime:yyMMdd-HHmm}";
            var exists = Directory.Exists(folderPath + "\\" + folderName);

            if (!exists)
            {
                Directory.CreateDirectory(folderPath + "\\" + folderName);
            }

            return folderPath;
        }

        public string BuildCsvRow(Security parameters, int head)
        {
            string row = default;
            foreach (var v in parameters.GetType().GetProperties())
            {
                try
                {
                    if (head == 0)
                    {
                        row += v.Name + ";";
                    }
                    else
                    {
                        var result = v.GetValue(parameters, null) + ";" ?? "0" + ";";
                        row += result.ToString();
                    }
                }
                catch (Exception) { }
            }
            return row;
        }
    }
}
