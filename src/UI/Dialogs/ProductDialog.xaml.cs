using System;
using System.Linq;
using System.Windows;
using EZPos.Business.Services;
using EZPos.Models.Domain;

namespace EZPos.UI.Dialogs
{
    /// <summary>
    /// Add / Edit product dialog.
    /// Pass null for existingProduct to open in Add mode.
    /// Pass an existing product to open in Edit mode.
    /// After ShowDialog() == true, read the saved product from the Product property.
    /// </summary>
    public partial class ProductDialog : Window
    {
        private readonly ProductService _productService;
        private readonly Product? _editingProduct;

        /// <summary>The saved product after a successful Save. Null if dialog was cancelled.</summary>
        public Product? Product { get; private set; }

        // ── Constructor: Add mode ──────────────────────────────────────────────
        public ProductDialog(ProductService productService)
        {
            InitializeComponent();
            _productService = productService;
            _editingProduct = null;

            TitleText.Text = "Add Product";
            CategoryCombo.SelectedIndex = 0;
        }

        // ── Constructor: Edit mode ─────────────────────────────────────────────
        public ProductDialog(ProductService productService, Product existingProduct)
        {
            InitializeComponent();
            _productService = productService;
            _editingProduct = existingProduct;

            TitleText.Text = "Edit Product";
            SaveText.Text  = "Save Changes";

            // Pre-fill fields
            NameBox.Text    = existingProduct.Name;
            BarcodeBox.Text = existingProduct.Barcode;
            PriceBox.Text   = existingProduct.Price.ToString("F2");
            StockBox.Text   = existingProduct.Stock.ToString();
            ReorderBox.Text = existingProduct.ReorderLevel.ToString();

            // Select or set category
            var match = CategoryCombo.Items
                .OfType<System.Windows.Controls.ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(i.Content?.ToString(), existingProduct.Category, StringComparison.OrdinalIgnoreCase));

            if (match != null)
                CategoryCombo.SelectedItem = match;
            else
                CategoryCombo.Text = existingProduct.Category;
        }

        // ── Save ──────────────────────────────────────────────────────────────
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate())
                return;

            var name     = NameBox.Text.Trim();
            var barcode  = BarcodeBox.Text.Trim();
            var price    = decimal.Parse(PriceBox.Text.Trim());
            var stock    = int.Parse(StockBox.Text.Trim());
            var reorder  = int.Parse(ReorderBox.Text.Trim());
            var category = (CategoryCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item
                ? item.Content?.ToString()
                : CategoryCombo.Text?.Trim()) ?? "General";

            if (string.IsNullOrWhiteSpace(category))
                category = "General";

            if (_editingProduct == null)
            {
                // ── Add mode ──
                var product = new Product
                {
                    Name         = name,
                    Barcode      = barcode,
                    Price        = price,
                    Stock        = stock,
                    ReorderLevel = reorder,
                    Category     = category,
                    MaxStock     = Math.Max(stock * 2, 100),
                    LastUpdated  = DateTime.Now
                };

                try
                {
                    _productService.Add(product);
                    Product = product;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save product:\n{ex.Message}",
                        "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                // ── Edit mode ──
                _editingProduct.Name         = name;
                _editingProduct.Barcode      = barcode;
                _editingProduct.Price        = price;
                _editingProduct.Stock        = stock;
                _editingProduct.ReorderLevel = reorder;
                _editingProduct.Category     = category;
                _editingProduct.LastUpdated  = DateTime.Now;

                try
                {
                    _productService.Update(_editingProduct);
                    Product = _editingProduct;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update product:\n{ex.Message}",
                        "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            DialogResult = true;
        }

        // ── Cancel ────────────────────────────────────────────────────────────
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // ── Validation ────────────────────────────────────────────────────────
        private bool Validate()
        {
            bool valid = true;

            // Name
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                NameError.Text       = "Product name is required.";
                NameError.Visibility = Visibility.Visible;
                valid = false;
            }
            else
            {
                NameError.Visibility = Visibility.Collapsed;
            }

            // Barcode
            var barcode = BarcodeBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                BarcodeError.Text       = "Barcode is required.";
                BarcodeError.Visibility = Visibility.Visible;
                valid = false;
            }
            else
            {
                // Uniqueness check — skip for edit if barcode unchanged
                bool isDuplicate = _productService.GetAll()
                    .Any(p => string.Equals(p.Barcode, barcode, StringComparison.OrdinalIgnoreCase)
                              && p.Id != (_editingProduct?.Id ?? 0));

                if (isDuplicate)
                {
                    BarcodeError.Text       = "A product with this barcode already exists.";
                    BarcodeError.Visibility = Visibility.Visible;
                    valid = false;
                }
                else
                {
                    BarcodeError.Visibility = Visibility.Collapsed;
                }
            }

            // Price
            if (!decimal.TryParse(PriceBox.Text.Trim(), out var price) || price <= 0)
            {
                PriceError.Visibility = Visibility.Visible;
                valid = false;
            }
            else
            {
                PriceError.Visibility = Visibility.Collapsed;
            }

            // Stock
            if (!int.TryParse(StockBox.Text.Trim(), out var stock) || stock < 0)
            {
                StockError.Visibility = Visibility.Visible;
                valid = false;
            }
            else
            {
                StockError.Visibility = Visibility.Collapsed;
            }

            // Reorder level
            if (!int.TryParse(ReorderBox.Text.Trim(), out var reorder) || reorder < 0)
            {
                ReorderError.Visibility = Visibility.Visible;
                valid = false;
            }
            else
            {
                ReorderError.Visibility = Visibility.Collapsed;
            }

            return valid;
        }
    }
}
