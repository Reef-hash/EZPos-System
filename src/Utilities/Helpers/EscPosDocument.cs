using System;
using System.Collections.Generic;
using System.Text;
using EZPos.Business.Services;

namespace EZPos.Utilities.Helpers
{
    /// <summary>
    /// Builds an ESC/POS byte sequence for a standard 80 mm (42-char) thermal receipt.
    /// Usage: Build() returns a byte[] ready to be sent via RawPrinterHelper.SendBytes().
    /// </summary>
    public sealed class EscPosDocument
    {
        // ── ESC/POS constants ─────────────────────────────────────────────
        private static readonly byte[] ESC_INIT       = { 0x1B, 0x40 };        // Initialize
        private static readonly byte[] ESC_ALIGN_LEFT  = { 0x1B, 0x61, 0x00 }; // Left align
        private static readonly byte[] ESC_ALIGN_CENTER= { 0x1B, 0x61, 0x01 }; // Center align
        private static readonly byte[] ESC_ALIGN_RIGHT = { 0x1B, 0x61, 0x02 }; // Right align
        private static readonly byte[] ESC_BOLD_ON    = { 0x1B, 0x45, 0x01 };  // Bold on
        private static readonly byte[] ESC_BOLD_OFF   = { 0x1B, 0x45, 0x00 };  // Bold off
        private static readonly byte[] ESC_FONT_TALL  = { 0x1D, 0x21, 0x01 };  // Double height
        private static readonly byte[] ESC_FONT_NORMAL= { 0x1D, 0x21, 0x00 };  // Normal size
        private static readonly byte[] LF             = { 0x0A };               // Line feed
        private static readonly byte[] CUT            = { 0x1D, 0x56, 0x41, 0x03 }; // Partial cut

        private const int LINE_WIDTH = 42; // characters per line for 80 mm paper
        private readonly List<byte> _buf = new();
        private readonly Encoding _enc = Encoding.GetEncoding(437); // PC437 (OEM US) — standard ESC/POS

        // ── Fluent builder ────────────────────────────────────────────────

        private EscPosDocument Append(byte[] bytes)        { _buf.AddRange(bytes); return this; }
        private EscPosDocument AppendText(string text)     { _buf.AddRange(_enc.GetBytes(text)); return this; }
        private EscPosDocument NewLine()                   { _buf.AddRange(LF); return this; }

        private EscPosDocument Center(string text)
        {
            Append(ESC_ALIGN_CENTER);
            AppendText(text);
            NewLine();
            Append(ESC_ALIGN_LEFT);
            return this;
        }

        private EscPosDocument CenterBold(string text)
        {
            Append(ESC_ALIGN_CENTER);
            Append(ESC_BOLD_ON);
            AppendText(text);
            Append(ESC_BOLD_OFF);
            NewLine();
            Append(ESC_ALIGN_LEFT);
            return this;
        }

        private EscPosDocument CenterTall(string text)
        {
            Append(ESC_ALIGN_CENTER);
            Append(ESC_FONT_TALL);
            AppendText(text);
            Append(ESC_FONT_NORMAL);
            NewLine();
            Append(ESC_ALIGN_LEFT);
            return this;
        }

        private EscPosDocument Divider()
        {
            AppendText(new string('-', LINE_WIDTH));
            NewLine();
            return this;
        }

        private EscPosDocument Row(string left, string right, bool bold = false)
        {
            if (bold) Append(ESC_BOLD_ON);
            int rightLen   = right.Length;
            int leftMax    = LINE_WIDTH - rightLen - 1;
            string leftStr = left.Length > leftMax ? left[..leftMax] : left.PadRight(leftMax);
            AppendText(leftStr + " " + right);
            NewLine();
            if (bold) Append(ESC_BOLD_OFF);
            return this;
        }

        // ── Public factory ────────────────────────────────────────────────

        /// <summary>
        /// Generates receipt bytes from a completed <see cref="SaleResult"/>.
        /// </summary>
        public static byte[] Build(SaleResult result, string storeName)
        {
            var taxRate    = EZPos.DataAccess.Repositories.ConfigHelper.Get("TaxRate", "6");
            var taxMode    = EZPos.DataAccess.Repositories.ConfigHelper.Get("TaxMode", "PerReceipt");
            var taxLabel   = taxMode == "Fake"
                ? $"Tax ({taxRate}%) *display"
                : $"Tax ({taxRate}%)";
            var doc = new EscPosDocument();
            doc.Append(ESC_INIT);
            doc.NewLine();

            // Header
            doc.CenterTall(storeName);
            doc.Center($"Sale #{result.SaleId:D4}");
            doc.Center(result.DateTime.ToString("dd MMM yyyy  hh:mm tt"));
            doc.Divider();

            // Line items
            foreach (var line in result.Lines)
            {
                string itemTotal = $"RM {line.UnitPrice * line.Quantity:F2}";
                doc.Row(line.ProductName, itemTotal);
                // sub-line: qty × unit price
                string subLine = $"  {line.Quantity} x RM {line.UnitPrice:F2}";
                doc.AppendText(subLine).NewLine();
            }

            doc.Divider();

            // Totals
            doc.Row("Subtotal", $"RM {result.Subtotal:F2}");
            doc.Row(taxLabel, $"RM {result.Tax:F2}");
            doc.Divider();
            // In Fake mode, Total == Subtotal; the label makes it clear tax is display-only
            doc.Row("TOTAL", $"RM {result.Total:F2}", bold: true);
            doc.Divider();

            // Payment
            doc.Row("Payment", result.PaymentMethod);
            if (result.PaymentMethod == "Cash")
            {
                doc.Row("Tendered", $"RM {result.Tendered:F2}");
                doc.Row("Change",   $"RM {result.Change:F2}", bold: true);
            }

            doc.NewLine();
            var footer = EZPos.DataAccess.Repositories.ConfigHelper.Get("ReceiptFooter", "Thank you, come again!");
            doc.Center(footer);
            doc.NewLine();
            doc.NewLine();
            doc.NewLine();

            doc.Append(CUT);

            return doc._buf.ToArray();
        }
    }
}
