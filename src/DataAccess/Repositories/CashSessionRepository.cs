using System;
using System.Collections.Generic;
using System.Data.SQLite;
using EZPos.Models.Domain;

namespace EZPos.DataAccess.Repositories
{
    public static class CashSessionRepository
    {
        // ── Write operations ──────────────────────────────────────────────────

        /// <summary>
        /// Inserts a new Open session and returns its generated Id.
        /// </summary>
        public static int OpenSession(CashSession session)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CashSessions (OpenedAt, OpenedByUser, OpeningBalance, ExpectedCash, Status)
                VALUES (@openedAt, @openedBy, @opening, @expected, 'Open');
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@openedAt", session.OpenedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@openedBy", session.OpenedByUser);
            cmd.Parameters.AddWithValue("@opening",  (double)session.OpeningBalance);
            cmd.Parameters.AddWithValue("@expected", (double)session.OpeningBalance); // starts equal to opening
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Closes an open session, recording the actual cash count, expected cash,
        /// discrepancy, closing user, timestamp, and optional notes.
        /// </summary>
        public static void CloseSession(int sessionId, string closedByUser, decimal expectedCash,
                                        decimal actualCash, decimal discrepancy, string? notes)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE CashSessions
                SET ClosedAt          = @closedAt,
                    ClosedByUser      = @closedBy,
                    ExpectedCash      = @expected,
                    ActualCash        = @actual,
                    DiscrepancyAmount = @discrepancy,
                    Status            = 'Closed',
                    Notes             = @notes
                WHERE Id = @id AND Status = 'Open'";
            cmd.Parameters.AddWithValue("@closedAt",    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@closedBy",    closedByUser);
            cmd.Parameters.AddWithValue("@expected",    (double)expectedCash);
            cmd.Parameters.AddWithValue("@actual",      (double)actualCash);
            cmd.Parameters.AddWithValue("@discrepancy", (double)discrepancy);
            cmd.Parameters.AddWithValue("@notes",       (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id",          sessionId);
            cmd.ExecuteNonQuery();
        }

        // ── Read operations ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the single currently-open session, or null if no shift is open.
        /// </summary>
        public static CashSession? GetOpenSession()
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM CashSessions WHERE Status = 'Open' ORDER BY OpenedAt DESC LIMIT 1";
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapRow(r) : null;
        }

        /// <summary>
        /// Returns the most recent sessions ordered by OpenedAt descending.
        /// </summary>
        public static List<CashSession> GetRecentSessions(int limit = 50)
        {
            var results = new List<CashSession>();
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM CashSessions ORDER BY OpenedAt DESC LIMIT @limit";
            cmd.Parameters.AddWithValue("@limit", limit);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                results.Add(MapRow(r));
            return results;
        }

        /// <summary>
        /// Calculates the total cash sales (PaymentMethod = 'Cash') taken between
        /// two timestamps (inclusive). Used to derive ExpectedCash at close.
        /// </summary>
        public static decimal GetCashSalesTotal(DateTime from, DateTime to)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(TotalAmount), 0)
                FROM Sales
                WHERE PaymentMethod = 'Cash'
                  AND DateTime >= @from
                  AND DateTime <= @to";
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@to",   to.ToString("yyyy-MM-dd HH:mm:ss"));
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        // ── Mapping ───────────────────────────────────────────────────────────

        private static CashSession MapRow(SQLiteDataReader r)
        {
            return new CashSession
            {
                Id                = r.GetInt32(r.GetOrdinal("Id")),
                OpenedAt          = DateTime.Parse(r.GetString(r.GetOrdinal("OpenedAt"))),
                ClosedAt          = r.IsDBNull(r.GetOrdinal("ClosedAt"))
                                        ? null
                                        : DateTime.Parse(r.GetString(r.GetOrdinal("ClosedAt"))),
                OpenedByUser      = r.GetString(r.GetOrdinal("OpenedByUser")),
                ClosedByUser      = r.IsDBNull(r.GetOrdinal("ClosedByUser"))
                                        ? null
                                        : r.GetString(r.GetOrdinal("ClosedByUser")),
                OpeningBalance    = (decimal)r.GetDouble(r.GetOrdinal("OpeningBalance")),
                ExpectedCash      = (decimal)r.GetDouble(r.GetOrdinal("ExpectedCash")),
                ActualCash        = r.IsDBNull(r.GetOrdinal("ActualCash"))
                                        ? null
                                        : (decimal?)r.GetDouble(r.GetOrdinal("ActualCash")),
                DiscrepancyAmount = r.IsDBNull(r.GetOrdinal("DiscrepancyAmount"))
                                        ? null
                                        : (decimal?)r.GetDouble(r.GetOrdinal("DiscrepancyAmount")),
                Status            = r.GetString(r.GetOrdinal("Status")),
                Notes             = r.IsDBNull(r.GetOrdinal("Notes"))
                                        ? null
                                        : r.GetString(r.GetOrdinal("Notes"))
            };
        }
    }
}
