using System;

namespace EZPos.Models.Domain
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ChangeQty { get; set; }   // positive = stock in, negative = stock out / sale
        public string Reason { get; set; }   // "SALE", "ADJUSTMENT", "CORRECTION"
        public DateTime DateTime { get; set; }
    }
}
