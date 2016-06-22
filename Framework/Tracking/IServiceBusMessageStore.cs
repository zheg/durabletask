namespace DurableTask.Tracking
{
    using System.IO;
    using System.Threading.Tasks;
    /// <summary>
    /// Interface to allow save and load service bus message as a stream using a storage store.
    /// </summary>
    interface IServiceBusMessageStore
    {
        /// <summary>
        /// Create a storage key based on the orchestrationInstance.
        /// This key will be used to save and load the stream message in external storage when it is too large.
        /// </summary>
        /// <param name="orchestrationInstance">The orchestration instance.</param>
        /// <returns></returns>
        string BuildMessageStorageKey(OrchestrationInstance orchestrationInstance);

        /// <summary>
        /// Save the stream of the service bus message using key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="stream">The stream of the service bus message.</param>
        /// <returns></returns>
        Task SaveSteamMessageWithKey(string key, Stream stream);

        /// <summary>
        /// Load the stream message from storage using key.
        /// </summary>
        /// <param name="key">Teh storage key.</param>
        /// <returns>The saved stream message.</returns>
        Task<Stream> LoadSteamMessageWithKey(string key);
    }
}
