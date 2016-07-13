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

    class BlobStorageClientHelper
    {
        static readonly string DateFormat = "yyyyMMdd";
        static readonly char ContainerDelimiter = '-';

        // the blob storage accesss key is in the format of {DateTime}|{bolbName}
        public static readonly char KeyDelimiter = '|';

        // the delimiter shown in the blob name as the file path
        public static readonly char BlobNameDelimiter = '/';

        public static string BuildStorageKey(DateTime blobCreationTime)
        {
            string id = Guid.NewGuid().ToString("N");
            return string.Format("blob{0}{2}{1}{3}", ContainerDelimiter, KeyDelimiter,
              GetDateStringForContainerName(blobCreationTime),
              id);
        }

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

        public static string BuildContainerName(string prefix, string suffix)
        {
            return $"{prefix}{ContainerDelimiter}{suffix}";
        }
    }
}
