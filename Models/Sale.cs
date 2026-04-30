using System;

namespace Models
{
    public class Sale
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public decimal TotalAmount { get; set; }
    }
}