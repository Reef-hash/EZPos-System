using System;

namespace EZPos.Models.Domain
{
    /// <summary>How a product is sold and how its stock is tracked.</summary>
    public enum UnitType
    {
        /// <summary>Sold per individual piece. Stock is whole units.</summary>
        Unit,
        /// <summary>Sold as a pack of multiple units. Stock deduction hits the parent unit product.</summary>
        Pack,
        /// <summary>Sold by weight (kg). Stock and cart quantity support decimals.</summary>
        Weight
    }

    public class Product
    {
        public int Id { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal Stock { get; set; }
        public string Category { get; set; } = "General";
        public int ReorderLevel { get; set; } = 5;
        public int MaxStock { get; set; } = 100;
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>Selling type: Unit, Pack, or Weight.</summary>
        public UnitType UnitType { get; set; } = UnitType.Unit;

        /// <summary>For Pack: how many base units are in one pack (e.g. 5 means 1 pack = 5 units).</summary>
        public decimal ConversionRate { get; set; } = 1m;

        /// <summary>For Pack: the Id of the base Unit product whose stock is deducted on sale.</summary>
        public int? ParentProductId { get; set; }
    }
}