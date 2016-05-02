using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RDU.ADF.RazStorageSQLADF.Tests
{
    [TestClass]
    public class MyDotNetActivityTests
    {
        private const string CustomerSqlConnectionString = "Data Source=tcp:razsql.database.windows.net,1433;Initial Catalog=razsql-StorageJsonDFA;User ID=razadmin;Password=Pass123!;Encrypt=True;TrustServerCertificate=False;Application Name=\"Azure Data Factory Linked Service\"";
        private const string AdfStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=razstorageadfpoc;AccountKey=9jgk0bt32bHjCy5xdntIXBIqaBv7bLkZkbh2L/k76nb0Uaebg75/b7EBR8KnB2Q3PHM64hGs2yY8dnD+EaZx3A==";
        private const string Container_Archive = "archive";
        private const string Container_InputFiles = "inputfiles";


        [TestMethod]
        public void ProcessTest()
        {
            var activity = new MyDotNetActivity();
            activity.Process(
                AdfStorageConnectionString, Container_InputFiles,
                AdfStorageConnectionString, Container_Archive,
                CustomerSqlConnectionString);
        }
    }
}
