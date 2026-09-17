using System;

namespace EZPos.Models.Domain
{
    /// <summary>A reusable label layout definition: dimensions, field toggles, and font sizes.</summary>
    public class LabelTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public double LabelWidthMm { get; set; }
        public double LabelHeightMm { get; set; }
        public int LabelsPerRow { get; set; } = 1;
        public int LabelsPerColumn { get; set; } = 1;
        public bool ShowBarcode { get; set; } = true;
        public bool ShowName { get; set; } = true;
        public bool ShowPrice { get; set; } = true;
        public bool ShowCategory { get; set; } = false;
        public bool ShowStoreName { get; set; } = false;
        public string? CustomText { get; set; }
        /// <summary>Fraction of the label height reserved for the barcode image (e.g. 0.50 = 50%).</summary>
        public double BarcodeHeightPct { get; set; } = 0.50;
        public double FontSizeName { get; set; } = 8;
        public double FontSizePrice { get; set; } = 10;
        public bool IsDefault { get; set; } = false;

        public override string ToString() => Name;
    }
}
