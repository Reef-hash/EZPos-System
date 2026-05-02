using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using EZPos.Business.Services;
using EZPos.Models.Domain;
using EZPos.UI.Dialogs;
using EZPos.UI.State;

namespace EZPos.UI.Pages
{
    public sealed class ProductStatusBrushConverter : IValueConverter
    {
        public static ProductStatusBrushConverter Instance { get; } = new();

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

    public partial class ProductsPage : UserControl
    {
        private readonly PosStateStore stateStore;
        private readonly ProductService productService;
        private ICollectionView? productsView;
        private bool isInitialized;

        public ProductsPage(PosStateStore stateStore, ProductService productService)
        {
            InitializeComponent();

            this.stateStore     = stateStore;
            this.productService = productService;
            // Independent view per page — never share the default view across pages
            productsView = new ListCollectionView(this.stateStore.Products);
            this.stateStore.PropertyChanged += StateStore_PropertyChanged;

            Loaded += ProductsPage_Loaded;
        }

        private void ProductsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInitialized)
            {
                return;
            }

            // Apply filter here — controls are fully ready and page is in visual tree
            productsView.Filter = ProductFilter;

            ProductsGrid.ItemsSource = productsView;
            if (FilterCombo is not null)
            {
                FilterCombo.SelectedIndex = 0;
            }

            UpdateCounters();
            isInitialized = true;
        }

        private void StateStore_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PosStateStore.CartItemCount))
            {
                UpdateCounters();
            }
        }

        private bool ProductFilter(object obj)
        {
            if (obj is not ProductRecord product)
            {
                return false;
            }

            var search = SearchBox?.Text?.Trim() ?? string.Empty;
            var statusFilter = (FilterCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Products";

            var matchesSearch = string.IsNullOrWhiteSpace(search)
                || product.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || product.Barcode.Contains(search, StringComparison.OrdinalIgnoreCase);

            var matchesFilter = statusFilter switch
            {
                "In Stock" => product.StockStatus == "In Stock",
                "Low Stock" => product.StockStatus == "Low Stock",
                "Out of Stock" => product.StockStatus == "Out of Stock",
                _ => true
            };

            return matchesSearch && matchesFilter;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialized || productsView is null)
            {
                return;
            }

            productsView.Refresh();
            UpdateCounters();
        }

        private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || productsView is null)
            {
                return;
            }

            productsView.Refresh();
            UpdateCounters();
        }

        private void RefreshProducts_Click(object sender, RoutedEventArgs e)
        {
            if (productsView is null)
            {
                return;
            }

            SearchBox.Clear();
            FilterCombo.SelectedIndex = 0;
            productsView.Refresh();
            UpdateCounters();
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductDialog(productService) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                productsView?.Refresh();
                UpdateCounters();
            }
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is not ProductRecord selected)
            {
                MessageBox.Show("Select a product to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Map ProductRecord → Product domain model for the dialog
            var domainProduct = new Product
            {
                Id           = selected.Id,
                Barcode      = selected.Barcode,
                Name         = selected.Name,
                Category     = selected.Category,
                Price        = selected.Price,
                Stock        = selected.Stock,
                ReorderLevel = selected.ReorderLevel,
                MaxStock     = selected.MaxStock,
                LastUpdated  = selected.LastUpdated
            };

            var dialog = new ProductDialog(productService, domainProduct) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                productsView?.Refresh();
                UpdateCounters();
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is not ProductRecord selected)
            {
                MessageBox.Show("Select a product to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Delete '{selected.Name}'? This cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                productService.Delete(selected.Id);
                productsView?.Refresh();
                UpdateCounters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete product:\n{ex.Message}",
                    "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: ProductRecord product })
            {
                return;
            }

            MessageBox.Show(
                $"Product Details\n\nName: {product.Name}\nBarcode: {product.Barcode}\nCategory: {product.Category}\nPrice: RM {product.Price:F2}\nStock: {product.Stock}",
                "Product Details",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ProductsGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EditProduct_Click(sender, e);
        }

        private void UpdateCounters()
        {
            if (TotalProductsText is null || CartCounterText is null || ProductsGrid is null)
            {
                return;
            }

            TotalProductsText.Text = $"{ProductsGrid.Items.Count} products";
            CartCounterText.Text = $"Cart: {stateStore.CartItemCount} items";
        }
    }
}
