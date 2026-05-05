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
                    cmd.Parameters.AddWithValue("@dt",    sale.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@total", sale.TotalAmount);
                    var saleId = Convert.ToInt32(cmd.ExecuteScalar());

                    foreach (var item in items)
                    {
                        var itemCmd = conn.CreateCommand();
                        itemCmd.CommandText = "INSERT INTO SaleItems (SaleId, ProductId, Quantity, Price) VALUES (@saleId, @productId, @qty, @price)";
                        itemCmd.Parameters.AddWithValue("@saleId",    saleId);
                        itemCmd.Parameters.AddWithValue("@productId", item.ProductId);
                        itemCmd.Parameters.AddWithValue("@qty",       (double)item.Quantity);
                        itemCmd.Parameters.AddWithValue("@price",     (double)item.Price);
                        itemCmd.ExecuteNonQuery();

                        // Update stock for the sold product
                        var stockCmd = conn.CreateCommand();
                        stockCmd.CommandText = "UPDATE Products SET Stock = Stock - @qty WHERE Id = @pid";
                        stockCmd.Parameters.AddWithValue("@qty", (double)item.Quantity);
                        stockCmd.Parameters.AddWithValue("@pid", item.ProductId);
                        stockCmd.ExecuteNonQuery();

                        // For Pack products: also deduct ConversionRate × qty from the parent unit product's stock
                        var typeCmd = conn.CreateCommand();
                        typeCmd.CommandText = "SELECT UnitType, ConversionRate, ParentProductId FROM Products WHERE Id = @pid";
                        typeCmd.Parameters.AddWithValue("@pid", item.ProductId);
                        using (var r = typeCmd.ExecuteReader())
                        {
                            if (r.Read()
                                && r.GetString(0) == "Pack"
                                && !r.IsDBNull(2))
                            {
                                var convRate   = r.GetDouble(1);
                                var parentId   = r.GetInt32(2);
                                var parentDeduction = item.Quantity * (decimal)convRate;

                                var parentStockCmd = conn.CreateCommand();
                                parentStockCmd.CommandText = "UPDATE Products SET Stock = Stock - @deduction WHERE Id = @parentId";
                                parentStockCmd.Parameters.AddWithValue("@deduction", (double)parentDeduction);
                                parentStockCmd.Parameters.AddWithValue("@parentId",  parentId);
                                parentStockCmd.ExecuteNonQuery();

                                StockMovementRepository.InsertWithConnection(conn, new StockMovement
                                {
                                    ProductId = parentId,
                                    ChangeQty = -parentDeduction,
                                    Reason    = $"Pack sale #{saleId} ({item.Quantity}×{convRate} units)",
                                    DateTime  = sale.DateTime
                                });
                            }
                        }

                        // Audit trail — record stock movement for the sale
                        StockMovementRepository.InsertWithConnection(conn, new StockMovement
                        {
                            ProductId = item.ProductId,
                            ChangeQty = -item.Quantity,
                            Reason    = $"Sale #{saleId}",
                            DateTime  = sale.DateTime
                        });
                    }

                    tran.Commit();
                    return saleId;
                }
            }
        }
    }
}
