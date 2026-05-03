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
    }
}
