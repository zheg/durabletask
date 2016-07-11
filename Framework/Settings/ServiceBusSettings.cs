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

namespace DurableTask.Settings
{
    /// <summary>
    ///     Settings to configure the Service Bus.
    /// </summary>
    public class ServiceBusSettings
    {     
        internal ServiceBusSettings()
        {
            SessionStreamExternalStorageThresholdInBytes = 230 * 1024;
            SessionStreamTerminationThresholdInBytes = 10 * 1024 * 1024;
        }

        /// <summary>
        ///     The threshold for external storage of session in stream. Default is 230K.
        /// </summary>
        public int SessionStreamExternalStorageThresholdInBytes { get; set; }

        /// <summary>
        ///     The threshold of session for orchestration termination. Default is 10M.
        /// </summary>
        public int SessionStreamTerminationThresholdInBytes { get; set; }
    }
}
