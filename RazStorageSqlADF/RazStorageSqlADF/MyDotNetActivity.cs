using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Data.SqlClient;

using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.Azure.Management.DataFactories.Runtime;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using Newtonsoft.Json;

namespace RDU.ADF
{
    public class MyDotNetActivity : IDotNetActivity
    {
        public IDictionary<string, string> Execute(IEnumerable<LinkedService> linkedServices, IEnumerable<Dataset> datasets, Activity activity, IActivityLogger logger)
        {
            IDictionary<string, string> extendedProperties = ((DotNetActivity)activity.TypeProperties).ExtendedProperties;

            logger.Write("Initializing Input Files DataSet...");
            var inputFilesDataSet = datasets.First(ds => ds.Name == Properties.Settings.Default.InputFilesDataSetName);
            var inputFilesLinkedService = linkedServices.First(linkedService => linkedService.Name == inputFilesDataSet.Properties.LinkedServiceName).Properties.TypeProperties as AzureStorageLinkedService;

            logger.Write("Initializing Archive DataSet...");
            var archiveDataSet = datasets.First(ds => ds.Name == Properties.Settings.Default.ArchiveDataSetName);
            var archiveLinkedService = linkedServices.First(linkedService => linkedService.Name == archiveDataSet.Properties.LinkedServiceName).Properties.TypeProperties as AzureStorageLinkedService;

            logger.Write("Initializing Customers DataSet...");
            var customersDataSet = datasets.First(ds => ds.Name == Properties.Settings.Default.CustomersDataSetName);
            var customersLinkedService = linkedServices.First(linkedService => linkedService.Name == customersDataSet.Properties.LinkedServiceName).Properties.TypeProperties as AzureSqlDatabaseLinkedService;

            logger.Write("Processing...");
            var inputFileStorageConnectionString = StorageHelper.GetStorageConnectionString(inputFilesLinkedService);
            var inputFilesStorageFolderPath = StorageHelper.GetStorageFolderPath(inputFilesDataSet);
            var archiveStorageConnectionString = StorageHelper.GetStorageConnectionString(archiveLinkedService);
            var archiveStorageFolderPath = StorageHelper.GetStorageFolderPath(archiveDataSet);
            var customersSqlConnectionString = customersLinkedService.ConnectionString;

            logger.Write("...inputFileStorageConnectionString = {0}", inputFileStorageConnectionString);
            logger.Write("...inputFilesStorageFolderPath = {0}", inputFilesStorageFolderPath);
            logger.Write("...archiveStorageConnectionString = {0}", archiveStorageConnectionString);
            logger.Write("...archiveStorageFolderPath = {0}", archiveStorageFolderPath);
            logger.Write("...customersSqlConnectionString = {0}", customersSqlConnectionString);

            Process(inputFileStorageConnectionString, inputFilesStorageFolderPath,
                archiveStorageConnectionString, archiveStorageFolderPath,
                customersSqlConnectionString,
                logger);

            logger.Write("Custom Activity Completed.");

            return new Dictionary<string, string>();
        }

        public void Process(
            string inputFileStorageConnectionString, string inputFilesStorageFolderPath,
            string archiveStorageConnectionString, string archiveStorageFolderPath,
            string customersSqlConnectionString,
            IActivityLogger logger = null
            )
        {
            var inputFilesStorageAccount = CloudStorageAccount.Parse(inputFileStorageConnectionString);
            var inputFilesClient = inputFilesStorageAccount.CreateCloudBlobClient();
            var inputFilesBlobContainer = inputFilesClient.GetContainerReference(inputFilesStorageFolderPath);
            var inputFilesBlobs = inputFilesBlobContainer.ListBlobs(useFlatBlobListing: true).OfType<CloudBlockBlob>().Cast<CloudBlockBlob>();

            var archiveStorageAccount = CloudStorageAccount.Parse(archiveStorageConnectionString);
            var archiveClient = archiveStorageAccount.CreateCloudBlobClient();
            var archiveBlobContainer = archiveClient.GetContainerReference(archiveStorageFolderPath);

            Process(inputFilesBlobs, archiveBlobContainer, customersSqlConnectionString, logger);
        }

        private void Process(
            IEnumerable<CloudBlockBlob> inputFilesBlobs, CloudBlobContainer archiveBlobContainer,
            string customersSqlConnectionString, IActivityLogger logger = null)
        {
            using (var sqlconn = new SqlConnection(customersSqlConnectionString))
            {
                sqlconn.Open();

                if (logger != null)
                    logger.Write("Traversing through the inputFilesBlobs...");

                foreach (var blob in inputFilesBlobs)
                {
                    if (logger != null)
                        logger.Write("Blob Name: {0}", blob.Name);

                    var folderName = string.Empty;
                    var fileName = string.Empty;
                    StorageHelper.GetBlobFileInfo(blob.Name, ref folderName, ref fileName);

                    if (logger != null)
                        logger.Write("(Folder, FileName) : {0}, {1}", folderName, fileName);

                    var json = blob.DownloadText();
                    var customers = Customer.FromJson(json);

                    foreach (var customer in customers)
                    {
                        customer.Identifier = Customer.GenerateIdentifier(folderName, fileName, Properties.Settings.Default.FileNamePrefix.Length);

                        var sqlcmd = new SqlCommand() { Connection = sqlconn };
                        sqlcmd.CommandText = string.Format(
                            "INSERT INTO Customers(Identifier, Name, Surname, TimeStamp) VALUES('{0}','{1}','{2}','{3}')",
                            customer.Identifier, customer.Name, customer.Surname, customer.TimeStamp);

                        if (logger != null)
                            logger.Write(sqlcmd.CommandText);

                        sqlcmd.ExecuteNonQuery();
                    }

                    if (logger != null)
                        logger.Write("Archiving...");

                    var archiveBlob = archiveBlobContainer.GetBlockBlobReference(blob.Name);
                    archiveBlob.UploadText(json);

                    if (logger != null)
                        logger.Write("Removing Blob from Source...");

                    blob.Delete();
                }
            }
        }
    }
}
