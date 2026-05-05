using System;
using System.Windows;
using EZPos.Business.Services;
using EZPos.DataAccess.Repositories;
using EZPos.Utilities.Helpers;

namespace EZPos.UI.Dialogs
{
    /// <summary>
    /// On-screen receipt shown after a successful checkout.
    /// Populated entirely from the SaleResult returned by SaleService.ProcessSale().
    /// </summary>
    public partial class ReceiptDialog : Window
    {
        private readonly SaleResult _result;

        public ReceiptDialog(SaleResult result)
        {
            InitializeComponent();
            _result = result;

            SaleIdText.Text       = $"Sale #{result.SaleId:D4}";
            DateTimeText.Text     = result.DateTime.ToString("dd MMM yyyy  hh:mm tt");
            SubtotalText.Text     = $"RM {result.Subtotal:F2}";
            TaxText.Text          = $"RM {result.Tax:F2}";
            TotalText.Text        = $"RM {result.Total:F2}";
            PaymentMethodText.Text = result.PaymentMethod;
            TenderedText.Text     = $"RM {result.Tendered:F2}";
            ChangeText.Text       = $"RM {result.Change:F2}";

            // Build receipt line items
            var lines = new System.Collections.Generic.List<ReceiptLine>();
            foreach (var line in result.Lines)
            {
                lines.Add(new ReceiptLine
                {
                    ProductName = line.ProductName,
                    Quantity    = line.Quantity,
                    LineTotal   = line.UnitPrice * line.Quantity
                });
            }
            LineItemsControl.ItemsSource = lines;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var printerName = ConfigHelper.Get("PrinterName");
            if (string.IsNullOrWhiteSpace(printerName))
            {
                MessageBox.Show(
                    "No printer configured.\n\nSet 'PrinterName' in Config\\config.ini to the exact Windows printer name.",
                    "Printer Not Configured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var storeName = ConfigHelper.Get("StoreName", "EZPos");
                var bytes     = EscPosDocument.Build(_result, storeName);
                RawPrinterHelper.SendBytes(printerName, bytes);
                MessageBox.Show("Receipt sent to printer.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed:\n{ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class ReceiptLine
        {
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal LineTotal { get; set; }
        }
    }
}
