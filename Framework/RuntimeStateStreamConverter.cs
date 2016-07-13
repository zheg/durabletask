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

namespace DurableTask
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using DurableTask.Common;
    using DurableTask.History;
    using DurableTask.Serializing;
    using DurableTask.Tracing;
    using DurableTask.Tracking;

    /// <summary>
    /// A converter that does conversion between the OrchestrationRuntimeState instance and a stream.
    /// The stream is a serialized OrchestrationSessionState that will set as session state.
    /// De-serialization is done with fallbacks in the order: OrchestrationSessionState -> OrchestrationRuntimeState -> IList<HistoryEvent>.
    /// </summary>
    class RuntimeStateStreamConverter
    {
        public static async Task<Stream> OrchestrationRuntimeStateToRawStream(OrchestrationRuntimeState newOrchestrationRuntimeState,
            OrchestrationRuntimeState runtimeState, DataConverter dataConverter, bool shouldCompress, long sessionStreamTerminationThresholdInBytes,
            long sessionStreamExternalStorageThresholdInBytes, IBlobStore blobStore, string sessionId)
        {
            OrchestrationSessionState orchestrationSessionState = new OrchestrationSessionState(newOrchestrationRuntimeState.Events);
            string serializedState = dataConverter.Serialize(orchestrationSessionState);

            long originalStreamSize = 0;
            Stream compressedState = Utils.WriteStringToStream(
                serializedState,
                shouldCompress,
                out originalStreamSize);

            runtimeState.Size = originalStreamSize;
            runtimeState.CompressedSize = compressedState.Length;

            if (runtimeState.CompressedSize > sessionStreamTerminationThresholdInBytes)
            {
                throw new ArgumentException($"Session state size of {runtimeState.CompressedSize} exceeded the termination threshold of {sessionStreamTerminationThresholdInBytes} bytes");
            }

            if (runtimeState.CompressedSize > sessionStreamExternalStorageThresholdInBytes)
            {
                return await CreateStreamForExternalStorageAsync(shouldCompress,
                        blobStore, sessionId, dataConverter, compressedState);
            }

            return compressedState;
        }

        async static Task<Stream> CreateStreamForExternalStorageAsync(bool shouldCompress,
            IBlobStore blobStore, string sessionId, DataConverter dataConverter, Stream compressedState)
        {
            if (blobStore == null)
            {
                throw new ArgumentException($"The compressed session is larger than supported. " +
                                                    $"Please provide an implementation of IBlobStore for external storage.",
                            nameof(IBlobStore));
            }

            // create a new orchestration session state with the external storage key
            string key = blobStore.BuildSessionStorageKey(sessionId);
            OrchestrationSessionState orchestrationSessionState = new OrchestrationSessionState(key);

            string serializedStateExternal = dataConverter.Serialize(orchestrationSessionState);
            long streamSize;
            Stream compressedStateForSession = Utils.WriteStringToStream(
                serializedStateExternal,
                shouldCompress,
                out streamSize);

            await blobStore.SaveStreamWithKeyAsync(key, compressedState);
            return compressedStateForSession;
        }


        public static async Task<OrchestrationRuntimeState> RawStreamToRuntimeState(Stream rawSessionStream, string sessionId, IBlobStore blobStore, DataConverter dataConverter)
        {
            bool isEmptySession;
            OrchestrationRuntimeState runtimeState;
            Stream sessionStream = await Utils.GetDecompressedStreamAsync(rawSessionStream);

            isEmptySession = sessionStream == null;
            long rawSessionStateSize = isEmptySession ? 0 : rawSessionStream.Length;
            long newSessionStateSize = isEmptySession ? 0 : sessionStream.Length;

            string storageKey;
            runtimeState = GetOrCreateInstanceState(sessionStream, sessionId, dataConverter, out storageKey);

            if (string.IsNullOrWhiteSpace(storageKey))
            {         
                TraceHelper.TraceSession(TraceEventType.Information, sessionId,
                    $"Size of session state is {newSessionStateSize}, compressed {rawSessionStateSize}");
                return runtimeState;
            }

            if (blobStore == null)
            {
                throw new ArgumentException($"Please provide an implementation of IBlobStore for external storage to load the runtime state.",
                            nameof(IBlobStore));
            }
            Stream externalStream = await blobStore.LoadStreamWithKeyAsync(storageKey);
            return await RawStreamToRuntimeState(externalStream, sessionId, blobStore, dataConverter);
        }

        static OrchestrationRuntimeState GetOrCreateInstanceState(Stream stateStream, string sessionId, DataConverter dataConverter, out string storageKey)
        {
            OrchestrationRuntimeState runtimeState;
            storageKey = string.Empty;
            if (stateStream == null)
            {
                TraceHelper.TraceSession(TraceEventType.Information, sessionId,
                    "No session state exists, creating new session state.");
                runtimeState = new OrchestrationRuntimeState();
            }
            else
            {
                if (stateStream.Position != 0)
                {
                    throw TraceHelper.TraceExceptionSession(TraceEventType.Error, sessionId,
                        new ArgumentException("Stream is partially consumed"));
                }

                string serializedState = null;
                using (var reader = new StreamReader(stateStream))
                {
                    serializedState = reader.ReadToEnd();
                }

                runtimeState = DeserializeToRuntimeStateWithFallback(serializedState, dataConverter, sessionId, out storageKey);
            }

            return runtimeState;
        }

        static OrchestrationRuntimeState DeserializeToRuntimeStateWithFallback(string serializedState, DataConverter dataConverter, string sessionId, out string storageKey)
        {
            OrchestrationRuntimeState runtimeState = null;
            storageKey = string.Empty;
            try
            {
                OrchestrationSessionState sessionState =
                    dataConverter.Deserialize<OrchestrationSessionState>(serializedState);
                runtimeState = new OrchestrationRuntimeState(sessionState.Events);
                storageKey = sessionState.StorageKey;
            }
            catch (Exception exception)
            {
                TraceHelper.TraceSession(TraceEventType.Warning, sessionId,
                   $"Failed to deseriazlize session state to OrchestrationSessionState object: {serializedState}");
                try
                {
                    OrchestrationRuntimeState restoredState =
                        dataConverter.Deserialize<OrchestrationRuntimeState>(serializedState);
                    // Create a new Object with just the events, we don't want the rest
                    runtimeState = new OrchestrationRuntimeState(restoredState.Events);
                }
                catch (Exception e)
                {
                    TraceHelper.TraceSession(TraceEventType.Warning, sessionId,
                        $"Failed to deseriazlize session state to OrchestrationRuntimeState object: {serializedState}");
                    IList<HistoryEvent> events = dataConverter.Deserialize<IList<HistoryEvent>>(serializedState);
                    runtimeState = new OrchestrationRuntimeState(events);
                }
            }
            return runtimeState;
        }
    }
}
