using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EZPos.UI.Pages
{
    public partial class SalesPage : UserControl
    {
        private ObservableCollection<CartItem> cartItems;
        private ObservableCollection<Product> allProducts;

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
        }

        public class CartItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public decimal Total => Price * Quantity;
        }

        public SalesPage()
        {
            InitializeComponent();
            InitializeData();
            BindData();
            UpdateCartSummary();
        }

        private void InitializeData()
        {
            cartItems = new ObservableCollection<CartItem>();
            
            // Sample products data
            allProducts = new ObservableCollection<Product>
            {
                new Product { Id = 1, Name = "Coffee", Price = 5.50m, Stock = 50 },
                new Product { Id = 2, Name = "Tea", Price = 4.00m, Stock = 40 },
                new Product { Id = 3, Name = "Juice", Price = 6.00m, Stock = 35 },
                new Product { Id = 4, Name = "Water", Price = 2.50m, Stock = 100 },
                new Product { Id = 5, Name = "Sandwich", Price = 8.50m, Stock = 25 },
                new Product { Id = 6, Name = "Cake", Price = 7.00m, Stock = 20 },
                new Product { Id = 7, Name = "Cookie", Price = 3.50m, Stock = 60 },
                new Product { Id = 8, Name = "Donut", Price = 4.50m, Stock = 45 },
            };
        }

        private void BindData()
        {
            ProductsGrid.ItemsSource = allProducts;
            CartItemsControl.ItemsSource = cartItems;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FilterProducts();
        }

        private void FilterProducts()
        {
            string searchText = ProductSearchBox.Text.ToLower();
            
            var filtered = allProducts
                .Where(p => p.Name.ToLower().Contains(searchText))
                .ToList();

            ProductsGrid.ItemsSource = new ObservableCollection<Product>(filtered);
        }

        private void ProductItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Product product)
            {
                AddToCart(product);
            }
        }

        private void AddToCart(Product product)
        {
            if (product.Stock <= 0)
            {
                MessageBox.Show("Product out of stock!", "Stock Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existingItem = cartItems.FirstOrDefault(c => c.Id == product.Id);
            
            if (existingItem != null)
            {
                if (existingItem.Quantity < product.Stock)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    MessageBox.Show("Cannot add more. Stock limited!", "Stock Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                cartItems.Add(new CartItem
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1
                });
            }

            UpdateCartSummary();
        }

        private void QuantityPlus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var item = cartItems.FirstOrDefault(c => c.Id == id);
                if (item != null)
                {
                    var product = allProducts.FirstOrDefault(p => p.Id == id);
                    if (product != null && item.Quantity < product.Stock)
                    {
                        item.Quantity++;
                        UpdateCartSummary();
                    }
                }
            }
        }

        private void QuantityMinus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var item = cartItems.FirstOrDefault(c => c.Id == id);
                if (item != null)
                {
                    if (item.Quantity > 1)
                    {
                        item.Quantity--;
                    }
                    else
                    {
                        cartItems.Remove(item);
                    }
                    UpdateCartSummary();
                }
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var item = cartItems.FirstOrDefault(c => c.Id == id);
                if (item != null)
                {
                    cartItems.Remove(item);
                    UpdateCartSummary();
                }
            }
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (cartItems.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Clear all items from cart?",
                    "Confirm Clear",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    cartItems.Clear();
                    UpdateCartSummary();
                }
            }
        }

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Cart is empty. Add items before checkout.", "Cart Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            decimal total = CalculateTotal();
            string paymentMethod = PaymentMethodCombo.SelectedItem?.ToString() ?? "Cash";

            MessageBoxResult result = MessageBox.Show(
                $"Total: RM {total:F2}\nPayment Method: {paymentMethod}\n\nProceed with checkout?",
                "Confirm Checkout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Payment successful! Transaction completed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                cartItems.Clear();
                UpdateCartSummary();
            }
        }

        private void UpdateCartSummary()
        {
            int itemCount = cartItems.Sum(c => c.Quantity);
            decimal subtotal = CalculateSubtotal();
            decimal tax = subtotal * 0.06m;
            decimal total = subtotal + tax;

            CartItemCount.Text = $"{itemCount} items";
            SummaryItems.Text = itemCount.ToString();
            SubtotalText.Text = $"RM {subtotal:F2}";
            TaxText.Text = $"RM {tax:F2}";
            TotalText.Text = $"RM {total:F2}";

            EmptyCartMessage.Visibility = cartItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private decimal CalculateSubtotal()
        {
            return cartItems.Sum(c => c.Total);
        }

        private decimal CalculateTotal()
        {
            decimal subtotal = CalculateSubtotal();
            return subtotal + (subtotal * 0.06m);
        }
    }
}
