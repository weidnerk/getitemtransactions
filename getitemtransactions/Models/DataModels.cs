using System;

namespace getitemtransactions.Models
{

    public class OrderHistory
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string SupplierPrice { get; set; }
        public string Qty { get; set; }
        //public string DateOfPurchaseStr { get; set; }
        public DateTime? DateOfPurchase { get; set; }
        public int RptNumber { get; set; }
        public string EbayUrl { get; set; }

        public string ImageUrl { get; set; }
        public bool ListingEnded { get; set; }
        public int PageNumber { get; set; }
        public string ItemId { get; set; }
    }
}
