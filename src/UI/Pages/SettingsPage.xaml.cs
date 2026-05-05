using System;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
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

            ConfigHelper.Set("StoreName",     StoreNameBox.Text.Trim());
            ConfigHelper.Set("PrinterName",   PrinterNameBox.Text.Trim());
            ConfigHelper.Set("TaxRate",       tax.ToString());
            ConfigHelper.Set("Currency",      CurrencyBox.Text.Trim());
            ConfigHelper.Set("ReceiptFooter", ReceiptFooterBox.Text.Trim());

            var taxMode = (TaxModeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "PerReceipt";
            ConfigHelper.Set("TaxMode", taxMode);

            // Apply new tax config to the live state store immediately
            _stateStore.ReloadTaxConfig();

            MessageBox.Show("Settings saved successfully.", "Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
