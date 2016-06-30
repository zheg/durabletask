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
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using DurableTask.Serializing;
    using DurableTask.Tracking;

    /// <summary>
    /// A helper class to save and load an object using external storage.
    /// </summary>
    class DurableTaskBlobHelper<T>
    {

        /// <summary>
        /// Serialize an object and use the blob store to save it externally with the generated key.
        /// The key is hold in the returned result for later retrieval.
        /// </summary>
        /// <param name="blobStore">The blob store for saving the serialized object</param>
        /// <param name="originalObject">The original object to be serialized and stored externally</param>
        /// <param name="creationDate">The object creation date. Could be DateTime.MinValue for default current date.</param>
        /// <returns>A <see cref="P:DurableTask.Blob.DurableTaskBlob"/> object as the saving result that hold the storage key.</returns>
        public static async Task<DurableTaskBlob<T>> SaveDurableTaskBlob(IBlobStore blobStore, T originalObject, DateTime creationDate)
        {
            string key = blobStore.BuildStorageKey(creationDate);
            DurableTaskBlob<T> durableTaskBlob = new DurableTaskBlob<T>();
            durableTaskBlob.StorageKey = key;

            Stream stream = Serialize(originalObject);
            await blobStore.SaveStreamWithKeyAsync(key, stream);
            return durableTaskBlob;
        }

        /// <summary>
        /// Load an object from external blob storage.
        /// </summary>
        /// <param name="blobStore">The blob store for loading the serialized object</param>
        /// <param name="durableTaskBlob">A <see cref="P:DurableTask.Blob.DurableTaskBlob"/> object that hold the storage key</param>
        /// <returns>An object loaded from external storage, using the storage key in durableTaskBlob</returns>
        public static async Task<T> LoadDurableTaskBlob(IBlobStore blobStore, DurableTaskBlob<T> durableTaskBlob)
        {
            Stream stream = await blobStore.LoadStreamWithKeyAsync(durableTaskBlob.StorageKey);
            return Deserialize(stream);
        }

        static Stream Serialize(T t)
        {
            if (t is string)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(t as string));
            }
            if (t is Stream)
            {
                return t as Stream;
            }
            JsonDataConverter jsonDataConverter = new JsonDataConverter();
            return new MemoryStream(Encoding.UTF8.GetBytes(jsonDataConverter.Serialize(t)));
        }

        static T Deserialize(Stream stream)
        {
            if (stream is T)
            {
                return (T)(object)stream;
            }

            StreamReader reader = new StreamReader(stream);
            if (typeof (T) == typeof (string))
            {
                return (T)(object)reader.ReadToEnd();
            }

            JsonDataConverter jsonDataConverter = new JsonDataConverter();          
            return jsonDataConverter.Deserialize<T>(reader.ReadToEnd());
        }
    }
}
