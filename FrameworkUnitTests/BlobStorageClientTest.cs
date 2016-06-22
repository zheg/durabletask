namespace FrameworkUnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DurableTask.Tracking;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;
    using Microsoft.WindowsAzure.Storage.Blob;

    [TestClass]
    public class BlobStorageClientTest
    {
        private BlobStorageClient blobStorageClient;

        [TestInitialize]
        public void TestInitialize()
        {
            var r = new Random();
            blobStorageClient = new BlobStorageClient("test00" + r.Next(0, 10000),
                "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1:10000/");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            List<CloudBlobContainer> containers = blobStorageClient.ListContainers().ToList();
            containers.ForEach(container => container.DeleteIfExists());
            containers = blobStorageClient.ListContainers().ToList();
            Assert.AreEqual(0, containers.Count);
        }

        [TestMethod]
        public async Task TestStreamBlobCreationAndDeletion()
        {
            string testContent = "test stream content";
            string key = "20101003|testBlobName";
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            await blobStorageClient.UploadStreamBlob(key, stream);

            MemoryStream result = await blobStorageClient.DownloadStreamAsync(key) as MemoryStream;
            string resultString = Encoding.UTF8.GetString(result.ToArray());
            Assert.AreEqual(resultString, testContent);
        }

        [TestMethod]
        public async Task TestDeleteContainers()
        {
            string testContent = "test stream content";
            string key1 = "20150516|a";
            string key2 = "20150517|b";
            string key3 = "20150518|c";
            string blobName = "testBlob";

            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            await blobStorageClient.UploadStreamBlob(key1, stream);
            await blobStorageClient.UploadStreamBlob(key2, stream);
            await blobStorageClient.UploadStreamBlob(key3, stream);

            DateTime dateTime = new DateTime(2015, 05, 17);
            await blobStorageClient.DeleteExpiredContainersAsync(dateTime);

            List<CloudBlobContainer> containers = blobStorageClient.ListContainers().ToList();
            Assert.AreEqual(2, containers.Count);
            List<string> sortedList = new List<string> {containers[0].Name, containers[1].Name};
            sortedList.Sort();

            Assert.IsTrue(sortedList[0].EndsWith("20150517"));
            Assert.IsTrue(sortedList[1].EndsWith("20150518"));

            await blobStorageClient.DeleteAllContainersAsync();
            containers = blobStorageClient.ListContainers().ToList();
            Assert.AreEqual(0, containers.Count);
        }
    }
}
