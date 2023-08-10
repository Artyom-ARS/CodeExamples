using System;
using System.IO;
using System.Threading.Tasks;

namespace PlatformCommon.Providers
{
    public class SaveToCsvProvider : ISaveToCsvProvider
    {
        public (Stream stream, TextWriter tw) OpenFileToWrite(string filePath)
        {
            Stream stream = new FileStream(filePath, FileMode.Create);
            TextWriter tw = new StreamWriter(stream);
            return (stream, tw);
        }

        public async Task CloseFile(Stream stream, TextWriter tw)
        {
            tw.Dispose();
            stream.Close();
            stream.Dispose();
        }

        public async Task WriteRow(TextWriter tw, string row)
        {
            tw.WriteLine(row);
        }

        public async Task<string> CreateDirectory(string pathClosedOrders, string folderName, DateTime datetime)
        {
            var folderPath = $"{Path.GetDirectoryName(pathClosedOrders)}\\test-results-{datetime:yyMMdd-HHmm}";
            var exists = Directory.Exists(folderPath + "\\" + folderName);

            if (!exists)
            {
                Directory.CreateDirectory(folderPath + "\\" + folderName);
            }

            return folderPath;
        }
    }
}
