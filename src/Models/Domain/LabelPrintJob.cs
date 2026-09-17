namespace EZPos.Models.Domain
{
    /// <summary>One product queued for label printing, with the quantity of labels to produce.</summary>
    public class LabelPrintJob
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public BarcodeFormat Format { get; set; } = BarcodeFormat.Code128;
        public int Quantity { get; set; } = 1;
    }
}
