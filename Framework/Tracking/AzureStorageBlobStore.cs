﻿//  ----------------------------------------------------------------------------------
//  Copyright Microsoft Corporation
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  ----------------------------------------------------------------------------------

namespace DurableTask.Tracking
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Azure blob storage to allow save and load large blobs, such as message and session, as a stream using Azure blob container.
    /// </summary>
    public class AzureStorageBlobStore : IOrchestrationServiceBlobStore
    {
        readonly BlobStorageClient blobClient;

        /// <summary>
        /// Creates a new AzureStorageBlobStore using the supplied hub name and connection string
        /// </summary>
        /// <param name="hubName">The hubname for this store</param>
        /// <param name="connectionString">Azure storage connection string</param>
        public AzureStorageBlobStore(string hubName, string connectionString)
        {
            this.blobClient = new BlobStorageClient(hubName, connectionString);
        }

        /// <summary>
        /// Create a storage key based on the creation date.
        /// This key will be used to save and load the stream in external storage when it is too large.
        /// </summary>
        /// <param name="creationDate">The creation date of the blob. Could be DateTime.MinValue if want to use current time.</param>
        /// <returns>A storage key.</returns>
        public string BuildStorageKey(DateTime creationDate)
        {
            return BlobStorageClientHelper.BuildStorageKey(creationDate);
        }

        /// <summary>
        /// Create a storage key based on the orchestrationInstance.
        /// This key will be used to save and load the stream message in external storage when it is too large.
        /// </summary>
        /// <param name="orchestrationInstance">The orchestration instance.</param>
        /// <param name="messageFireTime">The message fire time.</param>
        /// <returns>The created storage key.</returns>
        public string BuildMessageStorageKey(OrchestrationInstance orchestrationInstance, DateTime messageFireTime)
        {
            return BlobStorageClientHelper.BuildMessageStorageKey(
                orchestrationInstance != null ? orchestrationInstance.InstanceId : "null",
                orchestrationInstance != null ? orchestrationInstance.ExecutionId : "null",
                messageFireTime);
        }

        /// <summary>
        /// Create a storage key based on message session.
        /// This key will be used to save and load the stream in external storage when it is too large.
        /// </summary>
        /// <param name="sessionId">The message session Id.</param>
        /// <returns>A storage key.</returns>
        public string BuildSessionStorageKey(string sessionId)
        {
            return BlobStorageClientHelper.BuildSessionStorageKey(sessionId);
        }

        /// <summary>
        /// Save the stream of the message or seesion using key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="stream">The stream of the message or session.</param>
        /// <returns></returns>
        public async Task SaveStreamAsync(string key, Stream stream)
        {
            await this.blobClient.UploadStreamBlob(key, stream);
        }

        /// <summary>
        /// Load the stream of message or seesion from storage using key.
        /// </summary>
        /// <param name="key">Teh storage key.</param>
        /// <returns>The saved stream message or session.</returns>
        public async Task<Stream> LoadStreamAsync(string key)
        {
            return await this.blobClient.DownloadStreamAsync(key);
        }

        /// <summary>
        /// Deletes the Azure blob storage
        /// </summary>
        public async Task DeleteStoreAsync()
        {
            await this.blobClient.DeleteAllContainersAsync();
        }

        /// <summary>
        /// Purges history from storage for a given time threshold
        /// </summary>
        /// <param name="thresholdDateTimeUtc">The datetime in UTC to use as the threshold for purging history</param>
        public async Task PurgeExpiredBlobsAsync(DateTime thresholdDateTimeUtc)
        {
            await this.blobClient.DeleteExpiredContainersAsync(thresholdDateTimeUtc);
        }
    }
}
