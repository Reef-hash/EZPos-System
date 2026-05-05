using System;
using System.Collections.Generic;
using System.Data.SQLite;
using EZPos.DataAccess.Repositories;

namespace EZPos.Business.Services
{
    // ── Data transfer objects ─────────────────────────────────────────────────

    public class PeriodSummary
    {
        public decimal TotalRevenue    { get; set; }
        public int     TotalOrders     { get; set; }
        public decimal AverageOrder    { get; set; }
        public int     TotalItemsSold  { get; set; }
    }

    public class DailySales
    {
        public string  Label    { get; set; } = string.Empty; // e.g. "Mon", "01 May"
        public decimal Revenue  { get; set; }
    }

    public class TopProductResult
    {
        public int     Rank     { get; set; }
        public string  Name     { get; set; } = string.Empty;
        public int     Quantity { get; set; }
        public decimal Revenue  { get; set; }
    }

    public class HourlySales
    {
        public string  TimeSlot { get; set; } = string.Empty;
        public int     Orders   { get; set; }
        public decimal Sales    { get; set; }
    }

    public class LowStockAlert
    {
        public string Name         { get; set; } = string.Empty;
        public int    Stock        { get; set; }
        public int    ReorderLevel { get; set; }
    }

    public class TransactionRecord
    {
        public int     SaleId        { get; set; }
        public DateTime DateTime     { get; set; }
        public string  PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount   { get; set; }
        public int     ItemCount     { get; set; }
        public string  ItemsSummary  { get; set; } = string.Empty; // "ProductA x2, ProductB x1"
    }

    public class PaymentBreakdown
    {
        public string  Method  { get; set; } = string.Empty;
        public int     Orders  { get; set; }
        public decimal Revenue { get; set; }
    }

    public class StockSnapshot
    {
        public string  Barcode      { get; set; } = string.Empty;
        public string  Name         { get; set; } = string.Empty;
        public string  Category     { get; set; } = string.Empty;
        public decimal Price        { get; set; }
        public int     Stock        { get; set; }
        public int     ReorderLevel { get; set; }
        public string  Status       { get; set; } = string.Empty; // OK / Low / Out
    }

