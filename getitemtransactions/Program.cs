/*
 * Needs an assembly reference to C:\Program Files (x86)\eBay\eBay .NET SDK v1027 Release\eBay.Service.dll
 * to use GetItemTransactions
 * 
 */


using eBay.Service.Core.Soap;
using getitemtransactions.Models;
using sclib.Models;
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
        static samsDB samsdb = new samsDB();

        static void Main(string[] args)
        {
            if (args == null || args.Count() == 0)
            {
                Console.WriteLine("please provide a category");
            }
            else
            {
                int categoryId = Convert.ToInt32(args[0]);
                Task.Run(async () =>
                {
                    await Process(categoryId);
                }).Wait();
            }
            //Console.ReadKey();
        }

        static async Task Process(int categoryId)
        {
            db.RemoveOrders(categoryId);
            var orderHistory = GetOrders(categoryId);
            foreach (SellerOrderHistory o in orderHistory)
            {
                o.SourceID = 1;     // sam's
                var samsItem = GetSamsItem(o.SupplierItemId);
                var sellerItem = GetSellerItem(o.ItemId);
                o.Title = samsItem.Title;
                o.SourceDescription = samsItem.Description;
                o.SupplierItemId = samsItem.ItemId;

                var singleitem = await ebayAPIs.GetSingleItem(o.ItemId);
                o.PrimaryCategoryID = singleitem.PrimaryCategoryID;
                o.PrimaryCategoryName = singleitem.PrimaryCategoryName;
                o.Description = singleitem.Description;
                o.PictureUrl = singleitem.PictureUrl;
                // o.EbaySellerPrice = singleitem.EbaySellerPrice; // need to check accuracy
                o.EbaySeller = sellerItem.EbaySeller;
                o.CategoryId = categoryId;

                //db.RemoveOrder(o.ItemId);
                db.StoreOrder(o);

                Console.WriteLine("~~~~~~~~~ ebay id: " + o.ItemId);
                Console.WriteLine(samsItem.Title);
                Console.WriteLine(o.EbaySellerPrice);
                Console.WriteLine(o.Qty);
                Console.WriteLine(o.DateOfPurchase);
                Console.WriteLine("");
            }
        }

        static SamsClubItem GetSamsItem(string samsItemId)
        {
            var s = samsdb.SamsItems.Where(r => r.ItemId == samsItemId).FirstOrDefault();
            return s;
        }
        static EbaySamsSellerMap GetSellerItem(string sellerItemId)
        {
            var s = db.SamsSellerResult.Where(r => r.EbayItemId == sellerItemId).FirstOrDefault();
            return s;
        }

        // Iterate sellers
        static List<SellerOrderHistory> GetOrders(int categoryId)
        {
            Random rnd = new Random();
            int batchId = rnd.Next(1, 1000000);

            int i = 0;
            var allHistory = new List<SellerOrderHistory>();
            var orderHistory = new List<SellerOrderHistory>();
            int count = db.SellerMap.Where(r => r.CategoryId == categoryId).ToList().Count;
            foreach (vwSellerMap item in db.SellerMap.Where(r => r.CategoryId == categoryId).ToList())
            {
                Console.WriteLine((++i).ToString() + "/" + count.ToString() + " " + item.EbayItemID);
                orderHistory = GetTransactions(item.EbayItemID, item.SamsItemID, item.EbayUrl, batchId);
                if (orderHistory.Count > 0)
                    allHistory.AddRange(orderHistory);
            }
            return allHistory;
        }

        // Get the orders for an ebay item id
        static List<SellerOrderHistory> GetTransactions(string ebayItemId, string supplierItemId, string viewItemUrl, int categoryId)
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
                    order.ShippingAmount = (decimal)item.ActualShippingCost.Value;

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
