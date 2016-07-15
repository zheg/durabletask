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
    using System.Collections.Generic;
    using DurableTask.History;

    /// <summary>
    /// The object that represents the serialized session state.
    /// It holds a list of history events (when storage key is empty),
    /// or a key for external storage if the serialized stream is too large to fit into the the session state.
    /// </summary>
    internal class OrchestrationSessionState
    {
        /// <summary>
        /// A constructor for deserialzation.
        /// </summary>
        public OrchestrationSessionState()
        {
        }

        public OrchestrationSessionState(IList<HistoryEvent> events)
        {
            this.Events = events;
        }

        public OrchestrationSessionState(string storageKey)
        {
            this.StorageKey = storageKey;

            // generate history events only for serialization/deserialization;
            // the actual history events are stored exterally with the storage key.
            this.Events = new List<HistoryEvent>();
            this.Events.Add(new ExecutionStartedEvent(-1, string.Empty));
        }

        /// <summary>
        /// List of all history events for runtime state
        /// </summary>
        public IList<HistoryEvent> Events { get; set; }

        /// <summary>
        /// The storage key for external storage. Could be null or empty if not externally stored.
        /// </summary>
        public string StorageKey { get; set; }
    }
}
