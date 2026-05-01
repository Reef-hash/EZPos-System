using System;

namespace EZPos.Models.Domain
{
    public class Sale
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }
}