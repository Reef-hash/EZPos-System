using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using EZPos.Business.Services;
using EZPos.DataAccess.Repositories;
using EZPos.UI.Dialogs;
using EZPos.UI.State;

namespace EZPos.UI.Pages
{
    // ─────────────────────────────────────────────────────────────────────────
    // Lightweight model for a customer sales tab.
    // The tab stores a snapshot of its cart lines while it is NOT the active tab.
    // ─────────────────────────────────────────────────────────────────────────
    internal sealed class CartTab
    {
        public CartTab(int number) => Number = number;

        public int    Number     { get; }
        public string Label      => $"Customer {Number}";

        /// <summary>Cart lines saved when this tab is deactivated. Empty while the tab is active.</summary>
        public List<CartLine> SavedItems { get; set; } = new();
    }

    public partial class SalesPage : UserControl
    {
        private readonly PosStateStore stateStore;
        private readonly SaleService   saleService;
        private ICollectionView?       productsView;
        private bool                   isInitialized;

        // ── Tabs ─────────────────────────────────────────────────────────────
        private readonly List<CartTab> _tabs          = new();
        private int                    _activeTabIndex = 0;
        private int                    _nextTabNumber  = 1;

        // ── Barcode scanner detection ─────────────────────────────────────────
        // Scanners fire all chars + Enter within ≈ 50 ms; humans are slower.
        private DateTime _firstKeyTime  = DateTime.MinValue;
        private bool     _scanInProgress;

        public SalesPage(PosStateStore stateStore, SaleService saleService)
        {
            InitializeComponent();

            this.stateStore  = stateStore;
            this.saleService = saleService;

            productsView = new ListCollectionView(this.stateStore.Products);
            this.stateStore.PropertyChanged += StateStore_PropertyChanged;

            Loaded += SalesPage_Loaded;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Initialisation
        // ══════════════════════════════════════════════════════════════════════

        private void SalesPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInitialized) return;

            productsView!.Filter = ProductFilter;
            ProductsList.ItemsSource      = productsView;
            CartItemsControl.ItemsSource  = stateStore.CartItems;

            // Create the first tab without switching (cart is already empty & bound)
            _tabs.Add(CreateTab());
            RebuildTabStrip();

