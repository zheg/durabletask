using System;

namespace DurableTask.Tracking
{
    using System.IO;
    using System.Threading.Tasks;
    /// <summary>
    /// Interface to allow save and load message and session as a stream using a storage store.
    /// </summary>
    interface IMessageSessionStore
    {
        /// <summary>
        /// Create a storage key based on the orchestrationInstance.
        /// This key will be used to save and load the stream message in external storage when it is too large.
        /// </summary>
        /// <param name="orchestrationInstance">The orchestration instance.</param>
        /// <param name="messageFireTime">The message fire time. Could be DateTime.MinValue.</param>
        /// <returns>A message storage key.</returns>
        string BuildMessageStorageKey(OrchestrationInstance orchestrationInstance, DateTime messageFireTime);

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
