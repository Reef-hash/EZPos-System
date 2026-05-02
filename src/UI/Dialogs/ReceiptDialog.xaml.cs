using System.Windows;
using EZPos.Business.Services;

namespace EZPos.UI.Dialogs
{
    /// <summary>
    /// On-screen receipt shown after a successful checkout.
    /// Populated entirely from the SaleResult returned by SaleService.ProcessSale().
    /// </summary>
    public partial class ReceiptDialog : Window
    {
        public ReceiptDialog(SaleResult result)
        {
            InitializeComponent();

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

        private class ReceiptLine
        {
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal LineTotal { get; set; }
        }
    }
}