    // ── Service ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Queries the SQLite database for reporting data.
    /// All methods accept a date range and return safe defaults when no data exists.
    /// </summary>
    public class ReportService
    {
        /// <summary>Returns totals (revenue, orders, avg order, items sold) for a date range.</summary>
        public PeriodSummary GetSummary(DateTime from, DateTime to)
        {
            var result = new PeriodSummary();
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        COALESCE(SUM(s.TotalAmount), 0)        AS Revenue,
                        COUNT(DISTINCT s.Id)                   AS Orders,
                        COALESCE(SUM(si.Quantity), 0)          AS Items
                    FROM Sales s
                    LEFT JOIN SaleItems si ON si.SaleId = s.Id
                    WHERE DATE(s.DateTime) BETWEEN @from AND @to";
                cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@to",   to.ToString("yyyy-MM-dd"));

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    result.TotalRevenue   = reader.GetDecimal(0);
                    result.TotalOrders    = reader.GetInt32(1);
                    result.TotalItemsSold = reader.GetInt32(2);
                    result.AverageOrder   = result.TotalOrders > 0
                        ? result.TotalRevenue / result.TotalOrders
                        : 0;
                }
            }
            catch { /* return empty defaults on any DB error */ }
            return result;
        }

        /// <summary>Returns daily revenue for each day in the range (up to 31 days).</summary>
        public List<DailySales> GetDailyBreakdown(DateTime from, DateTime to)
        {
            var result = new List<DailySales>();
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT DATE(DateTime) AS Day, COALESCE(SUM(TotalAmount), 0) AS Revenue
                    FROM Sales
                    WHERE DATE(DateTime) BETWEEN @from AND @to
                    GROUP BY Day
                    ORDER BY Day ASC";
                cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@to",   to.ToString("yyyy-MM-dd"));

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var day = DateTime.Parse(reader.GetString(0));
                    result.Add(new DailySales
                    {
                        Label   = (to - from).TotalDays <= 7
                            ? day.ToString("ddd")        // Mon, Tue … for week view
                            : day.ToString("dd MMM"),    // 01 May … for month view
                        Revenue = reader.GetDecimal(1)
                    });
                }
            }
            catch { }
            return result;
        }

        /// <summary>Returns top N products by quantity sold in a date range.</summary>
        public List<TopProductResult> GetTopProducts(DateTime from, DateTime to, int top = 5)
        {
            var result = new List<TopProductResult>();
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        p.Name,
                        COALESCE(SUM(si.Quantity), 0)            AS Qty,
                        COALESCE(SUM(si.Quantity * si.Price), 0) AS Rev
                    FROM SaleItems si
                    JOIN Products p  ON p.Id  = si.ProductId
                    JOIN Sales    s  ON s.Id  = si.SaleId
                    WHERE DATE(s.DateTime) BETWEEN @from AND @to
                    GROUP BY p.Id, p.Name
                    ORDER BY Qty DESC
                    LIMIT @top";
                cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@to",   to.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@top",  top);

                int rank = 1;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new TopProductResult
                    {
                        Rank     = rank++,
                        Name     = reader.GetString(0),
                        Quantity = reader.GetInt32(1),
                        Revenue  = reader.GetDecimal(2)
                    });
                }
            }
            catch { }
            return result;
        }

        /// <summary>Returns sales grouped by hour for a single day.</summary>
        public List<HourlySales> GetHourlyBreakdown(DateTime day)
        {
            var result = new List<HourlySales>();
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        STRFTIME('%H', DateTime)         AS Hour,
                        COUNT(*)                         AS Orders,
                        COALESCE(SUM(TotalAmount), 0)   AS Sales
                    FROM Sales
                    WHERE DATE(DateTime) = @day
                    GROUP BY Hour
                    ORDER BY Hour ASC";
                cmd.Parameters.AddWithValue("@day", day.ToString("yyyy-MM-dd"));

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int h = int.Parse(reader.GetString(0));
                    result.Add(new HourlySales
                    {
                        TimeSlot = $"{h:D2}:00-{h + 1:D2}:00",
                        Orders   = reader.GetInt32(1),
                        Sales    = reader.GetDecimal(2)
                    });
                }
            }
            catch { }
            return result;
        }

        /// <summary>Returns products at or below their reorder level.</summary>
        public List<LowStockAlert> GetLowStockAlerts()
        {
            var result = new List<LowStockAlert>();
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Name, Stock, ReorderLevel
                    FROM Products
                    WHERE Stock <= ReorderLevel
                    ORDER BY Stock ASC";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new LowStockAlert
                    {
                        Name         = reader.GetString(0),
                        Stock        = reader.GetInt32(1),
                        ReorderLevel = reader.GetInt32(2)
                    });
                }
            }
            catch { }
            return result;
        }

        /// <summary>Returns today's summary (convenience wrapper).</summary>
        public PeriodSummary GetTodaySummary()
            => GetSummary(DateTime.Today, DateTime.Today);

        /// <summary>Returns all individual sales transactions in the date range, with an items summary string.</summary>
        public List<TransactionRecord> GetTransactions(DateTime from, DateTime to)
        {
            var result = new List<TransactionRecord>();
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                // First pass: get sale headers
                var salesCmd = conn.CreateCommand();
                salesCmd.CommandText = @"
                    SELECT Id, DateTime, PaymentMethod, TotalAmount
                    FROM Sales
                    WHERE DATE(DateTime) BETWEEN @from AND @to
                    ORDER BY DateTime DESC";
                salesCmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
                salesCmd.Parameters.AddWithValue("@to",   to.ToString("yyyy-MM-dd"));

                var sales = new List<(int id, DateTime dt, string method, decimal total)>();
                using (var r = salesCmd.ExecuteReader())
                    while (r.Read())
                        sales.Add((r.GetInt32(0), DateTime.Parse(r.GetString(1)), r.GetString(2), r.GetDecimal(3)));

                foreach (var (id, dt, method, total) in sales)
                {
                    // Second pass: get items for this sale
                    var itemsCmd = conn.CreateCommand();
                    itemsCmd.CommandText = @"
                        SELECT p.Name, si.Quantity
                        FROM SaleItems si
                        JOIN Products p ON p.Id = si.ProductId
                        WHERE si.SaleId = @saleId
                        ORDER BY p.Name";
                    itemsCmd.Parameters.AddWithValue("@saleId", id);

                    int count = 0;
                    var parts = new List<string>();
                    using (var ir = itemsCmd.ExecuteReader())
                        while (ir.Read())
                        {
                            count += ir.GetInt32(1);
                            parts.Add($"{ir.GetString(0)} x{ir.GetInt32(1)}");
                        }

                    result.Add(new TransactionRecord
                    {
                        SaleId        = id,
                        DateTime      = dt,
                        PaymentMethod = method,
                        TotalAmount   = total,
                        ItemCount     = count,
                        ItemsSummary  = string.Join(", ", parts)
                    });
                }
            }
            catch { }
            return result;
        }

        /// <summary>Returns revenue and order count grouped by payment method for a date range.</summary>
        public List<PaymentBreakdown> GetPaymentBreakdown(DateTime from, DateTime to)
        {
            var result = new List<PaymentBreakdown>();
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        PaymentMethod,
                        COUNT(*)                       AS Orders,
                        COALESCE(SUM(TotalAmount), 0)  AS Revenue
                    FROM Sales
                    WHERE DATE(DateTime) BETWEEN @from AND @to
                    GROUP BY PaymentMethod
                    ORDER BY Revenue DESC";
                cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@to",   to.ToString("yyyy-MM-dd"));

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(new PaymentBreakdown
                    {
                        Method  = reader.GetString(0),
                        Orders  = reader.GetInt32(1),
                        Revenue = reader.GetDecimal(2)
                    });
            }
            catch { }
            return result;
        }

        /// <summary>Returns a snapshot of all products with their current stock status.</summary>
        public List<StockSnapshot> GetStockSnapshot()
        {
            var result = new List<StockSnapshot>();
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Barcode, Name, Category, Price, Stock, ReorderLevel
                    FROM Products
                    ORDER BY Category ASC, Name ASC";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int stock  = reader.GetInt32(4);
                    int reorder = reader.GetInt32(5);
                    result.Add(new StockSnapshot
                    {
                        Barcode      = reader.GetString(0),
                        Name         = reader.GetString(1),
                        Category     = reader.GetString(2),
                        Price        = reader.GetDecimal(3),
                        Stock        = stock,
                        ReorderLevel = reorder,
                        Status       = stock == 0 ? "Out of Stock" : stock <= reorder ? "Low Stock" : "OK"
                    });
                }
            }
            catch { }
            return result;
        }
    }
}
