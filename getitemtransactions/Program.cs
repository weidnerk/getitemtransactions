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
        static void Main(string[] args)
        {

            string itemId = "182199535774";
            var r = getTransactions(itemId);    
            foreach(OrderHistory o in r)
            {
                Console.WriteLine(o.ItemId);
                Console.WriteLine(o.SupplierPrice);
                Console.WriteLine(o.Qty);
                Console.WriteLine(o.DateOfPurchase);
                Console.WriteLine("");
            }
            Console.ReadKey();
        }

        static List<OrderHistory> getTransactions(string itemId)
        {
            DateTime ModTimeTo = DateTime.Now.ToUniversalTime();
            DateTime ModTimeFrom = ModTimeTo.AddDays(-30);

            var transactions = ebayAPIs.GetItemTransactions(itemId, ModTimeFrom, ModTimeTo);
            var orderHistory = new List<OrderHistory>();

            foreach (TransactionType item in transactions)
            {
                // did it sell?
                if (item.MonetaryDetails != null)
                {
                    var pmtTime = item.MonetaryDetails.Payments.Payment[0].PaymentTime;
                    var pmtAmt = item.MonetaryDetails.Payments.Payment[0].PaymentAmount.Value;
                    var order = new OrderHistory();
                    //order.Title = searchItem.title;
                    order.Qty = item.QuantityPurchased.ToString();

                    order.SupplierPrice = item.TransactionPrice.Value.ToString();

                    order.DateOfPurchase = item.CreatedDate;
                    //order.EbayUrl = searchItem.viewItemURL;
                    //order.ImageUrl = searchItem.galleryURL;
                    //var pictures = searchItem.pictureURLLarge;
                    //order.PageNumber = pg;
                    order.ItemId = itemId;

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
