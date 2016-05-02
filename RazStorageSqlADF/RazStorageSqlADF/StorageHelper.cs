using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

namespace RDU.ADF
{
    public static class StorageHelper
    {
        public static string GetStorageConnectionString(AzureStorageLinkedService asset)
        {

            if (asset == null)
            {
                return null;
            }

            return asset.ConnectionString;
        }

        public static string GetStorageFolderPath(Dataset dataArtifact)
        {
            if (dataArtifact == null || dataArtifact.Properties == null)
            {
                return null;
            }

            AzureBlobDataset blobDataset = dataArtifact.Properties.TypeProperties as AzureBlobDataset;
            if (blobDataset == null)
            {
                return null;
            }

            return blobDataset.FolderPath;
        }

        public static void GetBlobFileInfo(string blobName, ref string folderName, ref string fileName)
        {
            var tmp = blobName.Split('/');

            FileInfo fileInfo = null;
            if (tmp.Length == 1)
                fileInfo = new FileInfo(tmp[0]);
            else if (tmp.Length == 2)
            {
                folderName = tmp[0];
                fileInfo = new FileInfo(tmp[1]);
            }

            if (fileInfo != null)
                fileName = fileInfo.Name;
        }
    }
}
