using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EZPos.Business.Services;
using EZPos.UI.Dialogs;
using EZPos.UI.State;

namespace EZPos.UI.Pages
{
    public partial class SalesPage : UserControl
    {
        private readonly PosStateStore stateStore;
        private readonly SaleService saleService;
        private ICollectionView? productsView;
        private bool isInitialized;

        public SalesPage(PosStateStore stateStore, SaleService saleService)
        {
            InitializeComponent();

            this.stateStore  = stateStore;
            this.saleService = saleService;
            // Independent view per page — never share the default view across pages
            productsView = new ListCollectionView(this.stateStore.Products);
            this.stateStore.PropertyChanged += StateStore_PropertyChanged;

            Loaded += SalesPage_Loaded;
        }

        private void SalesPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInitialized)
            {
                return;
            }

            // Apply filter here — controls are fully ready and page is in visual tree
            productsView.Filter = ProductFilter;

            ProductsList.ItemsSource = productsView;
            CartItemsControl.ItemsSource = stateStore.CartItems;

            if (PaymentMethodCombo is not null)
            {
                PaymentMethodCombo.SelectedIndex = 0;
            }

            RefreshSummary();
            isInitialized = true;
        }

        private void StateStore_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PosStateStore.CartItemCount)
                || e.PropertyName == nameof(PosStateStore.Subtotal)
                || e.PropertyName == nameof(PosStateStore.Tax)
                || e.PropertyName == nameof(PosStateStore.Total))
            {
                RefreshSummary();
            }
        }

        private bool ProductFilter(object obj)
        {
            if (obj is not ProductRecord product)
            {
                return false;
            }

            var search = ProductSearchBox?.Text?.Trim() ?? string.Empty;
            var category = (CategoryCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Categories";

            var matchesSearch = string.IsNullOrWhiteSpace(search)
                || product.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || product.Barcode.Contains(search, StringComparison.OrdinalIgnoreCase);

            var matchesCategory = category == "All Categories"
                || string.Equals(product.Category, category, StringComparison.OrdinalIgnoreCase);

            return matchesSearch && matchesCategory;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || productsView is null)
            {
                return;
            }

            productsView.Refresh();
        }

        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialized || productsView is null)
            {
                return;
            }

            productsView.Refresh();
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || productsView is null)
            {
                return;
            }

            productsView.Refresh();
        }

        private void ProductItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: ProductRecord product })
            {
                return;
            }

            if (!stateStore.AddToCart(product.Id))
            {
                MessageBox.Show("This product is unavailable or has reached stock limit in cart.", "Stock Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void QuantityPlus_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetProductId(sender, out var productId))
            {
                return;
            }

            stateStore.IncreaseQuantity(productId);
        }

        private void QuantityMinus_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetProductId(sender, out var productId))
            {
                return;
            }

            stateStore.DecreaseQuantity(productId);
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetProductId(sender, out var productId))
            {
                return;
            }

            stateStore.RemoveFromCart(productId);
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (stateStore.CartItems.Count == 0)
            {
                return;
            }

            var result = MessageBox.Show(
                "Clear all cart items?",
                "Confirm Clear",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                stateStore.ClearCart();
            }
        }

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            if (stateStore.CartItems.Count == 0)
            {
                MessageBox.Show("Cart is empty. Add products before checkout.", "Cart Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var paymentMethod = (PaymentMethodCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cash";
            var total = stateStore.Total;

            // For card/QR payments tendered = total exactly
            decimal tendered = total;

            if (paymentMethod == "Cash")
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Total: RM {total:F2}\n\nEnter cash tendered (RM):",
                    "Cash Tendered",
                    total.ToString("F2"));

                if (string.IsNullOrWhiteSpace(input))
                    return; // cancelled

                if (!decimal.TryParse(input, out tendered) || tendered < total)
                {
                    MessageBox.Show($"Tendered amount must be at least RM {total:F2}.",
                        "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var result = saleService.ProcessSale(paymentMethod, tendered);

            if (!result.Success)
            {
                MessageBox.Show($"Checkout failed:\n{result.ErrorMessage}",
                    "Checkout Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var receipt = new ReceiptDialog(result) { Owner = Window.GetWindow(this) };
            receipt.ShowDialog();
        }

        private void RefreshSummary()
        {
            if (CartItemCount is null || SummaryItems is null || SubtotalText is null || TaxText is null || TotalText is null || EmptyCartMessage is null)
            {
                return;
            }

            CartItemCount.Text = $"{stateStore.CartItemCount} items";
            SummaryItems.Text = stateStore.CartItemCount.ToString();
            SubtotalText.Text = $"RM {stateStore.Subtotal:F2}";
            TaxText.Text = $"RM {stateStore.Tax:F2}";
            TotalText.Text = $"RM {stateStore.Total:F2}";
            EmptyCartMessage.Visibility = stateStore.CartItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool TryGetProductId(object sender, out int productId)
        {
            productId = 0;
            if (sender is not Button { Tag: not null } button)
            {
                return false;
            }

            if (button.Tag is int id)
            {
                productId = id;
                return true;
            }

            return int.TryParse(button.Tag.ToString(), out productId);
        }
    }
}
