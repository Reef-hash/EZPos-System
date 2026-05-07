using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private readonly Dictionary<Key, Action> _shortcuts = new();

        public ReceiptDialog(SaleResult result)
        {
            InitializeComponent();
            _result = result;

            PreviewKeyDown += ReceiptDialog_PreviewKeyDown;
            Loaded += ReceiptDialog_Loaded;

            RegisterShortcut("ReceiptHotkeyNewSale", "PageUp", TriggerNewSale);
            RegisterShortcut("ReceiptHotkeyPrint", "PageDown", TriggerPrint);

            SaleIdText.Text        = $"Sale #{result.SaleId:D4}";
            DateTimeText.Text      = result.DateTime.ToString("dd MMM yyyy  hh:mm tt");
            SubtotalText.Text      = $"RM {result.Subtotal:F2}";

            // Dynamic tax label (reflects configured rate)
            var taxRate = ConfigHelper.Get("TaxRate", "6");
            TaxLabel.Text = $"Tax ({taxRate}%)";
            TaxText.Text  = $"RM {result.Tax:F2}";

            // Cash rounding line
            if (result.RoundingAdj != 0)
            {
                RoundingRow.Visibility  = Visibility.Visible;
                RoundingAdjText.Text    = $"{(result.RoundingAdj > 0 ? "+" : "")}RM {result.RoundingAdj:F2}";
            }

            TotalText.Text         = $"RM {result.Total:F2}";
            PaymentMethodText.Text = result.PaymentMethod;
            TenderedText.Text      = $"RM {result.Tendered:F2}";
            ChangeText.Text        = $"RM {result.Change:F2}";

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

        private void ReceiptDialog_Loaded(object sender, RoutedEventArgs e)
        {
            NewSaleBtn.Focus();

            if (ConfigHelper.Get("AutoPrint", "false") == "true")
                TriggerPrint();
        }

        private void RegisterShortcut(string configKey, string defaultKey, Action action)
        {
            var key = ConfigHelper.GetKey(configKey, defaultKey);
            if (key != Key.None)
                _shortcuts[key] = action;
        }

        private void ReceiptDialog_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_shortcuts.TryGetValue(e.Key, out var action))
                return;

            action();
            e.Handled = true;
        }

        private void TriggerNewSale()
        {
            DialogResult = true;
        }

        private void TriggerPrint()
        {
            Print_Click(this, new RoutedEventArgs());
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            TriggerNewSale();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var printerName = ConfigHelper.Get("PrinterName");
            if (string.IsNullOrWhiteSpace(printerName))
            {
                MessageBox.Show(
                    "No printer configured.\n\nSet 'PrinterName' in Settings to the exact Windows printer name.",
                    "Printer Not Configured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // First try ESC/POS raw bytes (works on thermal printers)
            bool rawSucceeded = false;
            try
            {
                var storeName = ConfigHelper.Get("StoreName", "EZPos");
                var bytes     = EscPosDocument.Build(_result, storeName);
                RawPrinterHelper.SendBytes(printerName, bytes);
                rawSucceeded = true;
                MessageBox.Show("Receipt sent to printer.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                // Raw printing failed — printer is likely a PDF / inkjet / laser driver
            }

            // Fallback: render the on-screen receipt using WPF PrintVisual
            // (works with PDF printers, network printers, laser/inkjet, etc.)
            if (!rawSucceeded)
            {
                PrintVisualReceipt(printerName);
            }
        }

        /// <summary>
        /// Prints the visible receipt panel using WPF's PrintDialog / PrintVisual API.
        /// Works with any Windows printer including PDF drivers.
        /// </summary>
        private void PrintVisualReceipt(string printerName)
        {
            try
            {
                var pd = new System.Windows.Controls.PrintDialog();

                // Pre-select the configured printer so the dialog opens on the right one
                // (PrintDialog doesn't expose a direct printer-name setter without PrintQueue,
                //  so we open it for the user to confirm)
                if (pd.ShowDialog() != true)
                    return;

                // Print just the receipt content panel (rows 0–6 = everything above the buttons)
                // We wrap it in a white-background border for clean PDF output
                var printPanel = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    Padding    = new Thickness(32, 24, 32, 24),
                    Width      = 400
                };

                // Clone the receipt content into a printable FixedDocument page
                var visual = ReceiptContentPanel;
                visual.Measure(new Size(400, double.PositiveInfinity));
                visual.Arrange(new Rect(new Size(400, visual.DesiredSize.Height)));

                pd.PrintVisual(visual, $"Receipt - Sale #{_result.SaleId:D4}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed:\n{ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class ReceiptLine
        {
            public string ProductName { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal LineTotal { get; set; }
        }
    }
}
