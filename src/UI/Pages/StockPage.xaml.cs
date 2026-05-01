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
    public partial class StockPage : UserControl
    {
        private ObservableCollection<StockItem> allStockItems;
        private ObservableCollection<StockItem> filteredStockItems;

        public class StockItem
        {
            public int Id { get; set; }
            public string Barcode { get; set; }
            public string Name { get; set; }
            public int Stock { get; set; }
            public int ReorderLevel { get; set; }
            public int MaxStock { get; set; }
            public DateTime LastUpdated { get; set; }
            
            public int StockPercentage
            {
                get => MaxStock > 0 ? (int)((Stock * 100) / MaxStock) : 0;
            }
        }

        public StockPage()
        {
            InitializeComponent();
            InitializeData();
            BindData();
        }

        private void InitializeData()
        {
            allStockItems = new ObservableCollection<StockItem>
            {
                new StockItem { Id = 1, Barcode = "001001", Name = "Espresso Coffee", Stock = 45, ReorderLevel = 15, MaxStock = 100, LastUpdated = DateTime.Now.AddDays(-2) },
                new StockItem { Id = 2, Barcode = "001002", Name = "Black Tea", Stock = 8, ReorderLevel = 20, MaxStock = 50, LastUpdated = DateTime.Now.AddDays(-1) },
                new StockItem { Id = 3, Barcode = "001003", Name = "Orange Juice", Stock = 0, ReorderLevel = 25, MaxStock = 80, LastUpdated = DateTime.Now },
                new StockItem { Id = 4, Barcode = "001004", Name = "Mineral Water", Stock = 120, ReorderLevel = 30, MaxStock = 150, LastUpdated = DateTime.Now.AddDays(-3) },
                new StockItem { Id = 5, Barcode = "002001", Name = "Cheese Sandwich", Stock = 22, ReorderLevel = 10, MaxStock = 50, LastUpdated = DateTime.Now },
                new StockItem { Id = 6, Barcode = "002002", Name = "Chocolate Cake", Stock = 15, ReorderLevel = 8, MaxStock = 40, LastUpdated = DateTime.Now.AddHours(-5) },
                new StockItem { Id = 7, Barcode = "002003", Name = "Cookie Mix", Stock = 5, ReorderLevel = 12, MaxStock = 60, LastUpdated = DateTime.Now },
                new StockItem { Id = 8, Barcode = "002004", Name = "Donut Box", Stock = 38, ReorderLevel = 15, MaxStock = 80, LastUpdated = DateTime.Now.AddDays(-1) },
                new StockItem { Id = 9, Barcode = "003001", Name = "Muffin Blueberry", Stock = 12, ReorderLevel = 10, MaxStock = 50, LastUpdated = DateTime.Now.AddHours(-12) },
                new StockItem { Id = 10, Barcode = "003002", Name = "Croissant", Stock = 0, ReorderLevel = 20, MaxStock = 60, LastUpdated = DateTime.Now.AddDays(-4) },
            };

            filteredStockItems = new ObservableCollection<StockItem>(allStockItems);
        }

        private void BindData()
        {
            InventoryGrid.ItemsSource = filteredStockItems;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StockFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string searchText = StockSearchBox.Text.ToLower();
            string stockFilter = (StockFilterCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Stock Levels";

            var filtered = allStockItems.Where(s =>
            {
                bool matchesSearch = s.Name.ToLower().Contains(searchText) || s.Barcode.ToLower().Contains(searchText);

                bool matchesFilter = stockFilter switch
                {
                    "In Stock" => s.Stock > s.ReorderLevel,
                    "Low Stock" => s.Stock > 0 && s.Stock <= s.ReorderLevel,
                    "Critical" => s.Stock > 0 && s.Stock <= 10,
                    "Out of Stock" => s.Stock <= 0,
                    _ => true
                };

                return matchesSearch && matchesFilter;
            }).ToList();

            filteredStockItems.Clear();
            foreach (var item in filtered)
            {
                filteredStockItems.Add(item);
            }
        }

        private void StockIn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Stock-In dialog would open here to add inventory.", "Stock-In", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StockOut_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Stock-Out dialog would open here to remove inventory.", "Stock-Out", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            StockSearchBox.Clear();
            StockFilterCombo.SelectedIndex = 0;
            CategoryFilterCombo.SelectedIndex = 0;
            MessageBox.Show("Stock inventory refreshed.", "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditStock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var item = allStockItems.FirstOrDefault(s => s.Id == id);
                if (item != null)
                {
                    MessageBox.Show($"Edit Stock for: {item.Name}\n\nCurrent: {item.Stock} units\nReorder Level: {item.ReorderLevel}", 
                        "Edit Stock", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
