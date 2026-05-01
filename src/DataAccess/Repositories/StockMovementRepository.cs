using System;
using System.Collections.Generic;
using System.Data.SQLite;
using EZPos.Models.Domain;

namespace EZPos.DataAccess.Repositories
{
    public static class StockMovementRepository
    {
        public static void Insert(StockMovement movement)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO StockMovements (ProductId, ChangeQty, Reason, DateTime)
                    VALUES (@productId, @changeQty, @reason, @dateTime)";
                cmd.Parameters.AddWithValue("@productId", movement.ProductId);
                cmd.Parameters.AddWithValue("@changeQty", movement.ChangeQty);
                cmd.Parameters.AddWithValue("@reason",    movement.Reason);
                cmd.Parameters.AddWithValue("@dateTime",  movement.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }
        }

        public static void InsertWithConnection(SQLiteConnection conn, StockMovement movement)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO StockMovements (ProductId, ChangeQty, Reason, DateTime)
                VALUES (@productId, @changeQty, @reason, @dateTime)";
            cmd.Parameters.AddWithValue("@productId", movement.ProductId);
            cmd.Parameters.AddWithValue("@changeQty", movement.ChangeQty);
            cmd.Parameters.AddWithValue("@reason",    movement.Reason);
            cmd.Parameters.AddWithValue("@dateTime",  movement.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public static List<StockMovement> GetByProduct(int productId)
        {
            var list = new List<StockMovement>();
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, ProductId, ChangeQty, Reason, DateTime
                    FROM StockMovements WHERE ProductId = @productId
                    ORDER BY DateTime DESC";
                cmd.Parameters.AddWithValue("@productId", productId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new StockMovement
                        {
                            Id        = reader.GetInt32(0),
                            ProductId = reader.GetInt32(1),
                            ChangeQty = reader.GetInt32(2),
                            Reason    = reader.GetString(3),
                            DateTime  = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.Now
                        });
                    }
                }
            }
            return list;
        }
    }
}
