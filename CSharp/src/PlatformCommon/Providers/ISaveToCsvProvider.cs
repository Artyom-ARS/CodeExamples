using System;
using System.IO;
using System.Threading.Tasks;

namespace PlatformCommon.Providers
{
    public interface ISaveToCsvProvider
    {
        Task CloseFile(Stream stream, TextWriter tw);

        (Stream stream, TextWriter tw) OpenFileToWrite(string filePath);

        Task WriteRow(TextWriter tw, string row);

        Task<string> CreateDirectory(string pathClosedOrders, string folderName, DateTime datetime);
    }
}
