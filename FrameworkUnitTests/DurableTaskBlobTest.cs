using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DurableTask.Blob;
using DurableTask.History;
using DurableTask.Tracking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FrameworkUnitTests
{
    [TestClass]
    public class DurableTaskBlobTest
    {
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
        public async Task DurableTaskBlobSaveLoadTest()
        {
            // string type
            string stringInput = "abc";
            DurableTaskBlob<string> duralbeTaskBlob = await DurableTaskBlobHelper<string>.SaveDurableTaskBlob(azureTableInstanceStore as IBlobStore, stringInput, DateTime.MinValue);
            string stringOutput =
                await DurableTaskBlobHelper<string>.LoadDurableTaskBlob(azureTableInstanceStore as IBlobStore,
                    duralbeTaskBlob);
            Assert.AreEqual(stringInput, stringOutput);

            // stream type
            MemoryStream streamInput = new MemoryStream(Encoding.UTF8.GetBytes(stringInput));
            DurableTaskBlob<MemoryStream> duralbeTaskBlobStream = await DurableTaskBlobHelper<MemoryStream>.SaveDurableTaskBlob(azureTableInstanceStore as IBlobStore, streamInput, DateTime.MinValue);
            Stream streamOutput =
                await DurableTaskBlobHelper<MemoryStream>.LoadDurableTaskBlob(azureTableInstanceStore as IBlobStore,
                    duralbeTaskBlobStream);
            StreamReader reader = new StreamReader(streamOutput);
            Assert.AreEqual(stringInput, reader.ReadToEnd());

            // object type
            int eventId = 10;
            string eventInput = "eventInput";
            ExecutionStartedEvent executionStartedEventInput = new ExecutionStartedEvent(eventId, eventInput);
            Dictionary<string, string> tags = new Dictionary<string, string>();
            tags.Add("key1", "value1");
            tags.Add("key2", "value2");
            executionStartedEventInput.Tags = tags;

            DurableTaskBlob<ExecutionStartedEvent> duralbeTaskBlobObject = await DurableTaskBlobHelper<ExecutionStartedEvent>.SaveDurableTaskBlob(azureTableInstanceStore as IBlobStore, executionStartedEventInput, DateTime.MinValue);
            ExecutionStartedEvent executionStartedEventOutput =
                await DurableTaskBlobHelper<ExecutionStartedEvent>.LoadDurableTaskBlob(azureTableInstanceStore as IBlobStore,
                    duralbeTaskBlobObject);
            Assert.AreEqual(eventId, executionStartedEventOutput.EventId);
            Assert.AreEqual(eventInput, executionStartedEventOutput.Input);
            Assert.AreEqual(EventType.ExecutionStarted, executionStartedEventOutput.EventType);

            string value1, value2;
            executionStartedEventOutput.Tags.TryGetValue("key1", out value1);
            executionStartedEventOutput.Tags.TryGetValue("key2", out value2);
            Assert.AreEqual("value1", value1);
            Assert.AreEqual("value2", value2);
        }
    }
}
