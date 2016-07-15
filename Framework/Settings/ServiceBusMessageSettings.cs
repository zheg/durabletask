﻿//  ----------------------------------------------------------------------------------
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

namespace DurableTask.Settings
{
    /// <summary>
    ///     Settings to configure the Service Bus message.
    /// </summary>
    public class ServiceBusMessageSettings
    {
        internal ServiceBusMessageSettings()
        {
            MaxMessageSizeInBytes = 170 * 1024;
            MaxMessageSizeForBlobInBytes = 10 * 1024 * 1024;
        }
        internal ServiceBusMessageSettings(int maxMessageSizeInBytes, int maxMessageSizeForBlobInBytes)
        {
            MaxMessageSizeInBytes = maxMessageSizeInBytes;
            MaxMessageSizeForBlobInBytes = maxMessageSizeForBlobInBytes;
        }

        /// <summary>
        ///     The max allowed message size after compression in service bus. Default is 170K.
        /// </summary>
        public int MaxMessageSizeInBytes { get; set; }

        /// <summary>
        ///    The max allowed message size for external storage. Default is 10M.
        /// </summary>
        public int MaxMessageSizeForBlobInBytes { get; set; }
    }
}
