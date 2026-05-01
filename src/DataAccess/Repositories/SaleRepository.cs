using System;
using System.Collections.Generic;
using System.Data.SQLite;
using EZPos.Models.Domain;

namespace EZPos.DataAccess.Repositories
{
    public static class SaleRepository
    {
        public static int AddSale(Sale sale, List<SaleItem> items)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO Sales (DateTime, TotalAmount) VALUES (@dt, @total); SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@dt", sale.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@total", sale.TotalAmount);
                    var saleId = Convert.ToInt32(cmd.ExecuteScalar());

                    foreach (var item in items)
                    {
                        var itemCmd = conn.CreateCommand();
                        itemCmd.CommandText = "INSERT INTO SaleItems (SaleId, ProductId, Quantity, Price) VALUES (@saleId, @productId, @qty, @price)";
                        itemCmd.Parameters.AddWithValue("@saleId", saleId);
                        itemCmd.Parameters.AddWithValue("@productId", item.ProductId);
                        itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                        itemCmd.Parameters.AddWithValue("@price", item.Price);
                        itemCmd.ExecuteNonQuery();

                        // Update stock
                        var stockCmd = conn.CreateCommand();
                        stockCmd.CommandText = "UPDATE Products SET Stock = Stock - @qty WHERE Id = @pid";
                        stockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                        stockCmd.Parameters.AddWithValue("@pid", item.ProductId);
                        stockCmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                    return saleId;
                }
            }
        }
    }
}
