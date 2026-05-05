using System;
using System.Windows;
using System.Windows.Controls;
using EZPos.Business.Services;
using EZPos.UI.State;

namespace EZPos.UI.Dialogs
{
    /// <summary>
    /// Adjust stock for a single product.
    /// Opens over the selected ProductRecord. Calls StockService.AdjustStock() on save.
    /// </summary>
    public partial class StockAdjustDialog : Window
    {
        private readonly StockService _stockService;
        private readonly ProductRecord _product;

        public StockAdjustDialog(StockService stockService, ProductRecord product)
        {
            InitializeComponent();
            _stockService = stockService;
            _product      = product;

            // Populate product info card
            ProductNameText.Text    = product.Name;
            ProductBarcodeText.Text = product.Barcode;
            CurrentStockText.Text   = product.Stock.ToString();

            // Wire quantity box to update preview
            QuantityBox.TextChanged += UpdatePreview;
            TypeCombo.SelectionChanged += (_, _) => UpdatePreview(null, null);

            ReasonCombo.SelectedIndex = 0;
            UpdatePreview(null, null);
        }

        // ── Live stock preview ─────────────────────────────────────────────────
        private void UpdatePreview(object? sender, EventArgs? e)
        {
            if (NewStockPreview == null) return;

            int qty = int.TryParse(QuantityBox?.Text?.Trim(), out var q) ? q : 0;
            int change = GetSignedChange(qty);
            decimal newStock = Math.Max(0, _product.Stock + change);
            NewStockPreview.Text = newStock.ToString();

            if (QtyLabel != null)
            {
                var tag = (TypeCombo?.SelectedItem as ComboBoxItem)?.Tag as string ?? "IN";
                QtyLabel.Text = tag switch
                {
                    "OUT" => "Quantity to Remove *",
                    "ADJ" => "Set New Quantity *",
                    _     => "Quantity to Add *"
                };
            }
        }

        private int GetSignedChange(int qty)
        {
            var tag = (TypeCombo?.SelectedItem as ComboBoxItem)?.Tag as string ?? "IN";
            return tag switch
            {
                "OUT" => -qty,
                "ADJ" => qty - (int)_product.Stock, // absolute → relative change
                _     => qty                    // IN
            };
        }

        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview(null, null);
        }

        // ── Save ──────────────────────────────────────────────────────────────
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate())
                return;

            int qty    = int.Parse(QuantityBox.Text.Trim());
            int change = GetSignedChange(qty);

            string reason = ReasonCombo.SelectedItem switch
            {
                ComboBoxItem item => item.Content?.ToString(),
                string value => value,
                _ => ReasonCombo.Text
            };

            reason = (reason ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(reason))
                reason = "ADJUSTMENT";

            var tag = (TypeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "IN";
            string dbReason = tag switch
            {
                "OUT" => $"OUT: {reason}",
                "ADJ" => $"ADJ: {reason}",
                _     => $"IN: {reason}"
            };

            try
            {
                _stockService.AdjustStock(_product.Id, change, dbReason);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to adjust stock:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // ── Validation ────────────────────────────────────────────────────────
        private bool Validate()
        {
            bool valid = true;

            if (!int.TryParse(QuantityBox.Text.Trim(), out var qty) || qty <= 0)
            {
                QuantityError.Visibility = Visibility.Visible;
                valid = false;
            }
            else
            {
                QuantityError.Visibility = Visibility.Collapsed;
            }

            string reasonText = ReasonCombo.SelectedItem switch
            {
                ComboBoxItem item => item.Content?.ToString(),
                string value => value,
                _ => ReasonCombo.Text
            };

            reasonText = (reasonText ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(reasonText))
            {
                ReasonError.Visibility = Visibility.Visible;
                valid = false;
            }
            else
            {
                ReasonError.Visibility = Visibility.Collapsed;
            }

            return valid;
        }
    }
}
