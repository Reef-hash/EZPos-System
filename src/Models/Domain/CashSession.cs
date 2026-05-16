using System;

namespace EZPos.Models.Domain
{
    /// <summary>
    /// Represents a single cashier shift — from register open to close.
    /// </summary>
    public class CashSession
    {
        public int      Id                { get; set; }

        /// <summary>UTC timestamp when the shift was opened.</summary>
        public DateTime OpenedAt          { get; set; }

        /// <summary>UTC timestamp when the shift was closed. Null while still open.</summary>
        public DateTime? ClosedAt         { get; set; }

        /// <summary>Name or identifier of the user who opened the shift.</summary>
        public string   OpenedByUser      { get; set; } = string.Empty;

        /// <summary>Name or identifier of the user who closed the shift. Null while open.</summary>
        public string?  ClosedByUser      { get; set; }

        /// <summary>Cash physically counted and placed in the drawer at shift start.</summary>
        public decimal  OpeningBalance    { get; set; }

        /// <summary>
        /// System-calculated expected cash at close:
        /// OpeningBalance + sum of all cash sales during the session.
        /// </summary>
        public decimal  ExpectedCash      { get; set; }

        /// <summary>Cash physically counted in the drawer at shift end. Null while open.</summary>
        public decimal? ActualCash        { get; set; }

        /// <summary>
        /// ActualCash – ExpectedCash. Positive = over; Negative = short; Zero = balanced.
        /// Null while shift is still open.
        /// </summary>
        public decimal? DiscrepancyAmount { get; set; }

        /// <summary>"Open" or "Closed".</summary>
        public string   Status            { get; set; } = "Open";

        /// <summary>Optional freeform note recorded at closing (e.g. "Customer refund paid manually").</summary>
        public string?  Notes             { get; set; }
    }
}