            RefreshSummary();
            isInitialized = true;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Tab management
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Rebuilds the tab-strip buttons from the current <c>_tabs</c> list.</summary>
        private void RebuildTabStrip()
        {
            TabStrip.Children.Clear();

            for (int i = 0; i < _tabs.Count; i++)
            {
                int capturedIndex = i;
                bool active = (i == _activeTabIndex);

                // Tab label button
                var tabBtn = new Button
                {
                    Content    = _tabs[i].Label,
                    Height     = 40,
                    Padding    = new Thickness(16, 0, _tabs.Count > 1 ? 6 : 16, 0),
                    Margin     = new Thickness(0, 0, 6, 0),
                    FontSize   = 14,
                    Background = active
                        ? (Brush)FindResource("PrimaryBrush")
                        : (Brush)FindResource("CardBackgroundBrush"),
                    Foreground = active
                        ? (Brush)FindResource("SidebarBrush")
                        : (Brush)FindResource("TextPrimaryBrush"),
                    BorderThickness = new Thickness(1),
                    Cursor     = Cursors.Hand
                };
                tabBtn.Click += (_, _) => SwitchToTab(capturedIndex);

                // If more than one tab, add a close (×) button inside the tab
                if (_tabs.Count > 1)
                {
                    var closeBtn = new Button
                    {
                        Content         = "✕",
                        Width           = 26,
                        Height          = 26,
                        Padding         = new Thickness(0),
                        Margin          = new Thickness(8, 0, 0, 0),
                        FontSize        = 13,
                        Background      = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Foreground      = active
                            ? (Brush)FindResource("SidebarBrush")
                            : (Brush)FindResource("TextSecondaryBrush"),
                        Cursor  = Cursors.Hand,
                        ToolTip = "Close tab"
                    };
                    closeBtn.Click += (_, ev) =>
                    {
                        ev.Handled = true;   // prevent the parent tab button from firing
                        CloseTab(capturedIndex);
                    };

                    // Wrap label + close in a StackPanel and set as button content
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    panel.Children.Add(new System.Windows.Controls.TextBlock { Text = _tabs[capturedIndex].Label, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
                    panel.Children.Add(closeBtn);
                    tabBtn.Content = panel;
                }

                TabStrip.Children.Add(tabBtn);
            }

            // "+" new tab button
            var newBtn = new Button
            {
                Content         = "+",
                Width           = 40,
                Height          = 40,
                Padding         = new Thickness(0),
                Margin          = new Thickness(0, 0, 0, 0),
                FontSize        = 20,
                Background      = (Brush)FindResource("CardBackgroundBrush"),
                Foreground      = (Brush)FindResource("PrimaryBrush"),
                BorderThickness = new Thickness(1),
                ToolTip         = "Open new customer tab",
                Cursor          = Cursors.Hand
            };
            newBtn.Click += NewTab_Click;
            TabStrip.Children.Add(newBtn);
        }

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            CreateAndSwitchToNewTab();
        }

        private void CreateAndSwitchToNewTab()
        {
            // Save current tab's cart, then add and activate a new empty tab
            SaveActiveTab();
            _tabs.Add(CreateTab());
            _activeTabIndex = _tabs.Count - 1;

            stateStore.ClearCart();          // new tab starts empty
            RebuildTabStrip();
            RefreshSummary();
        }

        private void HoldCurrentTab_Click(object sender, RoutedEventArgs e)
        {
            if (stateStore.CartItems.Count == 0)
            {
                MessageBox.Show("Current cart is empty. Add items before holding this customer.",
                    "Nothing to Hold", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CreateAndSwitchToNewTab();
        }

        private CartTab CreateTab()
        {
            return new CartTab(_nextTabNumber++);
        }

        private void SwitchToTab(int newIndex)
        {
            if (newIndex == _activeTabIndex) return;

            SaveActiveTab();
            _activeTabIndex = newIndex;
            RestoreActiveTab();

            RebuildTabStrip();
            RefreshSummary();
        }

        private void CloseTab(int index)
        {
            if (_tabs.Count <= 1) return;   // always keep at least one tab

            // If closing the active tab, decide which one becomes active
            bool closingActive = (index == _activeTabIndex);

            if (closingActive)
                SaveActiveTab();   // snapshot (will be discarded below)

            _tabs.RemoveAt(index);

            if (closingActive)
            {
                // Move to previous tab; if it was the first, use index 0
                _activeTabIndex = Math.Max(0, index - 1);
                RestoreActiveTab();
            }
            else if (index < _activeTabIndex)
            {
                _activeTabIndex--;  // index shift — no data change needed
            }

            RebuildTabStrip();
            RefreshSummary();
        }

        /// <summary>Saves the current cart contents into the active tab's snapshot.</summary>
        private void SaveActiveTab()
        {
            _tabs[_activeTabIndex].SavedItems = stateStore.CartItems.ToList();
        }

        /// <summary>Restores the active tab's snapshot into the live cart.</summary>
        private void RestoreActiveTab()
        {
            var saved = _tabs[_activeTabIndex].SavedItems;
            stateStore.CartItems.Clear();
            foreach (var line in saved)
                stateStore.CartItems.Add(line);
            _tabs[_activeTabIndex].SavedItems = new List<CartLine>();  // clear snapshot
        }

        // ══════════════════════════════════════════════════════════════════════
        // State change → UI
        // ══════════════════════════════════════════════════════════════════════

        private void StateStore_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PosStateStore.CartItemCount)
                               or nameof(PosStateStore.Subtotal)
                               or nameof(PosStateStore.Tax)
                               or nameof(PosStateStore.Total))
            {
                RefreshSummary();
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Product filter / search
        // ══════════════════════════════════════════════════════════════════════

        private bool ProductFilter(object obj)
        {
            if (obj is not ProductRecord product) return false;

            var search   = ProductSearchBox?.Text?.Trim() ?? string.Empty;
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
            if (!isInitialized || productsView is null) return;
            productsView.Refresh();
        }

        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialized || productsView is null) return;

            if (_firstKeyTime == DateTime.MinValue)
                _firstKeyTime = DateTime.Now;

            productsView.Refresh();
        }

        private void ProductSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !isInitialized) return;

            var input = ProductSearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

            bool isScan = _firstKeyTime != DateTime.MinValue
                          && (DateTime.Now - _firstKeyTime).TotalMilliseconds < 150;

            _firstKeyTime = DateTime.MinValue;

