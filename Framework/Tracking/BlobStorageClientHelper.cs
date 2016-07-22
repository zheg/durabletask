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

    /// <summary>
    /// A helper class for the Azure blob storage client.
    /// </summary>
    public class BlobStorageClientHelper
    {
        static readonly string DateFormat = "yyyyMMdd";
        static readonly char ContainerDelimiter = '-';

        // the blob storage accesss key is in the format of {DateTime}|{bolbName}
        public static readonly char KeyDelimiter = '|';

        // the delimiter shown in the blob name as the file path
        public static readonly char BlobNameDelimiter = '/';

        /// <summary>
        /// Build a storage key using the creation time specified.
        /// </summary>
        /// <param name="blobCreationTime">The specified creation time</param>
        /// <returns>The constructed storage key.</returns>
        public static string BuildStorageKey(DateTime blobCreationTime)
        {
            string id = Guid.NewGuid().ToString("N");
            return string.Format("blob{0}{2}{1}{3}", ContainerDelimiter, KeyDelimiter,
              GetDateStringForContainerName(blobCreationTime),
              id);
        }

        /// <summary>
        /// Build a storage key for the message.
        /// </summary>
        /// <param name="instanceId">The orchestration instance Id</param>
        /// <param name="executionId">The orchestration execution Id</param>
        /// <param name="messageFireTime">The message fire time. If it is DateTime.MinValue, use current date.</param>
        /// <returns>The constructed storage key for message</returns>
        public static string BuildMessageStorageKey(string instanceId, string executionId, DateTime messageFireTime)
        {
            string id = Guid.NewGuid().ToString("N");
            return string.Format("message{0}{2}{1}{3}{4}{5}{4}{6}", ContainerDelimiter, KeyDelimiter,
              GetDateStringForContainerName(messageFireTime),
              instanceId,
              BlobNameDelimiter,
              executionId,
              id);
        }

        /// <summary>
        /// Build a storage key for the session.
        /// </summary>
        /// <param name="sessionId">The session Id</param>
        /// <returns>The constructed storage key for session</returns>
        public static string BuildSessionStorageKey(string sessionId)
        {
            string id = Guid.NewGuid().ToString("N");
            return string.Format("session{0}{2}{1}{3}{4}{5}", ContainerDelimiter, KeyDelimiter,
              GetDateStringForContainerName(DateTime.MinValue), sessionId, BlobNameDelimiter, id);
        }

        // use the message fire time if it is set;
        // otherwise, use the current utc time as the date string as part of the container name
        static string GetDateStringForContainerName(DateTime messageFireTime)
        {
            return messageFireTime != DateTime.MinValue ?
                messageFireTime.ToString(DateFormat) :
                DateTime.UtcNow.ToString(DateFormat);
        }

        /// <summary>
        /// Parse the key for the contianer name suffix and the blob name.
        /// </summary>
        /// <param name="key">The input storage key</param>
        /// <param name="containerNameSuffix">The parsed container name suffix as output</param>
        /// <param name="blobName">The parsed blob name as output</param>
        public static void ParseKey(string key, out string containerNameSuffix, out string blobName)
        {
            string[] segments = key.Split(BlobStorageClientHelper.KeyDelimiter);
            if (segments.Length < 2)
            {
                throw new ArgumentException("storage key does not contain required 2 or more segments: {containerNameSuffix}|{blobName}.", nameof(key));
            }
            containerNameSuffix = segments[0];
            blobName = key.Substring(containerNameSuffix.Length + 1, key.Length - containerNameSuffix.Length - 1);
        }

        /// <summary>
        /// Check if the container is expired.
        /// </summary>
        /// <param name="containerName">The container name</param>
        /// <param name="thresholdDateTimeUtc">The specified date threshold</param>
        /// <returns></returns>
        public static bool IsContainerExpired(string containerName, DateTime thresholdDateTimeUtc)
        {
            string[] segments = containerName.Split(ContainerDelimiter);
            if (segments.Length != 3)
            {
                throw new ArgumentException("container name does not contain required 3 segments.", nameof(containerName));
            }

            DateTime containerDateTime = DateTime.ParseExact(segments[2], DateFormat, System.Globalization.CultureInfo.InvariantCulture);
            return containerDateTime < thresholdDateTimeUtc;
        }

        /// <summary>
        /// Build a container name using prefix and suffix.
        /// </summary>
        /// <param name="prefix">The container name prefix</param>
        /// <param name="suffix">The container name suffix</param>
        /// <returns>The container name</returns>
        public static string BuildContainerName(string prefix, string suffix)
        {
            return $"{prefix}{ContainerDelimiter}{suffix}";
        }
    }
}
