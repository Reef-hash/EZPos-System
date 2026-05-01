using System.Collections.Generic;
using System.Data.SQLite;
using EZPos.Models.Domain;

namespace EZPos.DataAccess.Repositories
{
    public static class ProductRepository
    {
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
                    {
                        return new Product
                        {
                            Id = reader.GetInt32(0),
                            Barcode = reader.GetString(1),
                            Name = reader.GetString(2),
                            Price = (decimal)reader.GetDouble(3),
                            Stock = reader.GetInt32(4)
                        };
                    }
                }
            }
            return null;
        }

        public static List<Product> GetAll()
        {
            var list = new List<Product>();
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM Products";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            Barcode = reader.GetString(1),
                            Name = reader.GetString(2),
                            Price = (decimal)reader.GetDouble(3),
                            Stock = reader.GetInt32(4)
                        });
                    }
                }
            }
            return list;
        }

        public static void UpdateStock(int productId, int newStock)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Products SET Stock = @stock WHERE Id = @id";
                cmd.Parameters.AddWithValue("@stock", newStock);
                cmd.Parameters.AddWithValue("@id", productId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void Add(Product product)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Products (Barcode, Name, Price, Stock) VALUES (@barcode, @name, @price, @stock)";
                cmd.Parameters.AddWithValue("@barcode", product.Barcode);
                cmd.Parameters.AddWithValue("@name", product.Name);
                cmd.Parameters.AddWithValue("@price", product.Price);
                cmd.Parameters.AddWithValue("@stock", product.Stock);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
