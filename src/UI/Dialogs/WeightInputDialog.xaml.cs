using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace EZPos.UI.Dialogs
{
    /// <summary>
    /// Prompts the cashier to enter a weight in kg for a weight-based product.
    /// After ShowDialog() == true, read <see cref="WeightKg"/> for the confirmed value.
    /// </summary>
    public partial class WeightInputDialog : UserControl
    {
        private readonly decimal _pricePerKg;

        /// <summary>The confirmed weight in kg. Only valid after ShowDialog() == true.</summary>
        public decimal WeightKg { get; private set; }

        public WeightInputDialog(string productName, decimal pricePerKg)
        {
            InitializeComponent();
            _pricePerKg = pricePerKg;

            ProductNameText.Text = productName;
            PricePerKgText.Text  = $"RM {pricePerKg:F2} / kg";

            Loaded += (_, _) =>
            {
                WeightBox.Focus();
                WeightBox.SelectAll();
            };

            WeightBox.TextChanged += (_, _) => UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (decimal.TryParse(WeightBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var w) && w > 0)
            {
                TotalPreviewText.Text = $"Total: RM {w * _pricePerKg:F2}";
                WeightError.Visibility = Visibility.Collapsed;
            }
            else
            {
                TotalPreviewText.Text = string.Empty;
            }
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            DialogHost.CloseDialogCommand.Execute(WeightKg, this);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogHost.CloseDialogCommand.Execute(null, this);
        }

        private void WeightBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!Validate()) return;
                DialogHost.CloseDialogCommand.Execute(WeightKg, this);
                e.Handled = true;
            }
        }

        private bool Validate()
        {
            var text = WeightBox.Text.Trim();
            if (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var w) || w <= 0)
            {
                WeightError.Visibility = Visibility.Visible;
                WeightBox.Focus();
                WeightBox.SelectAll();
                return false;
            }

            WeightError.Visibility = Visibility.Collapsed;
            WeightKg = w;
            return true;
        }
    }
}
