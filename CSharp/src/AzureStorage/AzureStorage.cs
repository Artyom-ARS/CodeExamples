using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Common.Facades;
using Common.Logging;
using Common.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace AzureStorage
{
    public class AzureStorage : IAzureStorage
    {
        private const string ContainerName = "growit";
        private readonly IConsole _console;

        public AzureStorage(IConsole console)
        {
            _console = console;
            Logger.SetLogger(LoggerType.StorageLogger);
        }

        public async Task<byte[]> GetPriceDataAsBytes(string fileName)
        {
            byte[] data = await DownloadDataAsBytes(fileName, null).ConfigureAwait(false);

            return data;
        }
        public async Task<Stream> GetPriceDataAsStream(string fileName, string filePathDisk)
        {
            byte[] data = await DownloadDataAsBytes(fileName, filePathDisk).ConfigureAwait(false);
            Stream stream = (data != null) ? new MemoryStream(data) : null;

            return stream;
        }

        private async Task<byte[]> DownloadDataAsBytes(string fileName, string filePathDisk)
        {
            var blob = GetBlob(fileName);
            var blobLocalDateTime = GetBlobAttributes(blob);
            try
            {
                blob.FetchAttributes();
            }
            catch (StorageException exception)
            {
                _console.WriteLine($"WARNING: Instrument {fileName} was not found in Storage.");
                Logger.Log.Error($"Failed to load Instrument {fileName} from storage with exception: {exception}");
                return null;
            }

            if (filePathDisk != null && blobLocalDateTime != DateTime.MinValue)
            {
                var fileTime = File.GetCreationTime(filePathDisk);
                if (blobLocalDateTime != fileTime)
                {
                    blob.DownloadToFile(filePathDisk, FileMode.OpenOrCreate);
                    File.SetCreationTime(filePathDisk, blobLocalDateTime);
                }
            }

            long fileByteLength = blob.Properties.Length;

            byte[] data = new byte[fileByteLength];
            blob.DownloadToByteArray(data, 0);
            return data;
        }

        public CloudBlockBlob GetBlob (string fileName)
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings.Get("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(ContainerName);
            var blob = container.GetBlockBlobReference(fileName);
            
            return blob;
        }

        public DateTime GetBlobAttributes (CloudBlob blob)
        {
            DateTime blobLocalDateTime;
            try
            {
                blob.FetchAttributes();
                blobLocalDateTime = blob.Properties.LastModified.Value.LocalDateTime;
            }
            catch (StorageException)
            {
                 blobLocalDateTime = DateTime.MinValue;
            }
 
            return blobLocalDateTime;
        }

        public async Task UploadData(string fileName, MemoryStream stream)
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings.Get("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(ContainerName);
            var blob = container.GetBlockBlobReference(fileName);

            var bytes = stream.GetBuffer();

            await blob.UploadFromByteArrayAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }
    }
}
