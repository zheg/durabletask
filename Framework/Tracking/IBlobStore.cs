//  ----------------------------------------------------------------------------------
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
    /// Interface to allow save and load large blobs, such as message and session, as a stream using a storage store.
    /// </summary>
    interface IBlobStore
    {
        /// <summary>
        /// Create a storage key based.
        /// This key will be used to save and load the stream in external storage when it is too large.
        /// </summary>
        /// <param name="creationDate">The creation date of the blob. Could be DateTime.MinValue if want to use current time.</param>
        /// <returns>A storage key.</returns>
        string BuildStorageKey(DateTime creationDate);

        /// <summary>
        /// Create a storage key based on the orchestrationInstance.
        /// This key will be used to save and load the stream message in external storage when it is too large.
        /// </summary>
        /// <param name="orchestrationInstance">The orchestration instance.</param>
        /// <param name="messageFireTime">The message fire time. Could be DateTime.MinValue.</param>
        /// <returns>A message storage key.</returns>
        string BuildMessageStorageKey(OrchestrationInstance orchestrationInstance, DateTime messageFireTime);

        /// <summary>
        /// Create a storage key based on message session.
        /// This key will be used to save and load the stream in external storage when it is too large.
        /// </summary>
        /// <param name="sessionId">The message session Id.</param>
        /// <returns>A storage key.</returns>
        string BuildSessionStorageKey(string sessionId);

        /// <summary>
        /// Save the stream of the message or seesion using key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="stream">The stream of the message or session.</param>
        /// <returns></returns>
        Task SaveStreamWithKeyAsync(string key, Stream stream);

        /// <summary>
        /// Load the stream of message or seesion from storage using key.
        /// </summary>
        /// <param name="key">Teh storage key.</param>
        /// <returns>The saved stream message or session.</returns>
        Task<Stream> LoadStreamWithKeyAsync(string key);
    }
}
