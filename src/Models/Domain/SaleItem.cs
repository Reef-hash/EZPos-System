namespace EZPos.Models.Domain
{
    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        /// <summary>Supports decimal for weight-based products (e.g. 1.25 kg).</summary>
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }
}