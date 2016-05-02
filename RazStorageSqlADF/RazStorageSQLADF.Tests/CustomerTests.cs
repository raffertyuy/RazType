using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RDU.ADF.RazStorageSQLADF.Tests
{
    [TestClass]
    public class CustomerTests
    {
        private const string _jsonText = @"[
  {
    ""Name"":""Tom"",
    ""Surname"":""Yang"",
    ""TimeStamp"":""28-Oct-2015 09:48 PM""
  },
  {
    ""Name"":""Judy"",
    ""Surname"":""Leaw"",
    ""TimeStamp"":""28-Oct-2015 09:49 PM""
  },
  {
    ""Name"":""Henry"",
    ""Surname"":""Jo"",
    ""TimeStamp"":""28-Oct-2015 09:48 PM""
  }
]";

        [TestMethod]
        public void JsonToCustomerClassTest()
        {
            var customers = Customer.FromJson(_jsonText);

            Assert.AreEqual(3, customers.Length);

            Assert.AreEqual("Tom", customers[0].Name);
            Assert.AreEqual("Yang", customers[0].Surname);
            Assert.AreEqual("28-Oct-2015 09:48 PM", customers[0].TimeStamp);

            Assert.AreEqual("Judy", customers[1].Name);
            Assert.AreEqual("Leaw", customers[1].Surname);
            Assert.AreEqual("28-Oct-2015 09:49 PM", customers[1].TimeStamp);

            Assert.AreEqual("Henry", customers[2].Name);
            Assert.AreEqual("Jo", customers[2].Surname);
            Assert.AreEqual("28-Oct-2015 09:48 PM", customers[2].TimeStamp);
        }

        [TestMethod]
        public void GenerateIdentifierTest()
        {
            var identifier = Customer.GenerateIdentifier("project1", "file001.json", 4);
            Assert.AreEqual("project1_001", identifier);
        }
    }
}
