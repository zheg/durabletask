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

namespace DurableTask.Blob
{
    using System.Collections.Generic;

    /// <summary>
    ///     A wrapper class that represents the result of an externally stored object after serialization.
    ///     The object with the storage key can be used for fetching the original object.
    ///     The <see cref="P:DurableTask.Blob.DurableTaskBlobHelper"/> is the helper class to save/load the serialized object.
    /// </summary>
    class DurableTaskBlob<T>
    {
        public IDictionary<string, string> Metadata { get; set; }
        public string StorageKey { get; set; }
    }
}
