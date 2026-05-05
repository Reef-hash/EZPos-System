using System;
using System.IO;
using System.Data.SQLite;

namespace EZPos.DataAccess.Repositories
{
    public static class Database
    {
        /// <summary>
        /// Database file path: %ProgramData%\EZPos\EZPos.db
        /// Initialized on first access and creates folder if needed.
        /// </summary>
        public static string DbFile
        {
            get
            {
                var programDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "EZPos");
                return Path.Combine(programDataDir, "EZPos.db");
            }
        }

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
                    Stock REAL NOT NULL,
                    Category TEXT NOT NULL DEFAULT 'General',
                    ReorderLevel INTEGER NOT NULL DEFAULT 5,
                    MaxStock INTEGER NOT NULL DEFAULT 100,
                    LastUpdated TEXT NOT NULL DEFAULT '',
                    UnitType TEXT NOT NULL DEFAULT 'Unit',
                    ConversionRate REAL NOT NULL DEFAULT 1,
                    ParentProductId INTEGER NULL
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
                    Quantity REAL NOT NULL,
                    Price REAL NOT NULL,
                    FOREIGN KEY(SaleId) REFERENCES Sales(Id),
                    FOREIGN KEY(ProductId) REFERENCES Products(Id)
                );
                CREATE TABLE IF NOT EXISTS StockMovements (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductId INTEGER NOT NULL,
                    ChangeQty REAL NOT NULL,
                    Reason TEXT NOT NULL,
                    DateTime TEXT NOT NULL,
                    FOREIGN KEY(ProductId) REFERENCES Products(Id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
                CREATE TABLE IF NOT EXISTS Categories (
                    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT    NOT NULL UNIQUE
                );
                ";
                cmd.ExecuteNonQuery();

                // Migrate existing Products table if new columns are missing
                MigrateProductsTable(conn);
                // Migrate existing Sales table if PaymentMethod column is missing
                MigrateSalesTable(conn);
                // Seed default categories (no-op if they already exist)
                SeedCategories(conn);
            }
        }

        private static void MigrateProductsTable(SQLiteConnection conn)
        {
            // Existing columns added in previous migrations
            var columns = new[] { "Category", "ReorderLevel", "MaxStock", "LastUpdated" };
            var definitions = new[] { "TEXT NOT NULL DEFAULT 'General'", "INTEGER NOT NULL DEFAULT 5", "INTEGER NOT NULL DEFAULT 100", "TEXT NOT NULL DEFAULT ''" };

            for (int i = 0; i < columns.Length; i++)
            {
                try
                {
                    var alter = conn.CreateCommand();
                    alter.CommandText = $"ALTER TABLE Products ADD COLUMN {columns[i]} {definitions[i]}";
                    alter.ExecuteNonQuery();
                }
                catch (SQLiteException)
                {
                    // Column already exists — safe to ignore
                }
            }

            // v2: product selling type columns
            TryAddColumn(conn, "Products", "UnitType",        "TEXT NOT NULL DEFAULT 'Unit'");
            TryAddColumn(conn, "Products", "ConversionRate",  "REAL NOT NULL DEFAULT 1");
            TryAddColumn(conn, "Products", "ParentProductId", "INTEGER NULL");
        }

        private static void TryAddColumn(SQLiteConnection conn, string table, string column, string definition)
        {
            try
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException)
            {
                // Column already exists — safe to ignore
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

        private static void SeedCategories(SQLiteConnection conn)
        {
            var defaults = new[] { "General", "Food & Beverage", "Electronics", "Household", "Clothing", "Health & Beauty", "Other" };
            foreach (var name in defaults)
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT OR IGNORE INTO Categories (Name) VALUES (@name)";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.ExecuteNonQuery();
            }

            // Also import any category names already in Products that are not yet in the table
            var import = conn.CreateCommand();
            import.CommandText = "INSERT OR IGNORE INTO Categories (Name) SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND Category != ''";
            import.ExecuteNonQuery();
        }
    }
}
