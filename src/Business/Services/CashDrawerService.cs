using System;
using System.Collections.Generic;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;

namespace EZPos.Business.Services
{
    // ── Result DTOs ───────────────────────────────────────────────────────────

    public class OpenShiftResult
    {
        public bool   Success      { get; set; }
        public int    SessionId    { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class CloseShiftResult
    {
        public bool    Success           { get; set; }
        public decimal ExpectedCash      { get; set; }
        public decimal ActualCash        { get; set; }
        /// <summary>Positive = over; Negative = short.</summary>
        public decimal DiscrepancyAmount { get; set; }
        public string  DiscrepancyLabel  { get; set; } = string.Empty;
        public string  ErrorMessage      { get; set; } = string.Empty;
    }

    // ── Service ───────────────────────────────────────────────────────────────

    public class CashDrawerService
    {
        // ── Shift lifecycle ───────────────────────────────────────────────────

        /// <summary>
        /// Opens a new cashier shift. Fails if another shift is already open.
        /// </summary>
        public OpenShiftResult OpenShift(string openedByUser, decimal openingBalance)
        {
            if (string.IsNullOrWhiteSpace(openedByUser))
                return new OpenShiftResult { Success = false, ErrorMessage = "Cashier name is required." };

            if (openingBalance < 0)
                return new OpenShiftResult { Success = false, ErrorMessage = "Opening balance cannot be negative." };

            var existing = CashSessionRepository.GetOpenSession();
            if (existing != null)
                return new OpenShiftResult
                {
                    Success      = false,
                    ErrorMessage = $"Shift #{existing.Id} opened by '{existing.OpenedByUser}' at {existing.OpenedAt:HH:mm} is still active. Close it before opening a new one."
                };

            var session = new CashSession
            {
                OpenedAt       = DateTime.Now,
                OpenedByUser   = openedByUser.Trim(),
                OpeningBalance = openingBalance,
                ExpectedCash   = openingBalance,
                Status         = "Open"
            };

            var id = CashSessionRepository.OpenSession(session);
            return new OpenShiftResult { Success = true, SessionId = id };
        }

        /// <summary>
        /// Closes the active shift. Calculates expected cash from sales records,
        /// then computes and stores the discrepancy against the user-supplied actual count.
        /// Returns a full result including the discrepancy breakdown.
        /// </summary>
        public CloseShiftResult CloseShift(int sessionId, string closedByUser, decimal actualCash, string? notes)
        {
            if (string.IsNullOrWhiteSpace(closedByUser))
                return new CloseShiftResult { Success = false, ErrorMessage = "Cashier name is required." };

            if (actualCash < 0)
                return new CloseShiftResult { Success = false, ErrorMessage = "Actual cash cannot be negative." };

            var session = CashSessionRepository.GetOpenSession();
            if (session == null || session.Id != sessionId)
                return new CloseShiftResult { Success = false, ErrorMessage = "No matching open shift found." };

            // Calculate expected: opening balance + all cash sales taken this session
            var cashSales    = CashSessionRepository.GetCashSalesTotal(session.OpenedAt, DateTime.Now);
            var expectedCash = session.OpeningBalance + cashSales;
            var discrepancy  = actualCash - expectedCash;

            CashSessionRepository.CloseSession(sessionId, closedByUser.Trim(), expectedCash,
                                               actualCash, discrepancy, notes);

            return new CloseShiftResult
            {
                Success           = true,
                ExpectedCash      = expectedCash,
                ActualCash        = actualCash,
                DiscrepancyAmount = discrepancy,
                DiscrepancyLabel  = BuildDiscrepancyLabel(discrepancy)
            };
        }

        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>Returns the currently-open session, or null if no shift is active.</summary>
        public CashSession? GetActiveSession() => CashSessionRepository.GetOpenSession();

        /// <summary>
        /// Previews expected cash for the active session (used in the Close dialog
        /// so the cashier can see what the system expects before submitting).
        /// Returns null if no session is open.
        /// </summary>
        public decimal? PreviewExpectedCash()
        {
            var session = CashSessionRepository.GetOpenSession();
            if (session == null) return null;
            var cashSales = CashSessionRepository.GetCashSalesTotal(session.OpenedAt, DateTime.Now);
            return session.OpeningBalance + cashSales;
        }

        /// <summary>
        /// Returns revenue grouped by payment method for the active shift.
        /// Returns empty list if no shift is open.
        /// </summary>
        public List<(string Method, decimal Revenue)> GetShiftPaymentBreakdown()
        {
            var session = CashSessionRepository.GetOpenSession();
            if (session == null) return new();
            return CashSessionRepository.GetShiftPaymentBreakdown(session.OpenedAt, DateTime.Now);
        }

        /// <summary>Returns recent closed/open sessions for the history view.</summary>
        public List<CashSession> GetSessionHistory(int limit = 50)
            => CashSessionRepository.GetRecentSessions(limit);

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string BuildDiscrepancyLabel(decimal discrepancy)
        {
            if (discrepancy == 0)   return "Balanced";
            if (discrepancy > 0)    return $"OVER by RM {discrepancy:N2}";
            return                         $"SHORT by RM {Math.Abs(discrepancy):N2}";
        }
    }
}
