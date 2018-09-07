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
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallib.Models;

namespace getitemtransactions
{
    class Program
    {
        static DataModelsDB db = new DataModelsDB();
        static samsDB samsdb = new samsDB();
        static walDB waldb = new walDB();
        private static string Log_File = "log.txt";

        static void Main(string[] args)
        {
            int count = 0;
            string msg = null;
            if (args == null || args.Count() == 0)
            {
                Console.WriteLine("please provide a category");
            }
            else
            {
                int categoryId = Convert.ToInt32(args[0]);
                try
                {
                    Task.Run(async () =>
                    {
                        msg = "start processing category " + categoryId;
                        dsutil.DSUtil.WriteFile(Log_File, msg);

                        count = await Process(categoryId);
                        count = await WalProcess(categoryId);

                        msg = "processed " + count.ToString() + " items";
                        dsutil.DSUtil.WriteFile(Log_File, msg);

                    }).Wait();
                }
                catch (Exception exc)
                {
                    dsutil.DSUtil.WriteFile(Log_File, exc.Message);
                }
            }
            //Console.ReadKey();
        }

        static async Task<int> WalProcess(int categoryId)
        {
            db.RemoveOrders(categoryId);
            var orderHistory = GetWalOrders(categoryId);
            foreach (SellerOrderHistory o in orderHistory)
            {
                o.SourceID = 2;
                var walItem = GetWalItem(o.SupplierItemId);
                var sellerItem = GetWalSellerItem(o.ItemId);
                o.Title = walItem.Title;
                o.SourceDescription = walItem.Description;
                o.SupplierItemId = walItem.ItemId;

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
                Console.WriteLine(walItem.Title);
                Console.WriteLine(o.EbaySellerPrice);
                Console.WriteLine(o.Qty);
                Console.WriteLine(o.DateOfPurchase);
                Console.WriteLine(o.EbaySeller);
                Console.WriteLine("");
            }
            return orderHistory.Count();
        }

        static async Task<int> Process(int categoryId)
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
                Console.WriteLine(o.EbaySeller);
                Console.WriteLine("");
            }
            return orderHistory.Count();
        }

        static WalItem GetWalItem(string walItemId)
        {
            var s = waldb.WalItems.Where(r => r.ItemId == walItemId).FirstOrDefault();
            return s;
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
        static WalMap GetWalSellerItem(string sellerItemId)
        {
            var s = waldb.WalMapItems.Where(r => r.EbayItemId == sellerItemId).FirstOrDefault();
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

            List<vwSellerMap> data =
                db.Database.SqlQuery<vwSellerMap>(
                "select * from dbo.fnSellerMap(@categoryId)",
                new SqlParameter("@categoryId", categoryId))
            .ToList();
            int count = data.Count;
            // for now, not including variation listings
            foreach (vwSellerMap item in data.Where(r => r.CategoryId == categoryId && !r.IsMultiVariationListing).ToList())
            {
                if (item.SamsItemID == "980043352")
                {
                    int hold = 999;
                }

                Console.WriteLine((++i).ToString() + "/" + count.ToString() + " " + item.EbayItemID);
                orderHistory = GetTransactions(item.EbayItemID, item.SamsItemID, item.EbayUrl, batchId);
                if (orderHistory != null)
                {
                    if (orderHistory.Count > 0)
                        allHistory.AddRange(orderHistory);
                }
            }
            return allHistory;
        }

        static List<SellerOrderHistory> GetWalOrders(int categoryId)
        {
            Random rnd = new Random();
            int batchId = rnd.Next(1, 1000000);

            int i = 0;
            var allHistory = new List<SellerOrderHistory>();
            var orderHistory = new List<SellerOrderHistory>();

            List<vwWalSellerMap> data =
                db.Database.SqlQuery<vwWalSellerMap>(
                "select * from dbo.fnWalSellerMap(@categoryId)",
                new SqlParameter("@categoryId", categoryId))
            .ToList();
            int count = data.Count;
            // for now, not including variation listings
            foreach (vwWalSellerMap item in data.Where(r => r.CategoryId == categoryId && !r.IsMultiVariationListing).ToList())
            {
                Console.WriteLine((++i).ToString() + "/" + count.ToString() + " " + item.EbayItemID);
                orderHistory = GetTransactions(item.EbayItemID, item.WalItemID, item.EbayUrl, batchId);
                if (orderHistory != null)
                {
                    if (orderHistory.Count > 0)
                        allHistory.AddRange(orderHistory);
                }
            }
            return allHistory;
        }

        //static List<SellerOrderHistory> GetOrders_Orig(int categoryId)
        //{
        //    Random rnd = new Random();
        //    int batchId = rnd.Next(1, 1000000);

        //    int i = 0;
        //    var allHistory = new List<SellerOrderHistory>();
        //    var orderHistory = new List<SellerOrderHistory>();
        //    int count = db.SellerMap.Where(r => r.CategoryId == categoryId).ToList().Count;

        //    // for now, not including variation listings
        //    foreach (vwSellerMap item in db.SellerMap.Where(r => r.CategoryId == categoryId && !r.IsMultiVariationListing).ToList())
        //    {
        //        if (item.EbayItemID == "183321220253")
        //        {
        //            int xyz = 999;
        //        }

        //        Console.WriteLine((++i).ToString() + "/" + count.ToString() + " " + item.EbayItemID);
        //        orderHistory = GetTransactions(item.EbayItemID, item.SamsItemID, item.EbayUrl, batchId);
        //        if (orderHistory != null)
        //        {
        //            if (orderHistory.Count > 0)
        //                allHistory.AddRange(orderHistory);
        //        }
        //    }
        //    return allHistory;
        //}

        // Get the orders for an ebay item id
        static List<SellerOrderHistory> GetTransactions(string ebayItemId, string supplierItemId, string viewItemUrl, int categoryId)
        {
            DateTime ModTimeTo = DateTime.Now.ToUniversalTime();
            DateTime ModTimeFrom = ModTimeTo.AddDays(-30);
            var transactions = ebayAPIs.GetItemTransactions(ebayItemId, ModTimeFrom, ModTimeTo);
            if (transactions != null)
            {
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
            else return null;
        }
    }
}
