using System;
using System.IO;
using System.Data.SQLite;

namespace EZPos.DataAccess.Repositories
{
    public static class Database
    {
        public static string DbFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "EZPos.db");

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection($"Data Source={DbFile};Version=3;");
        }

        public static void Initialize()
        {
            // Ensure the directory exists (safety net for any path configuration)
            var dir = Path.GetDirectoryName(DbFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Ensure the file exists as a valid empty SQLite database before opening
            if (!File.Exists(DbFile))
                SQLiteConnection.CreateFile(DbFile);

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
                    Stock INTEGER NOT NULL,
                    Category TEXT NOT NULL DEFAULT 'General',
                    ReorderLevel INTEGER NOT NULL DEFAULT 5,
                    MaxStock INTEGER NOT NULL DEFAULT 100,
                    LastUpdated TEXT NOT NULL DEFAULT ''
                );
                CREATE TABLE IF NOT EXISTS Sales (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DateTime TEXT NOT NULL,
                    TotalAmount REAL NOT NULL,
                    PaymentMethod TEXT NOT NULL DEFAULT 'Cash'
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
                CREATE TABLE IF NOT EXISTS StockMovements (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductId INTEGER NOT NULL,
                    ChangeQty INTEGER NOT NULL,
                    Reason TEXT NOT NULL,
                    DateTime TEXT NOT NULL,
                    FOREIGN KEY(ProductId) REFERENCES Products(Id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
                ";
                cmd.ExecuteNonQuery();

                // Migrate existing Products table if new columns are missing
                MigrateProductsTable(conn);
                // Migrate existing Sales table if PaymentMethod column is missing
                MigrateSalesTable(conn);
            }
        }

        private static void MigrateProductsTable(SQLiteConnection conn)
        {
            var columns = new[] { "Category", "ReorderLevel", "MaxStock", "LastUpdated" };
            var defaults = new[] { "'General'", "5", "100", "''" };

            for (int i = 0; i < columns.Length; i++)
            {
                try
                {
                    var alter = conn.CreateCommand();
                    alter.CommandText = $"ALTER TABLE Products ADD COLUMN {columns[i]} TEXT NOT NULL DEFAULT {defaults[i]}";
                    alter.ExecuteNonQuery();
                }
                catch (SQLiteException)
                {
                    // Column already exists — safe to ignore
                }
            }
        }

        private static void MigrateSalesTable(SQLiteConnection conn)
        {
            try
            {
                var alter = conn.CreateCommand();
                alter.CommandText = "ALTER TABLE Sales ADD COLUMN PaymentMethod TEXT NOT NULL DEFAULT 'Cash'";
                alter.ExecuteNonQuery();
            }
            catch (SQLiteException)
            {
                // Column already exists — safe to ignore
            }
        }
    }
}
