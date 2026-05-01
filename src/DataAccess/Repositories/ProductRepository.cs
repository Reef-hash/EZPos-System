using System;
using System.Collections.Generic;
using System.Data.SQLite;
using EZPos.Models.Domain;

namespace EZPos.DataAccess.Repositories
{
    public static class ProductRepository
    {
        private static Product MapRow(SQLiteDataReader reader)
        {
            return new Product
            {
                Id           = reader.GetInt32(reader.GetOrdinal("Id")),
                Barcode      = reader.GetString(reader.GetOrdinal("Barcode")),
                Name         = reader.GetString(reader.GetOrdinal("Name")),
                Price        = (decimal)reader.GetDouble(reader.GetOrdinal("Price")),
                Stock        = reader.GetInt32(reader.GetOrdinal("Stock")),
                Category     = reader.GetString(reader.GetOrdinal("Category")),
                ReorderLevel = reader.GetInt32(reader.GetOrdinal("ReorderLevel")),
                MaxStock     = reader.GetInt32(reader.GetOrdinal("MaxStock")),
                LastUpdated  = DateTime.TryParse(reader.GetString(reader.GetOrdinal("LastUpdated")), out var dt)
                                   ? dt : DateTime.Now
            };
        }

        public static List<Product> GetAll()
        {
            var list = new List<Product>();
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM Products ORDER BY Name";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapRow(reader));
                }
            }
            return list;
        }

        public static Product GetByBarcode(string barcode)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM Products WHERE Barcode = @barcode";
                cmd.Parameters.AddWithValue("@barcode", barcode);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapRow(reader);
                }
            }
            return null;
        }

        public static int Add(Product product)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Products (Barcode, Name, Price, Stock, Category, ReorderLevel, MaxStock, LastUpdated)
                    VALUES (@barcode, @name, @price, @stock, @category, @reorderLevel, @maxStock, @lastUpdated);
                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@barcode",      product.Barcode);
                cmd.Parameters.AddWithValue("@name",         product.Name);
                cmd.Parameters.AddWithValue("@price",        (double)product.Price);
                cmd.Parameters.AddWithValue("@stock",        product.Stock);
                cmd.Parameters.AddWithValue("@category",     product.Category);
                cmd.Parameters.AddWithValue("@reorderLevel", product.ReorderLevel);
                cmd.Parameters.AddWithValue("@maxStock",     product.MaxStock);
                cmd.Parameters.AddWithValue("@lastUpdated",  product.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss"));
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void Update(Product product)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Products
                    SET Barcode = @barcode, Name = @name, Price = @price, Stock = @stock,
                        Category = @category, ReorderLevel = @reorderLevel,
                        MaxStock = @maxStock, LastUpdated = @lastUpdated
                    WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id",           product.Id);
                cmd.Parameters.AddWithValue("@barcode",      product.Barcode);
                cmd.Parameters.AddWithValue("@name",         product.Name);
                cmd.Parameters.AddWithValue("@price",        (double)product.Price);
                cmd.Parameters.AddWithValue("@stock",        product.Stock);
                cmd.Parameters.AddWithValue("@category",     product.Category);
                cmd.Parameters.AddWithValue("@reorderLevel", product.ReorderLevel);
                cmd.Parameters.AddWithValue("@maxStock",     product.MaxStock);
                cmd.Parameters.AddWithValue("@lastUpdated",  product.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateStock(int productId, int newStock)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Products SET Stock = @stock, LastUpdated = @dt WHERE Id = @id";
                cmd.Parameters.AddWithValue("@stock", newStock);
                cmd.Parameters.AddWithValue("@dt",    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@id",    productId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void Delete(int productId)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Products WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", productId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

