using eBay.Service.Call;
using eBay.Service.Core.Sdk;
using eBay.Service.Core.Soap;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace getitemtransactions
{
    public class ebayAPIs
    {
        // https://ebaydts.com/eBayKBDetails?KBid=1937
        //
        // also look at GetOrderTransactions()
        public static TransactionTypeCollection GetItemTransactions(string itemId, DateTime ModTimeFrom, DateTime ModTimeTo)
        {
            ApiContext oContext = new ApiContext();

            string appID = ConfigurationManager.AppSettings["AppID"];
            string devID = ConfigurationManager.AppSettings["DevID"];
            string certID = ConfigurationManager.AppSettings["CertID"];
            string userToken = ConfigurationManager.AppSettings["Token"];

            //set the dev,app,cert information
            oContext.ApiCredential.ApiAccount.Developer = devID;
            oContext.ApiCredential.ApiAccount.Application = appID;
            oContext.ApiCredential.ApiAccount.Certificate = certID;
            oContext.ApiCredential.eBayToken = userToken;

            //set the endpoint (sandbox) use https://api.ebay.com/wsapi for production
            oContext.SoapApiServerUrl = "https://api.ebay.com/wsapi";

            //set the Site of the Context
            oContext.Site = eBay.Service.Core.Soap.SiteCodeType.US;

            //the WSDL Version used for this SDK build
            oContext.Version = "817";

            //very important, let's setup the logging
            ApiLogManager oLogManager = new ApiLogManager();
            oLogManager.ApiLoggerList.Add(new eBay.Service.Util.FileLogger("GetItemTransactions.log", false, false, true));
            oLogManager.EnableLogging = true;
            oContext.ApiLogManager = oLogManager;

            GetItemTransactionsCall oGetItemTransactionsCall = new GetItemTransactionsCall(oContext);

            //' set the Version used in the call
            oGetItemTransactionsCall.Version = oContext.Version;

            //' set the Site of the call
            oGetItemTransactionsCall.Site = oContext.Site;

            //' enable the compression feature
            oGetItemTransactionsCall.EnableCompression = true;

            DateTime CreateTimeFromPrev;

            //ModTimeTo set to the current time
            //ModTimeTo = DateTime.Now.ToUniversalTime();

            //ts1 is 15 mins
            //TimeSpan ts1 = new TimeSpan(9000000000);
            //CreateTimeFromPrev = ModTimeTo.AddDays(-30);

            //Set the ModTimeFrom the last time you made the call minus 2 minutes
            //ModTimeFrom = CreateTimeFromPrev;

            //set ItemID and <DetailLevel>ReturnAll<DetailLevel>
            oGetItemTransactionsCall.ItemID = itemId;
            oGetItemTransactionsCall.DetailLevelList.Add(DetailLevelCodeType.ReturnAll);

            var r = oGetItemTransactionsCall.GetItemTransactions(itemId, ModTimeFrom, ModTimeTo);

            return r;
        }

    }
}
