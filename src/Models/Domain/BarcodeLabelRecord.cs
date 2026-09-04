using System;

namespace EZPos.Models.Domain
{
    /// <summary>One historical print-job entry: which product, when, how many, with which template/format.</summary>
    public class BarcodeLabelRecord
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public DateTime PrintedAt { get; set; } = DateTime.Now;
        public int Quantity { get; set; } = 1;
        public string TemplateName { get; set; } = string.Empty;
        public BarcodeFormat BarcodeFormat { get; set; } = BarcodeFormat.Code128;
    }
}
