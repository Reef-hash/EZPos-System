using System;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EZPos.DataAccess.Repositories;
using EZPos.UI.State;

namespace EZPos.UI.Pages
{
    public partial class SettingsPage : UserControl
    {
        private readonly PosStateStore _stateStore;

        public SettingsPage(PosStateStore stateStore)
        {
            InitializeComponent();
            _stateStore = stateStore;
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            StoreNameBox.Text     = ConfigHelper.Get("StoreName",      "EZPos Store");
            PrinterNameBox.Text   = ConfigHelper.Get("PrinterName",    "");
            TaxRateBox.Text       = ConfigHelper.Get("TaxRate",        "6");
            CurrencyBox.Text      = ConfigHelper.Get("Currency",       "RM");
            ReceiptFooterBox.Text = ConfigHelper.Get("ReceiptFooter",  "Thank you, come again!");

            PaymentCashHotkeyBox.Text    = ConfigHelper.Get("PaymentHotkeyCash", "F1");
            PaymentQrHotkeyBox.Text      = ConfigHelper.Get("PaymentHotkeyQr", "F2");
            PaymentCardHotkeyBox.Text    = ConfigHelper.Get("PaymentHotkeyCard", "F3");
            PaymentChequeHotkeyBox.Text  = ConfigHelper.Get("PaymentHotkeyCheque", "F4");
            ReceiptNewSaleHotkeyBox.Text = ConfigHelper.Get("ReceiptHotkeyNewSale", "PageUp");
            ReceiptPrintHotkeyBox.Text   = ConfigHelper.Get("ReceiptHotkeyPrint", "PageDown");

            // Select matching TaxMode item
            var savedMode = ConfigHelper.Get("TaxMode", "PerReceipt");
            foreach (ComboBoxItem item in TaxModeCombo.Items)
                if (item.Tag?.ToString() == savedMode)
                { TaxModeCombo.SelectedItem = item; break; }
            if (TaxModeCombo.SelectedItem is null) TaxModeCombo.SelectedIndex = 0;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate tax rate is a non-negative number
            if (!decimal.TryParse(TaxRateBox.Text.Trim(), out var tax) || tax < 0 || tax > 100)
            {
                MessageBox.Show("Tax rate must be a number between 0 and 100.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TaxRateBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(StoreNameBox.Text.Trim()))
            {
                MessageBox.Show("Store name cannot be empty.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StoreNameBox.Focus();
                return;
            }

            if (!TryGetValidatedHotkey(PaymentCashHotkeyBox, "Payment: Cash", out var paymentCashKey)) return;
            if (!TryGetValidatedHotkey(PaymentQrHotkeyBox, "Payment: QR", out var paymentQrKey)) return;
            if (!TryGetValidatedHotkey(PaymentCardHotkeyBox, "Payment: Card", out var paymentCardKey)) return;
            if (!TryGetValidatedHotkey(PaymentChequeHotkeyBox, "Payment: Cheque", out var paymentChequeKey)) return;
            if (!TryGetValidatedHotkey(ReceiptNewSaleHotkeyBox, "Receipt: New Sale", out var receiptNewSaleKey)) return;
            if (!TryGetValidatedHotkey(ReceiptPrintHotkeyBox, "Receipt: Print", out var receiptPrintKey)) return;

            var duplicate = new[]
            {
                (Name: "Payment: Cash", Key: paymentCashKey),
                (Name: "Payment: QR", Key: paymentQrKey),
                (Name: "Payment: Card", Key: paymentCardKey),
                (Name: "Payment: Cheque", Key: paymentChequeKey),
                (Name: "Receipt: New Sale", Key: receiptNewSaleKey),
                (Name: "Receipt: Print", Key: receiptPrintKey)
            }
            .GroupBy(x => x.Key)
            .FirstOrDefault(g => g.Count() > 1);

            if (duplicate is not null)
            {
                var labels = string.Join(", ", duplicate.Select(x => x.Name));
                MessageBox.Show($"Duplicate hotkey detected ({duplicate.Key}): {labels}\n\nPlease use unique keys.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConfigHelper.Set("StoreName",     StoreNameBox.Text.Trim());
            ConfigHelper.Set("PrinterName",   PrinterNameBox.Text.Trim());
            ConfigHelper.Set("TaxRate",       tax.ToString());
            ConfigHelper.Set("Currency",      CurrencyBox.Text.Trim());
            ConfigHelper.Set("ReceiptFooter", ReceiptFooterBox.Text.Trim());
            ConfigHelper.SetKey("PaymentHotkeyCash", paymentCashKey);
            ConfigHelper.SetKey("PaymentHotkeyQr", paymentQrKey);
            ConfigHelper.SetKey("PaymentHotkeyCard", paymentCardKey);
            ConfigHelper.SetKey("PaymentHotkeyCheque", paymentChequeKey);
            ConfigHelper.SetKey("ReceiptHotkeyNewSale", receiptNewSaleKey);
            ConfigHelper.SetKey("ReceiptHotkeyPrint", receiptPrintKey);

            var taxMode = (TaxModeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "PerReceipt";
            ConfigHelper.Set("TaxMode", taxMode);

            // Apply new tax config to the live state store immediately
            _stateStore.ReloadTaxConfig();

            MessageBox.Show("Settings saved successfully.", "Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static bool TryGetValidatedHotkey(TextBox input, string label, out Key key)
        {
            key = Key.None;
            var raw = input.Text.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                MessageBox.Show($"{label} hotkey cannot be empty.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                input.Focus();
                return false;
            }

            if (!TryParseHotkey(raw, out key) || key == Key.None)
            {
                MessageBox.Show($"Invalid key for {label}: '{raw}'.\n\nExample values: F1, F2, F3, F4, PageUp, PageDown, Insert, Home",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                input.Focus();
                input.SelectAll();
                return false;
            }

            input.Text = key.ToString();
            return true;
        }

        private static bool TryParseHotkey(string raw, out Key key)
        {
            key = Key.None;
            var normalized = raw.Trim();

            if (normalized.Equals("PgUp", StringComparison.OrdinalIgnoreCase))
            {
                key = Key.PageUp;
                return true;
            }

            if (normalized.Equals("PgDn", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("PgDown", StringComparison.OrdinalIgnoreCase))
            {
                key = Key.PageDown;
                return true;
            }

            if (Enum.TryParse<Key>(normalized, true, out var parsed))
            {
                key = parsed;
                return true;
            }

            var converter = new KeyConverter();
            var converted = converter.ConvertFromString(normalized);
            if (converted is Key typed)
            {
                key = typed;
                return true;
            }

            return false;
        }

        private void DetectPrinters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var queue = new LocalPrintServer();
                var printers = queue.GetPrintQueues()
                                    .Select(q => q.FullName)
                                    .OrderBy(n => n)
                                    .ToList();

                if (printers.Count == 0)
                {
                    MessageBox.Show("No printers found on this computer.", "No Printers",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show a picker dialog listing all available printers
                var picker = new Window
                {
                    Title               = "Select Printer",
                    Width               = 420,
                    Height              = 340,
                    ResizeMode          = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner               = Window.GetWindow(this),
                    Background          = (System.Windows.Media.Brush)FindResource("ContentBrush"),
                    FontFamily          = (System.Windows.Media.FontFamily)FindResource("AppFont")
                };

                var listBox = new ListBox
                {
                    Margin          = new Thickness(16, 16, 16, 8),
                    FontSize        = 13,
                    Background      = (System.Windows.Media.Brush)FindResource("CardBackgroundBrush"),
                    Foreground      = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                    BorderBrush     = (System.Windows.Media.Brush)FindResource("BorderBrush"),
                    BorderThickness = new Thickness(1)
                };
                foreach (var p in printers) listBox.Items.Add(p);
                if (!string.IsNullOrWhiteSpace(PrinterNameBox.Text))
                    listBox.SelectedItem = printers.FirstOrDefault(p => p == PrinterNameBox.Text);

                var selectBtn = new Button
                {
                    Content  = "Select",
                    Height   = 38,
                    Margin   = new Thickness(16, 0, 16, 16),
                    Style    = (Style)FindResource("PrimaryButtonStyle")
                };

                selectBtn.Click += (_, _) =>
                {
                    if (listBox.SelectedItem is string chosen)
                    {
                        PrinterNameBox.Text = chosen;
                        PrinterHintText.Text = $"Selected: {chosen}";
                    }
                    picker.DialogResult = true;
                };

                var panel = new StackPanel();
                panel.Children.Add(listBox);
                panel.Children.Add(selectBtn);
                picker.Content = panel;
                picker.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not list printers:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
