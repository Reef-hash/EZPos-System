namespace EZPos.Models.Domain
{
    /// <summary>Barcode symbology used to render and print a product's barcode.</summary>
    public enum BarcodeFormat
    {
        /// <summary>Default. Any alphanumeric string, any length. Recommended for all internal products.</summary>
        Code128,
        /// <summary>Older scanners and industrial labels. Uppercase alphanumeric + a few symbols.</summary>
        Code39,
        /// <summary>Standard retail barcode. Exactly 12 digits + check digit (auto-calculated).</summary>
        EAN13,
        /// <summary>URL or structured data. Phase 3 — only useful with a web product catalogue.</summary>
        QR
    }
}
