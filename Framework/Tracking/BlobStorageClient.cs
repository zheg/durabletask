namespace DurableTask.Tracking
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using System.Collections.Generic;
    using System.Linq;

    internal class BlobStorageClient
    {
        // use hubName as the container prefix. 
        // the container full name is in the format of {hubName}-{DateTime};
        // the date time is in the format of yyyyMMdd.
        readonly string hubName;
        readonly CloudBlobClient blobClient;

        const int MaxRetries = 3;
        static readonly TimeSpan MaximumExecutionTime = TimeSpan.FromSeconds(30);
        static readonly TimeSpan DeltaBackOff = TimeSpan.FromSeconds(5);

        public BlobStorageClient(string hubName, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Invalid connection string", "connectionString");
            }

            if (string.IsNullOrEmpty(hubName))
            {
                throw new ArgumentException("Invalid hub name", "hubName");
            }

            blobClient = CloudStorageAccount.Parse(connectionString).CreateCloudBlobClient();
            blobClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(DeltaBackOff,
               MaxRetries);
            blobClient.DefaultRequestOptions.MaximumExecutionTime = MaximumExecutionTime;

            this.hubName = hubName;
        }

        public async Task UploadStreamBlob(string key, Stream stream)
        {
            string dateString;
            string blobName;
            ParseKey(key, out dateString, out blobName);
            var cloudBlob = await GetCloudBlockBlobReferenceAsync(dateString, blobName);
            await cloudBlob.UploadFromStreamAsync(stream);
        }

        public async Task<Stream> DownloadStreamAsync(string key)
        {
            string dateString;
            string blobName;
            ParseKey(key, out dateString, out blobName);

            var blob = await this.GetCloudBlockBlobReferenceAsync(dateString, blobName);
            Stream targetStream = new MemoryStream();
            await blob.DownloadToStreamAsync(targetStream);
            return targetStream;
        }

        void ParseKey(string key, out string dateString, out string blobName)
        {
            string[] segments = key.Split(BlobStorageClientHelper.KeyDelimiter);
            if (segments.Length < 2)
            {
                throw new ArgumentException("storage key does not contain required 2 or more segments: {dateString}|{blobName}.", nameof(key));
            }
            dateString = segments[0];
            blobName = key.Substring(dateString.Length + 1, key.Length - dateString.Length - 1);
        }

        async Task<ICloudBlob> GetCloudBlockBlobReferenceAsync(string dateString, string blobName)
        {
            string containerName = BlobStorageClientHelper.BuildContainerName(hubName, dateString);        
            var cloudBlobContainer = blobClient.GetContainerReference(containerName);
            await cloudBlobContainer.CreateIfNotExistsAsync();
            return cloudBlobContainer.GetBlockBlobReference(blobName);
        }

        public IEnumerable<CloudBlobContainer> ListContainers()
        {
            return blobClient.ListContainers(prefix : hubName);
        }

        public async Task DeleteExpiredContainersAsync(DateTime thresholdDateTimeUtc)
        {
            IEnumerable<CloudBlobContainer> containers = ListContainers();
            var tasks = containers.Where(container => BlobStorageClientHelper.IsContainerExpired(container.Name, thresholdDateTimeUtc)).ToList().Select(container => container.DeleteIfExistsAsync());
            await Task.WhenAll(tasks);
        }

        public async Task DeleteAllContainersAsync()
        {
            IEnumerable<CloudBlobContainer> containers = ListContainers();
            var tasks = containers.ToList().Select(container => container.DeleteIfExistsAsync());
            await Task.WhenAll(tasks);
        }
    }
}
