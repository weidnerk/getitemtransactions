using System.Data.Entity;

namespace getitemtransactions.Models
{
    public class DataModelsDB : DbContext
    {
        static DataModelsDB()
        {
            //do not try to create a database 
            Database.SetInitializer<DataModelsDB>(null);
        }

        public DataModelsDB()
            : base("name=OPWContext")
        {
        }

        public DbSet<EbaySamsSellerMap> SamsSellerResult { get; set; }
        public DbSet<SellerOrderHistory> EbayOrders { get; set; }
        public DbSet<vwSellerMap> SellerMap { get; set; }

        public void StoreOrder(SellerOrderHistory order)
        {
            this.EbayOrders.Add(order);
            this.SaveChanges();
        }
        public void RemoveOrder(string ebayItemId)
        {
            Database.ExecuteSqlCommand("delete from SellerOrderHistory where itemId = '" + ebayItemId + "'");
        }

        public void RemoveOrders(int categoryId)
        {
            Database.ExecuteSqlCommand("delete from SellerOrderHistory where CategoryId = " + categoryId);
        }
    }
}
