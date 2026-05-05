using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
        private readonly CategoryService categoryService;
        private ICollectionView? productsView;
        private bool isInitialized;

        // Barcode scanner detection — same 150 ms threshold as SalesPage
        private DateTime _firstKeyTime = DateTime.MinValue;

        public ProductsPage(PosStateStore stateStore, ProductService productService, CategoryService categoryService)
        {
            InitializeComponent();

            this.stateStore      = stateStore;
            this.productService  = productService;
            this.categoryService = categoryService;
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

            if (_firstKeyTime == DateTime.MinValue)
                _firstKeyTime = DateTime.Now;

            productsView.Refresh();
            UpdateCounters();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !isInitialized)
                return;

            var input = SearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
                return;

            bool isScan = _firstKeyTime != DateTime.MinValue
                          && (DateTime.Now - _firstKeyTime).TotalMilliseconds < 150;
            _firstKeyTime = DateTime.MinValue;

            if (!isScan)
            {
                // Manual Enter — just refresh filter
                productsView?.Refresh();
                e.Handled = true;
                return;
            }

            // Scanner path — look up by exact barcode
            var existing = System.Linq.Enumerable.FirstOrDefault(
                stateStore.Products, p => string.Equals(p.Barcode, input, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                // Product already registered — highlight it in the grid
                SearchBox.Text = string.Empty;
                productsView?.Refresh();
                // Show all and select the matched row
                FilterCombo.SelectedIndex = 0;
                SearchBox.Text = existing.Barcode;
                productsView?.Refresh();
                ProductsGrid.SelectedItem = existing;
                ProductsGrid.ScrollIntoView(existing);
                MessageBox.Show($"Barcode already registered:\n{existing.Name}",
                    "Product Found", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // New barcode — open Add Product dialog pre-filled
                SearchBox.Text = string.Empty;
                productsView?.Refresh();
                var dialog = new ProductDialog(productService, categoryService, input) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true)
                {
                    productsView?.Refresh();
                    UpdateCounters();
                }
            }

            e.Handled = true;
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

        private void ManageCategories_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CategoryManagementDialog(categoryService) { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();
            // After managing categories, reload products view (category names may have changed)
            productsView?.Refresh();
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductDialog(productService, categoryService) { Owner = Window.GetWindow(this) };
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
                Id              = selected.Id,
                Barcode         = selected.Barcode,
                Name            = selected.Name,
                Category        = selected.Category,
                Price           = selected.Price,
                Stock           = selected.Stock,
                ReorderLevel    = selected.ReorderLevel,
                MaxStock        = selected.MaxStock,
                LastUpdated     = selected.LastUpdated,
                UnitType        = selected.UnitType,
                ConversionRate  = selected.ConversionRate,
                ParentProductId = selected.ParentProductId
            };

            var dialog = new ProductDialog(productService, categoryService, domainProduct) { Owner = Window.GetWindow(this) };
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
