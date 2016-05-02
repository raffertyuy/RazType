using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace RDU.ADF.RazStorageSQLADF.Tests
{
    [TestClass]
    public class StorageHelperTests
    {
        [TestMethod]
        public void GetBlobFileInfoTest()
        {
            string folderName = null;
            string fileName = null;

            StorageHelper.GetBlobFileInfo("project1/file001.json", ref folderName, ref fileName);

            Assert.AreEqual("project1", folderName);
            Assert.AreEqual("file001.json", fileName);
        }
    }
}
