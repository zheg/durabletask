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

namespace DurableTask.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using DurableTask.Settings;
    using DurableTask.Tracing;
    using DurableTask.Tracking;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    internal static class ServiceBusUtils
    {
        public static async Task<BrokeredMessage> GetBrokeredMessageFromObjectAsync(object serializableObject, CompressionSettings compressionSettings)
        {
            return await GetBrokeredMessageFromObjectAsync(serializableObject, compressionSettings, new ServiceBusMessageSettings(), null, null, null, DateTime.MinValue);
        }

        public static async Task<BrokeredMessage> GetBrokeredMessageFromObjectAsync(
            object serializableObject,
            CompressionSettings compressionSettings,
            ServiceBusMessageSettings messageSettings,
            OrchestrationInstance instance,
            string messageType,
            IBlobStore blobStore,
            DateTime messageFireTime)
        {
            if (serializableObject == null)
            {
                throw new ArgumentNullException(nameof(serializableObject));
            }

            if (compressionSettings.Style == CompressionStyle.Legacy)
            {
                return new BrokeredMessage(serializableObject) { SessionId = instance?.InstanceId };
            }

            bool disposeStream = true;
            var rawStream = new MemoryStream();

            Utils.WriteObjectToStream(rawStream, serializableObject);

            try
            {
                BrokeredMessage brokeredMessage = null;

                if (compressionSettings.Style == CompressionStyle.Always ||
                    (compressionSettings.Style == CompressionStyle.Threshold &&
                     rawStream.Length > compressionSettings.ThresholdInBytes))
                {
                    Stream compressedStream = Utils.GetCompressedStream(rawStream);
                    var rawLen = rawStream.Length;
                    TraceHelper.TraceInstance(TraceEventType.Information, instance,
                        () =>
                            "Compression stats for " + (messageType ?? string.Empty) + " : " +
                            brokeredMessage.MessageId +
                            ", uncompressed " + rawLen + " -> compressed " + compressedStream.Length);

                    if (compressedStream.Length < messageSettings.MaxMessageSizeInBytes)
                    {
                        brokeredMessage = new BrokeredMessage(compressedStream, true);
                        brokeredMessage.Properties[FrameworkConstants.CompressionTypePropertyName] =
                            FrameworkConstants.CompressionTypeGzipPropertyValue;
                    }
                    else
                    {
                        brokeredMessage = await GenerateBrokeredMessageWithStorageKeyPropertyAsync(compressedStream, blobStore, instance, messageSettings, messageFireTime, FrameworkConstants.CompressionTypeGzipPropertyValue);
                    }
                }
                else
                {
                    if (rawStream.Length < messageSettings.MaxMessageSizeInBytes)
                    {
                        brokeredMessage = new BrokeredMessage(rawStream, true);
                        disposeStream = false;
                        brokeredMessage.Properties[FrameworkConstants.CompressionTypePropertyName] =
                            FrameworkConstants.CompressionTypeNonePropertyValue;
                    }
                    else
                    {
                        brokeredMessage = await GenerateBrokeredMessageWithStorageKeyPropertyAsync(rawStream, blobStore, instance, messageSettings, messageFireTime, FrameworkConstants.CompressionTypeNonePropertyValue);
                    }
                }

                brokeredMessage.SessionId = instance?.InstanceId;
                // TODO : Test more if this helps, initial tests shows not change in performance
                // brokeredMessage.ViaPartitionKey = instance?.InstanceId;
                
                return brokeredMessage;
            }
            finally
            {
                if (disposeStream)
                {
                    rawStream.Dispose();
                }
            }
        }

        static async Task<BrokeredMessage> GenerateBrokeredMessageWithStorageKeyPropertyAsync(
            Stream stream,
            IBlobStore blobStore,
            OrchestrationInstance instance,
            ServiceBusMessageSettings messageSettings,
            DateTime messageFireTime,
            string compressionType)
        {
            if (stream.Length > messageSettings.MaxMessageSizeForBlobInBytes)
            {
                throw new ArgumentException(
                    $"The serialized message size {stream.Length} is larger than the supported external storage blob size {messageSettings.MaxMessageSizeForBlobInBytes}.",
                    "stream");
            }

            if (blobStore == null)
            {
                throw new ArgumentException(
                    "Please provide an implementation of IBlobStore for external storage.",
                    "blobStore");
            }

            // save the compressed stream using external storage when it is larger
            // than the supported message size limit.
            // the stream is stored using the generated key, which is saved in the message property.
            string storageKey = blobStore.BuildMessageStorageKey(instance, messageFireTime);

            TraceHelper.TraceInstance(
                TraceEventType.Information,
                instance,
                () => "Saving the message stream in blob storage using key {storageKey}.");
            await blobStore.SaveStreamWithKeyAsync(storageKey, stream);

            BrokeredMessage brokeredMessage = new BrokeredMessage();
            brokeredMessage.Properties[FrameworkConstants.MessageStorageKey] = storageKey;
            brokeredMessage.Properties[FrameworkConstants.CompressionTypePropertyName] = compressionType;

            return brokeredMessage;
        }

        public static async Task<T> GetObjectFromBrokeredMessageAsync<T>(BrokeredMessage message, IBlobStore blobStore)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            T deserializedObject;

            object compressionTypeObj = null;
            string compressionType = string.Empty;

            if (message.Properties.TryGetValue(FrameworkConstants.CompressionTypePropertyName, out compressionTypeObj))
            {
                compressionType = (string)compressionTypeObj;
            }

            if (string.IsNullOrEmpty(compressionType))
            {
                // no compression, legacy style
                deserializedObject = message.GetBody<T>();
            }
            else if (string.Equals(compressionType, FrameworkConstants.CompressionTypeGzipPropertyValue,
                StringComparison.OrdinalIgnoreCase))
            {
                using (var compressedStream = await LoadMessageStream(message, blobStore))
                {
                    if (!Utils.IsGzipStream(compressedStream))
                    {
                        throw new ArgumentException(
                            $"message specifies a CompressionType of {compressionType} but content is not compressed",
                            nameof(message));
                    }

                    using (Stream objectStream = await Utils.GetDecompressedStreamAsync(compressedStream))
                    {
                        deserializedObject = Utils.ReadObjectFromStream<T>(objectStream);
                    }
                }
            }
            else if (string.Equals(compressionType, FrameworkConstants.CompressionTypeNonePropertyValue,
                StringComparison.OrdinalIgnoreCase))
            {
                using (var rawStream = await LoadMessageStream(message, blobStore))
                {
                    deserializedObject = Utils.ReadObjectFromStream<T>(rawStream);
                }
            }
            else
            {
                throw new ArgumentException(
                    $"message specifies an invalid CompressionType: {compressionType}",
                    nameof(message));
            }

            return deserializedObject;
        }

        static async Task<Stream> LoadMessageStream(BrokeredMessage message, IBlobStore blobStore)
        {
            object storageKeyObj = null;
            string storageKey = string.Empty;

            if (message.Properties.TryGetValue(FrameworkConstants.MessageStorageKey, out storageKeyObj))
            {
                storageKey = (string)storageKeyObj;
            }
            if (string.IsNullOrEmpty(storageKey))
            {
                // load the stream from the message directly if the storage key property is not set,
                // i.e., it is not stored externally
                return message.GetBody<Stream>();
            }

            // if the storage key is set in the message property,
            // load the stream message from the service bus message store.
            if (blobStore == null)
            {
                throw new ArgumentException($"Failed to load compressed message from external storage with key: {storageKey}. Please provide an implementation of IServiceBusMessageStore for external storage.", nameof(IBlobStore));
            }

            return await blobStore.LoadStreamWithKeyAsync(storageKey);

        }

        public static void CheckAndLogDeliveryCount(string sessionId, IEnumerable<BrokeredMessage> messages, int maxDeliverycount)
        {
            foreach (BrokeredMessage message in messages)
            {
                CheckAndLogDeliveryCount(sessionId, message, maxDeliverycount);
            }
        }

        public static void CheckAndLogDeliveryCount(IEnumerable<BrokeredMessage> messages, int maxDeliverycount)
        {
            foreach (BrokeredMessage message in messages)
            {
                CheckAndLogDeliveryCount(message, maxDeliverycount);
            }
        }

        public static void CheckAndLogDeliveryCount(BrokeredMessage message, int maxDeliverycount)
        {
            CheckAndLogDeliveryCount(null, message, maxDeliverycount);
        }

        public static void CheckAndLogDeliveryCount(string sessionId, BrokeredMessage message, int maxDeliveryCount)
        {
            if (message.DeliveryCount >= maxDeliveryCount - 2)
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    TraceHelper.TraceSession(TraceEventType.Critical, sessionId,
                        "Delivery count for message with id {0} is {1}. Message will be deadlettered if processing continues to fail.",
                        message.MessageId, message.DeliveryCount);
                }
                else
                {
                    TraceHelper.Trace(TraceEventType.Critical,
                        "Delivery count for message with id {0} is {1}. Message will be deadlettered if processing continues to fail.",
                        message.MessageId, message.DeliveryCount);
                }
            }
        }

        public static MessagingFactory CreateMessagingFactory(string connectionString)
        {
            MessagingFactory factory = MessagingFactory.CreateFromConnectionString(connectionString);
            factory.RetryPolicy = RetryPolicy.Default;
            return factory;
        }
    }
}
