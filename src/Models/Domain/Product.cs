using System;

namespace EZPos.Models.Domain
{
    public class Product
    {
        public int Id { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; } = "General";
        public int ReorderLevel { get; set; } = 5;
        public int MaxStock { get; set; } = 100;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}