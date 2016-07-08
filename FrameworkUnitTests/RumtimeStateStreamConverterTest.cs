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

namespace FrameworkUnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using DurableTask;
    using DurableTask.History;
    using DurableTask.Serializing;
    using DurableTask.Tracking;

    [TestClass]
    public class RumtimeStateStreamConverterTest
    {
        const long SessionStreamTerminationThresholdInBytes = 10 * 1024;
        const long SessionStreamExternalStorageThresholdInBytes = 2 * 1024;
        const string sessionId = "session123";

        AzureTableInstanceStore azureTableInstanceStore;
        [TestInitialize]
        public void TestInitialize()
        {
            azureTableInstanceStore = TestHelpers.CreateAzureTableInstanceStore();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            azureTableInstanceStore.DeleteStoreAsync().Wait();
        }

        [TestMethod]
        public async Task SmallRuntimeStateConverterTest()
        {       
            string smallInput = "abc";
            
            OrchestrationRuntimeState newOrchestrationRuntimeStateSmall = generateOrchestrationRuntimeState(smallInput);
            OrchestrationRuntimeState runtimeState = new OrchestrationRuntimeState();
            DataConverter dataConverter = new JsonDataConverter();

            // a small runtime state doesn't need external storage.
            Stream rawStreamSmall = await RuntimeStateStreamConverter.OrchestrationRuntimeStateToRawStream(newOrchestrationRuntimeStateSmall,
                runtimeState, dataConverter, true, SessionStreamTerminationThresholdInBytes,
                SessionStreamExternalStorageThresholdInBytes, azureTableInstanceStore as IBlobStore, sessionId);
            OrchestrationRuntimeState convertedRuntimeStateSmall = await RuntimeStateStreamConverter.RawStreamToRuntimeState(rawStreamSmall, "sessionId", azureTableInstanceStore as IBlobStore, dataConverter);
            verifyEventInput(smallInput, convertedRuntimeStateSmall);

            // test for un-compress case
            Stream rawStreamSmall2 = await RuntimeStateStreamConverter.OrchestrationRuntimeStateToRawStream(newOrchestrationRuntimeStateSmall,
                runtimeState, dataConverter, false, SessionStreamTerminationThresholdInBytes,
                SessionStreamExternalStorageThresholdInBytes, azureTableInstanceStore as IBlobStore, sessionId);
            OrchestrationRuntimeState convertedRuntimeStateSmall2 = await RuntimeStateStreamConverter.RawStreamToRuntimeState(rawStreamSmall2, "sessionId", azureTableInstanceStore as IBlobStore, dataConverter);
            verifyEventInput(smallInput, convertedRuntimeStateSmall2);

            // test for backward comp: ok for an un-implemented (or null) IBlobStorage for small runtime states
            Stream rawStreamSmall3 = await RuntimeStateStreamConverter.OrchestrationRuntimeStateToRawStream(newOrchestrationRuntimeStateSmall,
                runtimeState, dataConverter, true, SessionStreamTerminationThresholdInBytes,
                SessionStreamExternalStorageThresholdInBytes, null, sessionId);
            OrchestrationRuntimeState convertedRuntimeStateSmall3 = await RuntimeStateStreamConverter.RawStreamToRuntimeState(rawStreamSmall3, "sessionId", null, dataConverter);
            verifyEventInput(smallInput, convertedRuntimeStateSmall3);
        }

        [TestMethod]
        public async Task LargeRuntimeStateConverterTest()
        {
            string largeInput = TestUtils.GenerateRandomString(5 * 1024);
            OrchestrationRuntimeState newOrchestrationRuntimeStateLarge = generateOrchestrationRuntimeState(largeInput);
            OrchestrationRuntimeState runtimeState = new OrchestrationRuntimeState();
            DataConverter dataConverter = new JsonDataConverter();

            // a large runtime state that needs external storage.
            Stream rawStreamLarge = await RuntimeStateStreamConverter.OrchestrationRuntimeStateToRawStream(newOrchestrationRuntimeStateLarge,
                runtimeState, dataConverter, true, SessionStreamTerminationThresholdInBytes,
                SessionStreamExternalStorageThresholdInBytes, azureTableInstanceStore as IBlobStore, sessionId);
            OrchestrationRuntimeState convertedRuntimeStateLarge = await RuntimeStateStreamConverter.RawStreamToRuntimeState(rawStreamLarge, "sessionId", azureTableInstanceStore as IBlobStore, dataConverter);
            verifyEventInput(largeInput, convertedRuntimeStateLarge);

            // test for un-compress case
            string largeInput2 = TestUtils.GenerateRandomString(3 * 1024);
            OrchestrationRuntimeState newOrchestrationRuntimeStateLarge2 = generateOrchestrationRuntimeState(largeInput2);
            Stream rawStreamLarge2 = await RuntimeStateStreamConverter.OrchestrationRuntimeStateToRawStream(newOrchestrationRuntimeStateLarge2,
                runtimeState, dataConverter, false, SessionStreamTerminationThresholdInBytes,
                SessionStreamExternalStorageThresholdInBytes, azureTableInstanceStore as IBlobStore, sessionId);
            OrchestrationRuntimeState convertedRuntimeStateLarge2 = await RuntimeStateStreamConverter.RawStreamToRuntimeState(rawStreamLarge2, "sessionId", azureTableInstanceStore as IBlobStore, dataConverter);
            verifyEventInput(largeInput2, convertedRuntimeStateLarge2);

            // test for an un-implemented (or null) IBlobStorage for large runtime states
            try
            {
                await
                    RuntimeStateStreamConverter.OrchestrationRuntimeStateToRawStream(newOrchestrationRuntimeStateLarge,
                        runtimeState, dataConverter, true, SessionStreamTerminationThresholdInBytes,
                        SessionStreamExternalStorageThresholdInBytes, null, sessionId);
                Assert.Fail("ArgumentException must be thrown");
            }
            catch (ArgumentException e)
            {
                // expected
                Assert.IsTrue(e.Message.Contains("IBlobStore"), "Exception must contain IBlobStore.");
            }
        }

        [TestMethod]
        public async Task VeryLargeRuntimeStateConverterTest()
        {
            string veryLargeInput = TestUtils.GenerateRandomString(20 * 1024);
            OrchestrationRuntimeState newOrchestrationRuntimeStateLarge = generateOrchestrationRuntimeState(veryLargeInput);
            OrchestrationRuntimeState runtimeState = new OrchestrationRuntimeState();
            DataConverter dataConverter = new JsonDataConverter();

            // test for very large size rumtime state: should throw exception
            try
            {
                Stream rawStreamVeryLarge = await RuntimeStateStreamConverter.OrchestrationRuntimeStateToRawStream(newOrchestrationRuntimeStateLarge,
                    runtimeState, dataConverter, true, SessionStreamTerminationThresholdInBytes,
                    SessionStreamExternalStorageThresholdInBytes, azureTableInstanceStore as IBlobStore, sessionId);
                Assert.Fail("ArgumentException must be thrown");
            }
            catch (ArgumentException e)
            {
                // expected
            }
        }

        OrchestrationRuntimeState generateOrchestrationRuntimeState(string input)
        {
            IList<HistoryEvent> historyEvents = new List<HistoryEvent>();
            ExecutionStartedEvent historyEvent = new ExecutionStartedEvent(1, input);
            historyEvents.Add(historyEvent);
            OrchestrationRuntimeState newOrchestrationRuntimeState = new OrchestrationRuntimeState(historyEvents);

            return newOrchestrationRuntimeState;
        }

        void verifyEventInput(string expectedHistoryEventInput, OrchestrationRuntimeState runtimeState)
        {
            ExecutionStartedEvent executionStartedEvent = runtimeState.Events[0] as ExecutionStartedEvent;
            Assert.AreEqual(expectedHistoryEventInput, executionStartedEvent.Input);
        }
    }
}