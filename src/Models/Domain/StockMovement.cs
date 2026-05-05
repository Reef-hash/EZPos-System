using System;

namespace EZPos.Models.Domain
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        /// <summary>Supports decimal for weight-based products. Positive = stock in, negative = stock out / sale.</summary>
        public decimal ChangeQty { get; set; }
        public string Reason { get; set; }   // "SALE", "ADJUSTMENT", "CORRECTION"
        public DateTime DateTime { get; set; }
    }
}
