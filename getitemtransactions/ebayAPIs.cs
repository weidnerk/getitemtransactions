using eBay.Service.Call;
using eBay.Service.Core.Sdk;
using eBay.Service.Core.Soap;
using getitemtransactions.com.ebay.developer;
using getitemtransactions.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        static decimal parsePrice(string priceStr)
        {
            decimal price = 0;
            bool r = decimal.TryParse(priceStr, out price);
            return price;
        }

        // Purpose of GetSingleItem is to fetch properties such as a listing's description and photos
        // it is used when performing an auto-listing
        public static async Task<SellerOrderHistory> GetSingleItem(string itemId)
        {
            //StringReader sr;
            string output;
            string appID = ConfigurationManager.AppSettings["AppID"];
            //DataModelsDB db = new DataModelsDB();
            //var profile = db.UserProfiles.Find(user.Id);

            Shopping svc = new Shopping();
            // set the URL and it's parameters
            svc.Url = string.Format("http://open.api.ebay.com/shopping?callname=GetSingleItem&IncludeSelector=Description,ItemSpecifics&appid={0}&version=515&ItemID={1}", appID, itemId);
            // create a new request type
            GetSingleItemRequestType request = new GetSingleItemRequestType();
            // create a new response type
            GetSingleItemResponseType response = new GetSingleItemResponseType();

            string uri = svc.Url;
            using (HttpClient httpClient = new HttpClient())
            {
                string s = await httpClient.GetStringAsync(uri);
                s = s.Replace("\"", "'");
                output = s.Replace(" xmlns='urn:ebay:apis:eBLBaseComponents'", string.Empty);

                XElement root = XElement.Parse(output);
                var qryRecords = from record in root.Elements("Item")
                                    select record;
                var r = (from r2 in qryRecords
                            select new
                            {
                                Description = r2.Element("Description"),
                                Title = r2.Element("Title"),
                                Price = r2.Element("ConvertedCurrentPrice"),
                                ListingUrl = r2.Element("ViewItemURLForNaturalSearch"),
                                PrimaryCategoryID = r2.Element("PrimaryCategoryID"),
                                PrimaryCategoryName = r2.Element("PrimaryCategoryName")
                            }).Single();

                var list = qryRecords.Elements("PictureURL")
                        .Select(element => element.Value)
                        .ToArray();

                var si = new SellerOrderHistory();
                si.PictureUrl = Util.ListToDelimited(list, ';');
                si.Title = r.Title.Value;
                si.Description = r.Description.Value;
                si.EbaySellerPrice = parsePrice(r.Price.Value);
                si.EbayUrl = r.ListingUrl.Value;
                si.PrimaryCategoryID = r.PrimaryCategoryID.Value;
                si.PrimaryCategoryName = r.PrimaryCategoryName.Value;
                return si;
            }
        }
    }
}
