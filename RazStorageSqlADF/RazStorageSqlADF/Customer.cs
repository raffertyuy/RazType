using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDU.ADF
{
    public class Customer
    {
        [JsonProperty("Identifier")]
        public string Identifier { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Surname")]
        public string Surname { get; set; }

        [JsonProperty("TimeStamp")]
        public string TimeStamp { get; set; }

        public static Customer[] FromJson(string jsonText)
        {
            return JsonConvert.DeserializeObject<Customer[]>(jsonText);
        }

        public static string GenerateIdentifier(string folderName, string fileName, int fileNamePrefixLength = 0)
        {
            if (fileName.Contains('.'))
                fileName = fileName.Substring(0, fileName.IndexOf('.'));

            return string.Concat(
                            folderName, "_",
                            fileName.Length > fileNamePrefixLength
                                ? fileName.Substring(fileNamePrefixLength)
                                : fileName);
        }
    }
}
