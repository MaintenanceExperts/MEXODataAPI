using MEXModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MEXODataAPI.Examples
{
    class AssetReadingImport
    {
        const string MEXURL = "http://trial.mex.com.au/MEXTrial"; // change this URL to your own MEX server, or you can use the trial for testing

        private static DataToken DataToken;

        public static void AssetReadingImportExample(DataToken dataToken)
        {
            DataToken = dataToken;

            // Use first or default if you want the first result but there could be 0+ results. Use Single Or Default if there should only be 1 or 0 results
            Asset asset = DataToken.Assets.Where(a => a.AssetNumber == "ADMIN").FirstOrDefault();

            if (asset == null) {
                // Put code here for if the asset doesn't exist in MEX
                return;
            }

            // Get the frequency type. If you want you can find the ID before hand and use the ID instead of the frequency type name
            FrequencyType frequencyType = DataToken.FrequencyTypes.Where(ft => ft.FrequencyTypeName == "Hours").SingleOrDefault();

            if (frequencyType == null) {
                //Put code here for if the frequency type doesn't exist.
                return;
            }

            AssetReading assetReading = DataToken.AssetReadings.Where(ar => ar.AssetID == asset.AssetID && ar.FrequencyTypeID == frequencyType.FrequencyTypeID).FirstOrDefault();

            if (assetReading == null) {
                // Create a new Asset Reading. When I create things with the API I use ContactID -1 so it is obvious.
                assetReading = AssetReading.CreateAssetReading(0, asset.AssetID, frequencyType.FrequencyTypeID, 0, 0, "0", true, -1, DateTime.Now, -1, DateTime.Now);
                // Because this is a new record, it has to be added to the dataservice so it knows to track it.
                DataToken.AddToAssetReadings(assetReading);

                // Commit it to the database so it can be used
                DataToken.SaveChanges();
            }

            // Change between adding the line directly in the database, or going through a service operation
            bool processRequest = true;

            if (processRequest) {
                //********************* Code for going through the Service Op **************************//


                // The reading you are pushing to MEX
                double newReading = 123;
                // Need to know if you are going to update all the components of the asset as well. 
                bool updateComponents = false;
                // If you want mex to validate hours set this to false.
                bool disregardValidateHours = true;
                // If a reading exists for that datetime, should it be overridden
                bool overrideOldReading = true;
                // If this is true it will prompt you before making the changes if an identical record exists.
                // I'm not 100% sure on how this works, so I would suggest leaving this as false.
                bool checkForIdentical = false;

                // Construct the request. The first parameter of 'AddQueryOption' is the parameter name in MEX, and the second is the value to set it to. 
                var request = DataToken.CreateQuery<ODataFunctionImportQueryableData>("ProcessReading")
                                        .AddQueryOption("pFrequencyTypeID", frequencyType.FrequencyTypeID)
                                        .AddQueryOption("pNewReading", $"'{newReading}'")
                                        .AddQueryOption("pCompletedDateTime", $"'{DateTime.Now}'")
                                        .AddQueryOption("pAssetID", asset.AssetID)
                                        .AddQueryOption("pIsUpdateComponents", $"'{updateComponents}'")
                                        .AddQueryOption("pCreatedContactID", -1)
                                        .AddQueryOption("pIsDisregardHoursValidation", $"'{disregardValidateHours}'")
                                        .AddQueryOption("pIsOverwriteOldReading", $"'{overrideOldReading}'")
                                        .AddQueryOption("pCheckIdentical", $"'{checkForIdentical}'")
                                        .AddQueryOption("pLanguageNameID", 1); // This will probably always be 1. But you can find out by querying the DB to find the languageID for english

                // Execute the request
                var result = request.Execute().FirstOrDefault();
                // Log the result
                Console.WriteLine(result);

                // Pause the app so you can see the result
                Console.ReadKey();
            } else {
                //********************* Code for going directly Inserting Data **************************//

                // The current reading on the meter
                decimal enteredReading = 50;

                // The total reading that this asset. 
                // EG if a meter was reset to 0 after 150 hours, and was back up to 50 hours, total reading would be 200
                decimal totalReading = 200;

                // If you have replaced a meter and this is resetting the reading, set this to true
                bool isReset = false;

                AssetReadingLine arl = AssetReadingLine.CreateAssetReadingLine(0, assetReading.AssetReadingID, DateTime.Now, enteredReading, totalReading, isReset, -1, DateTime.Now, -1, DateTime.Now);

                DataToken.AddToAssetReadingLines(arl);
                DataToken.SaveChanges();
            }

        }
    }
}
