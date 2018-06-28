/*
 * needs an assembly reference to C:\Program Files (x86)\eBay\eBay .NET SDK v1027 Release\eBay.Service.dll
 * to use GetItemTransactions
 * 
 */


using eBay.Service.Core.Soap;
using getitemtransactions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace getitemtransactions
{
    class Program
    {
        static DataModelsDB db = new DataModelsDB();

        static void Main(string[] args)
        {
            int categoryId = 2;
            Task.Run(async () =>
            {
                await Process(categoryId);
            }).Wait();

            //Console.ReadKey();
        }

        static async Task Process(int categoryId)
        {
            var orderHistory = ScanMap(categoryId);
            foreach (SellerOrderHistory o in orderHistory)
            {
                o.SourceID = 1;     // sam's
                o.Title = GetSamsTitle(o.SupplierItemId);
                var singleitem = await ebayAPIs.GetSingleItem(o.ItemId);
                o.PrimaryCategoryID = singleitem.PrimaryCategoryID;
                o.PrimaryCategoryName = singleitem.PrimaryCategoryName;
                o.Description = singleitem.Description;
                o.PictureUrl = singleitem.PictureUrl;
                o.EbaySellerPrice = singleitem.EbaySellerPrice;

                db.RemoveOrder(o.ItemId);
                db.StoreOrder(o);

                Console.WriteLine("~~~~~~~~~ ebay id: " + o.ItemId);
                Console.WriteLine(GetSamsTitle(o.ItemId));     // can get title from OrderHistory table
                Console.WriteLine(o.EbaySellerPrice);
                Console.WriteLine(o.Qty);
                Console.WriteLine(o.DateOfPurchase);
                Console.WriteLine("");
            }
        }

        static string GetSamsTitle(string ebayItemId)
        {
            string title = null;
            var s = db.SamsItems.Where(r => r.ItemId == ebayItemId).FirstOrDefault();
            if (s != null)
                title = s.Title;
            return title;
        }

        static List<SellerOrderHistory> ScanMap(int categoryId)
        {
            int i = 0;
            var allHistory = new List<SellerOrderHistory>();
            var orderHistory = new List<SellerOrderHistory>();
            foreach (vwSellerMap item in db.SellerMap.Where(r => r.CategoryId == categoryId).ToList())
            {
                Console.WriteLine((++i).ToString() + "/" + db.SamsSellerResult.Count().ToString() + " " + item.EbayItemID);
                orderHistory = getTransactions(item.EbayItemID, item.SamsItemID, item.EbayUrl);
                if (orderHistory.Count > 0)
                    allHistory.AddRange(orderHistory);
            }
            return allHistory;
        }

        static List<SellerOrderHistory> getTransactions(string ebayItemId, string supplierItemId, string viewItemUrl)
        {
            DateTime ModTimeTo = DateTime.Now.ToUniversalTime();
            DateTime ModTimeFrom = ModTimeTo.AddDays(-30);

            var transactions = ebayAPIs.GetItemTransactions(ebayItemId, ModTimeFrom, ModTimeTo);
            var orderHistory = new List<SellerOrderHistory>();

            foreach (TransactionType item in transactions)
            {
                // did it sell?
                if (item.MonetaryDetails != null)
                {
                    var pmtTime = item.MonetaryDetails.Payments.Payment[0].PaymentTime;
                    var pmtAmt = item.MonetaryDetails.Payments.Payment[0].PaymentAmount.Value;
                    var order = new SellerOrderHistory();
                    //order.Title = searchItem.title;
                    order.Qty = item.QuantityPurchased.ToString();
                    
                    order.EbaySellerPrice = (decimal)item.TransactionPrice.Value;

                    order.DateOfPurchase = item.CreatedDate;
                    order.EbayUrl = viewItemUrl;
                    //order.ImageUrl = searchItem.galleryURL;
                    //var pictures = searchItem.pictureURLLarge;
                    //order.PageNumber = pg;
                    order.ItemId = ebayItemId;
                    order.SupplierItemId = supplierItemId;
                    // testing GetSingleItem
                    // purpose of GetSingleItem is to fetch properties like listing descriptiong
                    // it is used when performing an auto-listing
                    // var r = await GetSingleItem(order.ItemId, user);

                    orderHistory.Add(order);
                }
                else
                {
                    // i don't see this ever being executed which makes sense if querying only sold items
                    //HomeController.WriteFile(_logfile, "Unexpected: item.MonetaryDetails == null");
                }
            }
            return orderHistory;
        }
    }
}
