using MEXModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MEXODataAPI.Examples;

namespace MEXODataAPI
{
    class Program
    {
        const string MEXURL = "https://trial.mex.com.au/MEXAPI/OData.svc"; // change this URL to your own MEX server, or you can use the trial for testing

        private static DataToken DataToken { get; set; }

        #region MAIN
        static void Main(string[] args)
        {
            // included in this project is a service reference generated on a build 70 database
            // use this tool: https://marketplace.visualstudio.com/items?itemName=laylaliu.ODataConnectedService#overview if you would like to generate your own service reference

            // after generating a service reference, you can use the MEXEntities class to query, insert and update records in MEX
            DataToken = new DataToken(new Uri(MEXURL));

            // below are some examples of querying, modifying and inserting records into the database
            BasicExamples.CloseWorkOrder(DataToken);
            BasicExamples.UpdateContact(DataToken);
            BasicExamples.ProcessReading(DataToken);
            BasicExamples.GetSupplierListing(DataToken);
            BasicExamples.CreateRequest(DataToken);

            // Asset Reading Example
            AssetReadingImport.AssetReadingImportExample(DataToken);
        }
        #endregion


    }
}
