using MEXModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEXODataAPI.Examples
{
    class BasicExamples
    {
        private static DataToken DataToken;
        
        public static void CloseWorkOrder(DataToken dataToken)
        {
            DataToken = dataToken;
            const string exampleAssetNumber = "ADMIN";

            WorkOrder newWorkOrder = CreateWorkOrderWithAssetAndWorkOrderDescription(exampleAssetNumber, "Fix cuboard");
            CloseWorkOrderWithWorkOrderNumber(newWorkOrder.WorkOrderNumber);
        }

        public static void UpdateContact(DataToken dataToken)
        {
            DataToken = dataToken;
            Contact newContact = CreateContactWithFirstAndLastName("Maitland", "Marshall");
            UpdateContactWithAddress(newContact.ContactID, "123 Fake Street");
        }

        public static void ProcessReading(DataToken dataToken)
        {
            DataToken = dataToken;
            const string exampleAssetNumber = "ADMIN";

            ProcessReadingForAsset(exampleAssetNumber);
        }

        public static List<vwContactsListing> GetSupplierListing(DataToken dataToken)
        {
            DataToken = dataToken;
            return GetSupplierListing();
        }

        public static void CreateRequest(DataToken dataToken)
        {
            DataToken = dataToken;
            CreateRequestWithDescription("Fix light in store room");
        }

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
            if (workOrder == null) {
                throw new Exception($"Work Order {workOrderNumber} does not exist");
            }

            return workOrder;
        }

        static Asset GetAssetWithAssetNumber(string assetNumber)
        {
            Asset asset = DataToken.Assets.Where(y => y.AssetNumber == assetNumber).FirstOrDefault();
            if (asset == null) {
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