            if (isScan)
            {
                var match = stateStore.Products
                    .FirstOrDefault(p => string.Equals(p.Barcode, input, StringComparison.OrdinalIgnoreCase));

                if (match is not null)
                {
                    if (!stateStore.AddToCart(match.Id))
                        MessageBox.Show("Product has reached stock limit.", "Stock Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        ProductSearchBox.Text = string.Empty;
                }
                else
                {
                    MessageBox.Show($"Barcode '{input}' not found in product list.", "Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ProductSearchBox.SelectAll();
                }

                e.Handled = true;
                return;
            }

            productsView?.Refresh();
            e.Handled = true;
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || productsView is null) return;
            productsView.Refresh();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Cart operations
        // ══════════════════════════════════════════════════════════════════════

        private void ProductItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: ProductRecord product }) return;

            if (!stateStore.AddToCart(product.Id))
                MessageBox.Show("This product is unavailable or has reached stock limit in cart.",
                    "Stock Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void QuantityPlus_Click(object sender, RoutedEventArgs e)
        {
            if (TryGetProductId(sender, out var id)) stateStore.IncreaseQuantity(id);
        }

        private void QuantityMinus_Click(object sender, RoutedEventArgs e)
        {
            if (TryGetProductId(sender, out var id)) stateStore.DecreaseQuantity(id);
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (TryGetProductId(sender, out var id)) stateStore.RemoveFromCart(id);
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (stateStore.CartItems.Count == 0) return;

            var result = MessageBox.Show(
                "Clear all items from the current tab?",
                "Confirm Clear",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                stateStore.ClearCart();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Checkout
        // ══════════════════════════════════════════════════════════════════════

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            if (stateStore.CartItems.Count == 0)
            {
                MessageBox.Show("Cart is empty. Add products before checkout.",
                    "Cart Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var payDialog = new PaymentDialog(stateStore.Subtotal, stateStore.Tax, stateStore.Total)
            {
                Owner = Window.GetWindow(this)
            };

            if (payDialog.ShowDialog() != true)
                return;

            var result = saleService.ProcessSale(
                payDialog.SelectedPaymentMethod,
                payDialog.TenderedAmount,
                payDialog.RoundingAdjustment);

            if (!result.Success)
            {
                MessageBox.Show($"Checkout failed:\n{result.ErrorMessage}",
                    "Checkout Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var receipt = new ReceiptDialog(result) { Owner = Window.GetWindow(this) };
            receipt.ShowDialog();

            // After a successful sale: close this tab if others exist, otherwise just reset
            if (_tabs.Count > 1)
                CloseTab(_activeTabIndex);
            // else: cart was already cleared by SaleService, single tab stays open
        }

        // ══════════════════════════════════════════════════════════════════════
        // Summary bar
        // ══════════════════════════════════════════════════════════════════════

        private void RefreshSummary()
        {
            if (CartItemCount is null || SummaryItems is null || SubtotalText is null
                || TaxLabel is null || TaxText is null || TotalText is null
                || RoundingRow is null || RoundingAdjText is null || EmptyCartMessage is null
                || CartFooterTotalText is null || CartFooterCheckoutText is null)
                return;

            // Dynamic tax label
            var taxRate = ConfigHelper.Get("TaxRate", "6");
            TaxLabel.Text = $"Tax ({taxRate}%):";

            CartItemCount.Text = $"{stateStore.CartItemCount} items";
            SummaryItems.Text  = stateStore.CartItemCount.ToString();
            SubtotalText.Text  = $"RM {stateStore.Subtotal:F2}";
            TaxText.Text       = $"RM {stateStore.Tax:F2}";

            // On sales page we show base total only; payment-specific rounding is handled in PaymentDialog.
            RoundingRow.Visibility = Visibility.Collapsed;
            var total = stateStore.Total;
            TotalText.Text = $"RM {total:F2}";
            CartFooterTotalText.Text = $"RM {total:F2}";
            CartFooterCheckoutText.Text = $"Checkout - RM {total:F2}";

            EmptyCartMessage.Visibility = stateStore.CartItems.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════════════

        private static bool TryGetProductId(object sender, out int productId)
        {
            productId = 0;
            if (sender is not Button { Tag: not null } button) return false;
            if (button.Tag is int id) { productId = id; return true; }
            return int.TryParse(button.Tag.ToString(), out productId);
        }
    }
}

