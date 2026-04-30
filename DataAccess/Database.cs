using System;
using System.Data.SQLite;

namespace DataAccess
{
    public static class Database
    {
        public static string DbFile = "EZPos.db";

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection($"Data Source={DbFile};Version=3;");
        }

        public static void Initialize()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Barcode TEXT UNIQUE NOT NULL,
                    Name TEXT NOT NULL,
                    Price REAL NOT NULL,
                    Stock INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Sales (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DateTime TEXT NOT NULL,
                    TotalAmount REAL NOT NULL
                );
                CREATE TABLE IF NOT EXISTS SaleItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SaleId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    Quantity INTEGER NOT NULL,
                    Price REAL NOT NULL,
                    FOREIGN KEY(SaleId) REFERENCES Sales(Id),
                    FOREIGN KEY(ProductId) REFERENCES Products(Id)
                );
                ";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
