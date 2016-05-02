using RDU.ADF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAZ.ADF.RazStorageSqlADF.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var activity = new MyDotNetActivity();
            activity.Process(
                Properties.Settings.Default.AdfStorageConnectionString, Properties.Settings.Default.Container_InputFiles,
                Properties.Settings.Default.AdfStorageConnectionString, Properties.Settings.Default.Container_Archive,
                Properties.Settings.Default.CustomerSqlConnectionString);
        }
    }
}
