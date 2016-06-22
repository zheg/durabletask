namespace FrameworkUnitTests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using DurableTask.Tracking;

    [TestClass]
    public class BlobStorageClientHelperTest
    {
        [TestMethod]
        public async Task TestBlobStorageClientHelper()
        {
            Assert.AreEqual("ab-cd", BlobStorageClientHelper.BuildContainerName("ab", "cd"));

            Assert.IsTrue(BlobStorageClientHelper.IsContainerExpired("hubName-20100101", DateTime.UtcNow));
            Assert.IsFalse(BlobStorageClientHelper.IsContainerExpired("hubName-20990101", DateTime.UtcNow));

            DateTime dateTime = new DateTime(2015, 05, 17);
            Assert.IsTrue(BlobStorageClientHelper.IsContainerExpired("hubName-20150516", dateTime));
            Assert.IsFalse(BlobStorageClientHelper.IsContainerExpired("hubName-20150517", dateTime));
            Assert.IsFalse(BlobStorageClientHelper.IsContainerExpired("hubName-20150518", dateTime));
            Assert.IsTrue(BlobStorageClientHelper.IsContainerExpired("hubName-20140518", dateTime));

            try
            {
                BlobStorageClientHelper.IsContainerExpired("invalidContainerName", DateTime.UtcNow);
                Assert.Fail("ArgumentException must be thrown");
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.Contains("containerName"), "Exception must contain containerName.");
            }
        }
    }
}
