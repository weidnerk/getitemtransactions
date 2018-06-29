using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace getitemtransactions.Models
{
    [Table("vwSellerMap")]
    public class vwSellerMap
    {
        public string EbayTitle { get; set; }
        public string SamsTitle { get; set; }
        public string EbayUrl { get; set; }
        public string SamsUrl { get; set; }
        public int CategoryId { get; set; }
        [Key]
        public string EbayItemID { get; set; }
        public string SamsItemID { get; set; }
    }
    [Table("SamsClubItems")]
    public class SamsClubItem
    {
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string Url { get; set; }
        public string Limit { get; set; }
        public string SeoAvgRating { get; set; }
        public string SeoBestRating { get; set; }
        public string SeoReviewCount { get; set; }
        [Key]
        public string ItemId { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }

    }

    [Table("SellerOrderHistory")]
    public class SellerOrderHistory
    {
        public int ID { get; set; }
        public int SourceID { get; set; }
        public string Title { get; set; }
        public decimal EbaySellerPrice { get; set; }
        public string Qty { get; set; }
        //public string DateOfPurchaseStr { get; set; }
        public DateTime? DateOfPurchase { get; set; }
        public string EbayUrl { get; set; }

        public string ImageUrl { get; set; }
        public bool ListingEnded { get; set; }
        public int PageNumber { get; set; }
        public string ItemId { get; set; }
        public string SupplierItemId { get; set; }
        public string PrimaryCategoryID { get; set; }
        public string PrimaryCategoryName { get; set; }
        public string PictureUrl { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public string SourceDescription { get; set; }
        public string EbaySeller { get; set; }
    }

    [Table("EbaySamsSellerMap")]
    public class EbaySamsSellerMap
    {
        public int ID { get; set; }
        public string EbaySeller { get; set; }
        public string EbayItemId { get; set; }
        public string SamsClubItemId { get; set; }
        public string EbayUrl { get; set; }
        public string Title { get; set; }
        public int CategoryId { get; set; }
    }
}
