using MEXModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MEXODataAPI
{
    class Program
    {
        const string MEXURL = "http://trial.mex.com.au/MEXTrial"; // change this URL to your own MEX server, or you can use the trial for testing

        static DataToken DataToken { get; set; }

        #region MAIN
        static void Main(string[] args)
        {
            // If you want to test importing values and don't have your own testing site, use the Trial as it get reset every night.
            DataToken = new DataToken(MEXURL);

            // OldMainCode();
            AssetReadingExample();

            Console.WriteLine("Finished");
            Console.ReadKey();
        }
        #endregion

        private static void AssetReadingExample()
        {
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
            bool processRequest = false;

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

        #region Old Main Code

        private static void OldMainCode()
        {            
            // included in this project is a service reference generated on a build 70 database
            // use this tool: https://marketplace.visualstudio.com/items?itemName=laylaliu.ODataConnectedService#overview if you would like to generate your own service reference

            // After generating a service reference, you can use the DataToken class to query, insert and update records in MEX
            DataToken = new DataToken(MEXURL);

            // below are some examples of querying, modifying and inserting records into the database
            const string exampleAssetNumber = "ADMIN";

            try {
                WorkOrder newWorkOrder = CreateWorkOrderWithAssetAndWorkOrderDescription(exampleAssetNumber, "Fix cuboard");
                CloseWorkOrderWithWorkOrderNumber(newWorkOrder.WorkOrderNumber);

                Contact newContact = CreateContactWithFirstAndLastName("Maitland", "Marshall");
                UpdateContactWithAddress(newContact.ContactID, "123 Fake Street");

                ProcessReadingForAsset(exampleAssetNumber);

                GetSupplierListing();
            } finally {
                // refresh the data token to avoid cache & peformance issues
                DataToken = new DataToken(DataToken.BaseUri);
            }

            CreateRequestWithDescription("Fix light in store room");
        }

        #endregion  

        #region EXAMPLES

        #region CONTACTS
        static List<vwContactsListing> GetSupplierListing()
        {
            List<vwContactsListing> listing = DataToken.vwContactsListings.Where(y => y.ParentContactTypeName == "Supplier").ToList();
            return listing;
        }

        static Contact CreateContactWithFirstAndLastName(string firstName, string lastName)
        {
            Contact newContact = Contact.CreateContact(
                0, // contact ID
                firstName, // first name
                true, // is primary contact
                false, // is tax exempt
                0, // customer discount percentage
                0, // catalogue marked up ceiling
                0, // non catalogue marked up ceiling
                0, // catalogue marked up percentage
                0, // non catalogue marked up percentage
                0, // working day hours
                0, // expected freight price
                "Supplier", // contact type
                1, // created by
                DateTime.Now, // created date
                1, // modified by
                DateTime.Now, // modified date
                true, // is active
                DateTime.Now // added date time
                );

            newContact.LastName = lastName;

            DataToken.AddToContacts(newContact);
            DataToken.SaveChanges();

            return newContact;
        }

        static void UpdateContactWithAddress(int contactID, string newAddress)
        {
            Contact contact = DataToken.Contacts.Where(y => y.ContactID == contactID).FirstOrDefault();
            contact.Address1 = newAddress;

            DataToken.SaveChanges();
        }
        #endregion

        #region REQUESTS
        static void CreateRequestWithDescription(string requestDescription)
        {
            // these are the only valid approval statuses
            const string
                pendingApproval = "Pending Approval",
                approved = "Approved",
                declined = "Declined";

            // call GetRequestNumber to retrieve a valid unused request number
            ODataFunctionImportQueryableData requestNumber = DataToken.CreateQuery<ODataFunctionImportQueryableData>("GetRequestNumber").Execute().FirstOrDefault();

            Request newRequest = Request.CreateRequest(
                0, // request ID
                int.Parse(requestNumber.FieldValue), // request number
                false, // is cancelled
                false, // is completed
                0, // estimated cost
                1, // created by
                DateTime.Now, // created date
                1, // modified by
                DateTime.Now, // modified date
                false // is accepted by requester
                );

            newRequest.RequestedByContactID = 1;
            newRequest.RequestDescription = requestDescription;

            // AddToRequests so when SaveChanges is called it gets persisted
            DataToken.AddToRequests(newRequest);
            DataToken.SaveChanges();

            // Requests need a RecordApproval database entry to be valid
            RecordApproval newRequestRecordApproval = RecordApproval.CreateRecordApproval(
                0, // record approval ID
                nameof(Request), // entity Name
                newRequest.RequestID, // request ID
                pendingApproval, // approval status
                1, // approved by
                DateTime.Now, // approved date time
                1, // created by
                DateTime.Now, // created date
                1, // modified by
                DateTime.Now // modified date
                );

            DataToken.AddToRecordApprovals(newRequestRecordApproval);
            DataToken.SaveChanges();
        }
        #endregion

        #region WORK ORDERS
        static WorkOrder GetWorkOrderWithWorkOrderNumber(int workOrderNumber)
        {
            WorkOrder workOrder = DataToken.WorkOrders.Where(y => y.WorkOrderNumber == workOrderNumber).FirstOrDefault();
            if(workOrder == null)
            {
                throw new Exception($"Work Order {workOrderNumber} does not exist");
            }

            return workOrder;
        }

        static Asset GetAssetWithAssetNumber(string assetNumber)
        {
            Asset asset = DataToken.Assets.Where(y => y.AssetNumber == assetNumber).FirstOrDefault();
            if (asset == null)
            {
                throw new Exception($"Asset {assetNumber} does not exist");
            }

            return asset;
        }

        static WorkOrder CreateWorkOrderWithAssetAndWorkOrderDescription(string assetNumber, string workOrderDescription)
        {
            // call the GetWorkOrderDefaults service operation to retrieve a valid unused work order number and default status ID
            List<ODataFunctionImportQueryableData> workOrderDefaults = DataToken.CreateQuery<ODataFunctionImportQueryableData>("GetWorkOrderDefaults").Execute().ToList();
            string
                workOrderNumber = workOrderDefaults[0].FieldValue,
                workOrderStatusID = workOrderDefaults[1].FieldValue;

            Asset workOrderAsset = GetAssetWithAssetNumber(assetNumber);

            // create new entities by using the static Create[Entity] method on each class
            // this will ensure you supply parameters for every field which is not nullable

            WorkOrder newWorkOrder = WorkOrder.CreateWorkOrder(
                0, // work order id
                int.Parse(workOrderNumber), // work order number
                workOrderAsset.AssetID, // asset ID
                false, // is group work order
                DateTime.Now, // raised date time
                false, // is history created 
                0, // overall duration hours
                int.Parse(workOrderStatusID), // work order status ID
                0, // estimated labour cost 
                0, // estimated material cost
                0, // estimated other cost 
                0, // actual labour cost 
                0, // actual material cost 
                0, // actual other cost
                false, // is printed
                0, // progress indicator percentage
                0, // down time hours 
                0, // repair time hours
                false, // is quoted amount invoiced
                1, // created by
                DateTime.Now, // created date time
                1, // modified by
                DateTime.Now, // modified date time
                0, // contractor quote amount
                "Standard", // work order format
                false, // is completed by contractor 
                false, // is contractor work order 
                false, // is contractor invoice paid
                false // is audit
                );

            newWorkOrder.WorkOrderDescription = workOrderDescription;

            DataToken.AddToWorkOrders(newWorkOrder);
            DataToken.SaveChanges();

            Debug.WriteLine($"Work Order Number {newWorkOrder.WorkOrderNumber} was created with Work Order ID {newWorkOrder.WorkOrderID}");

            return newWorkOrder;
        }

        static void CloseWorkOrderWithWorkOrderNumber(int workOrderNumberToClose)
        {
            WorkOrder workOrderToClose = GetWorkOrderWithWorkOrderNumber(workOrderNumberToClose);

            ODataFunctionImportQueryableData result = DataToken.CreateQuery<ODataFunctionImportQueryableData>("CloseWorkOrdersToHistory")
                .AddQueryOption("pWorkOrderIDs", $"'({workOrderToClose.WorkOrderID})'")
                .AddQueryOption("pCompleteRequests", "'False'").Execute().FirstOrDefault();

            Debug.WriteLine(result.FieldValue);
        }

        #endregion

        #region READINGS
        static void ProcessReadingForAsset(string assetNumber)
        {
            bool 
                pIsDisregardHoursValidation = true,
                pIsOverwriteOldReading = true,
                pCheckIdentical = true,
                pIsUpdateComponents = false;

            DateTime pCompletedDateTime = DateTime.Now;

            int pFrequencyTypeID = DataToken.FrequencyTypes.Where(y => y.FrequencyTypeName == "Hours").FirstOrDefault().FrequencyTypeID,
                pAssetID = GetAssetWithAssetNumber(assetNumber).AssetID,
                pCreatedContactID = 1,
                pLanguageNameID = 1,
                pNewReading = 5;

            var request = DataToken.CreateQuery<ODataFunctionImportQueryableData>("ProcessReading")
                .AddQueryOption(nameof(pFrequencyTypeID), pFrequencyTypeID)
                .AddQueryOption(nameof(pNewReading), $"'{pNewReading}'")
                .AddQueryOption(nameof(pCompletedDateTime), $"'{pCompletedDateTime.ToString("yyyy-MM-dd hh:mm:ss")}'")
                .AddQueryOption(nameof(pAssetID), pAssetID)
                .AddQueryOption(nameof(pIsUpdateComponents), $"'{pIsUpdateComponents}'")
                .AddQueryOption(nameof(pCreatedContactID), pCreatedContactID)
                .AddQueryOption(nameof(pIsDisregardHoursValidation), $"'{pIsDisregardHoursValidation}'")
                .AddQueryOption(nameof(pIsOverwriteOldReading), $"'{pIsOverwriteOldReading}'")
                .AddQueryOption(nameof(pCheckIdentical), $"'{pCheckIdentical}'")
                .AddQueryOption(nameof(pLanguageNameID), pLanguageNameID);

            ODataFunctionImportQueryableData result = request.Execute().FirstOrDefault();
            Debug.WriteLine(result.FieldValue);
        }
        #endregion

        #endregion
    }
}
