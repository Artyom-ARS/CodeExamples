using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;


namespace AzureStorage
{
    public interface IAzureStorage
    {
        Task<byte[]> GetPriceDataAsBytes(string fileName);

        Task<Stream> GetPriceDataAsStream(string fileName, string filePathDisk);

        Task UploadData(string fileName, MemoryStream stream);

        CloudBlockBlob GetBlob(string fileName);

        DateTime GetBlobAttributes(CloudBlob blob);
    }
}
