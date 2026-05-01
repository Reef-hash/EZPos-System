using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;

namespace EZPos.UI.Pages
{
    // Stock Status Converter
    public class StockStatusConverter : IValueConverter
    {
        public static readonly StockStatusConverter Instance = new StockStatusConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stock)
            {
                if (stock <= 0)
                    return "Out of Stock";
                else if (stock <= 10)
                    return "Low Stock";
                else
                    return "In Stock";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Stock Status Color Converter
    public class StockStatusColorConverter : IValueConverter
    {
        public static readonly StockStatusColorConverter Instance = new StockStatusColorConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stock)
            {
                if (stock <= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEF4444")); // Red
                else if (stock <= 10)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF59E0B")); // Amber
                else
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF10B981")); // Green
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF64748B")); // Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class ProductsPage : UserControl
    {
        private ObservableCollection<ProductItem> allProducts;
        private ObservableCollection<ProductItem> filteredProducts;

        public class ProductItem
        {
            public int Id { get; set; }
            public string Barcode { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public bool IsSelected { get; set; }
        }

        public ProductsPage()
        {
            InitializeComponent();
            InitializeData();
            BindData();
        }

        private void InitializeData()
        {
            allProducts = new ObservableCollection<ProductItem>
            {
                new ProductItem { Id = 1, Barcode = "001001", Name = "Espresso Coffee", Price = 5.50m, Stock = 45, IsSelected = false },
                new ProductItem { Id = 2, Barcode = "001002", Name = "Black Tea", Price = 4.00m, Stock = 8, IsSelected = false },
                new ProductItem { Id = 3, Barcode = "001003", Name = "Orange Juice", Price = 6.00m, Stock = 0, IsSelected = false },
                new ProductItem { Id = 4, Barcode = "001004", Name = "Mineral Water", Price = 2.50m, Stock = 120, IsSelected = false },
                new ProductItem { Id = 5, Barcode = "002001", Name = "Cheese Sandwich", Price = 8.50m, Stock = 22, IsSelected = false },
                new ProductItem { Id = 6, Barcode = "002002", Name = "Chocolate Cake", Price = 7.00m, Stock = 15, IsSelected = false },
                new ProductItem { Id = 7, Barcode = "002003", Name = "Cookie Mix", Price = 3.50m, Stock = 5, IsSelected = false },
                new ProductItem { Id = 8, Barcode = "002004", Name = "Donut Box", Price = 4.50m, Stock = 38, IsSelected = false },
                new ProductItem { Id = 9, Barcode = "003001", Name = "Muffin Blueberry", Price = 6.50m, Stock = 12, IsSelected = false },
                new ProductItem { Id = 10, Barcode = "003002", Name = "Croissant", Price = 5.00m, Stock = 0, IsSelected = false },
                new ProductItem { Id = 11, Barcode = "003003", Name = "Brownie", Price = 4.00m, Stock = 28, IsSelected = false },
                new ProductItem { Id = 12, Barcode = "004001", Name = "Cappuccino", Price = 6.50m, Stock = 60, IsSelected = false },
            };

            filteredProducts = new ObservableCollection<ProductItem>(allProducts);
        }

        private void BindData()
        {
            ProductsGrid.ItemsSource = filteredProducts;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string searchText = SearchBox.Text.ToLower();
            string filterStatus = (FilterCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Products";

            var filtered = allProducts.Where(p =>
            {
                bool matchesSearch = p.Name.ToLower().Contains(searchText) || 
                                    p.Barcode.ToLower().Contains(searchText);

                bool matchesFilter = filterStatus switch
                {
                    "In Stock" => p.Stock > 10,
                    "Low Stock" => p.Stock > 0 && p.Stock <= 10,
                    "Out of Stock" => p.Stock <= 0,
                    _ => true
                };

                return matchesSearch && matchesFilter;
            }).ToList();

            filteredProducts.Clear();
            foreach (var item in filtered)
            {
                filteredProducts.Add(item);
            }
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Product dialog would open here.", "Add Product", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ProductsGrid.SelectedItem as ProductItem;
            if (selectedItem != null)
            {
                MessageBox.Show($"Edit Product: {selectedItem.Name}", "Edit Product", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a product to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ProductsGrid.SelectedItem as ProductItem;
            if (selectedItem != null)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Delete product: {selectedItem.Name}?\n\nThis action cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    allProducts.Remove(selectedItem);
                    filteredProducts.Remove(selectedItem);
                    MessageBox.Show("Product deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Please select a product to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshProducts_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            FilterCombo.SelectedIndex = 0;
            MessageBox.Show("Product list refreshed.", "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var product = allProducts.FirstOrDefault(p => p.Id == id);
                if (product != null)
                {
                    MessageBox.Show($"Product Details:\n\nName: {product.Name}\nBarcode: {product.Barcode}\nPrice: RM {product.Price:F2}\nStock: {product.Stock}", 
                        "Product Details", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ProductsGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedItem = ProductsGrid.SelectedItem as ProductItem;
            if (selectedItem != null)
            {
                MessageBox.Show($"Edit Mode: {selectedItem.Name}", "Edit Product", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
