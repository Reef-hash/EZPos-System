using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using EZPos.UI.State;

namespace EZPos.UI.Pages
{
    public sealed class StockStatusBrushConverter : IValueConverter
    {
        public static StockStatusBrushConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string status)
            {
                return Brushes.Gray;
            }

            return status switch
            {
                "Out of Stock" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEF4444")),
                "Low Stock" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF59E0B")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF10B981"))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class StockPage : UserControl
    {
        private readonly PosStateStore stateStore;
        private ICollectionView? stockView;
        private bool isInitialized;

        public StockPage(PosStateStore stateStore)
        {
            InitializeComponent();

            this.stateStore = stateStore;
            // Independent view per page — never share the default view across pages
            stockView = new ListCollectionView(this.stateStore.Products);

            Loaded += StockPage_Loaded;
        }

        private void StockPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInitialized)
            {
                return;
            }

            // Apply filter here — controls are fully ready and page is in visual tree
            stockView.Filter = StockFilter;

            InventoryGrid.ItemsSource = stockView;

            if (StockFilterCombo is not null)
            {
                StockFilterCombo.SelectedIndex = 0;
            }

            if (CategoryFilterCombo is not null)
            {
                CategoryFilterCombo.SelectedIndex = 0;
            }

            UpdateSummary();
            isInitialized = true;
        }

        private bool StockFilter(object obj)
        {
            if (obj is not ProductRecord product)
            {
                return false;
            }

            var search = StockSearchBox?.Text?.Trim() ?? string.Empty;
            var stockStatus = (StockFilterCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Stock Levels";
            var category = (CategoryFilterCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Categories";

            var matchesSearch = string.IsNullOrWhiteSpace(search)
                || product.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || product.Barcode.Contains(search, StringComparison.OrdinalIgnoreCase);

            var matchesStock = stockStatus switch
            {
                "In Stock" => product.StockStatus == "In Stock",
                "Low Stock" => product.StockStatus == "Low Stock",
                "Out of Stock" => product.StockStatus == "Out of Stock",
                _ => true
            };

            var matchesCategory = category == "All Categories"
                || string.Equals(product.Category, category, StringComparison.OrdinalIgnoreCase);

            return matchesSearch && matchesStock && matchesCategory;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialized || stockView is null)
            {
                return;
            }

            stockView.Refresh();
            UpdateSummary();
        }

        private void StockFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || stockView is null)
            {
                return;
            }

            stockView.Refresh();
            UpdateSummary();
        }

        private void CategoryFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || stockView is null)
            {
                return;
            }

            stockView.Refresh();
            UpdateSummary();
        }

        private void StockIn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Stock-in flow can be connected to inventory transactions.", "Stock", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StockOut_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Stock-out flow can be connected to inventory transactions.", "Stock", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (stockView is null)
            {
                return;
            }

            StockSearchBox.Clear();
            StockFilterCombo.SelectedIndex = 0;
            CategoryFilterCombo.SelectedIndex = 0;
            stockView.Refresh();
            UpdateSummary();
        }

        private void EditStock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: ProductRecord item })
            {
                return;
            }

            MessageBox.Show(
                $"Edit Stock\n\nProduct: {item.Name}\nCurrent Stock: {item.Stock}\nReorder Level: {item.ReorderLevel}",
                "Stock",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void UpdateSummary()
        {
            if (InStockValue is null || LowStockValue is null || OutOfStockValue is null || TotalUnitsValue is null)
            {
                return;
            }

            var inStock = 0;
            var lowStock = 0;
            var outOfStock = 0;
            var totalUnits = 0;

            foreach (var item in stateStore.Products)
            {
                totalUnits += item.Stock;

                if (item.StockStatus == "Out of Stock")
                {
                    outOfStock++;
                }
                else if (item.StockStatus == "Low Stock")
                {
                    lowStock++;
                }
                else
                {
                    inStock++;
                }
            }

            InStockValue.Text = inStock.ToString();
            LowStockValue.Text = lowStock.ToString();
            OutOfStockValue.Text = outOfStock.ToString();
            TotalUnitsValue.Text = totalUnits.ToString();
        }
    }
}
