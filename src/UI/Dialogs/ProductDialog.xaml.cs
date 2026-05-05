using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private readonly ProductService  _productService;
        private readonly CategoryService _categoryService;
        private readonly Product? _editingProduct;

        /// <summary>The saved product after a successful Save. Null if dialog was cancelled.</summary>
        public Product? Product { get; private set; }

        // ── Constructor: Add mode ──────────────────────────────────────────────
        public ProductDialog(ProductService productService, CategoryService categoryService)
        {
            InitializeComponent();
            _productService  = productService;
            _categoryService = categoryService;
            _editingProduct  = null;

            TitleText.Text = "Add Product";
            UnitTypeCombo.SelectedIndex = 0;
            Loaded += (_, _) => { LoadCategories(null); PopulateParentProductCombo(); };
        }

        // ── Constructor: Scan mode (new product with barcode pre-filled) ──────
        public ProductDialog(ProductService productService, CategoryService categoryService, string scannedBarcode)
        {
            InitializeComponent();
            _productService  = productService;
            _categoryService = categoryService;
            _editingProduct  = null;

            TitleText.Text  = "Register New Product";
            SaveText.Text   = "Register Product";

            BarcodeBox.Text       = scannedBarcode;
            BarcodeBox.IsReadOnly = true;
            UnitTypeCombo.SelectedIndex = 0;

            Loaded += (_, _) =>
            {
                LoadCategories(null);
                PopulateParentProductCombo();
                NameBox.Focus();
            };
        }

        // ── Constructor: Edit mode ─────────────────────────────────────────────
        public ProductDialog(ProductService productService, CategoryService categoryService, Product existingProduct)
        {
            InitializeComponent();
            _productService  = productService;
            _categoryService = categoryService;
            _editingProduct  = existingProduct;

            TitleText.Text = "Edit Product";
            SaveText.Text  = "Save Changes";

            // Pre-fill fields
            NameBox.Text    = existingProduct.Name;
            BarcodeBox.Text = existingProduct.Barcode;
            PriceBox.Text   = existingProduct.Price.ToString("F2");
            StockBox.Text   = existingProduct.Stock.ToString("G");
            ReorderBox.Text = existingProduct.ReorderLevel.ToString();

            // Select unit type
            var utMatch = UnitTypeCombo.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(i.Tag?.ToString(), existingProduct.UnitType.ToString(), StringComparison.OrdinalIgnoreCase));
            if (utMatch != null)
                UnitTypeCombo.SelectedItem = utMatch;
            else
                UnitTypeCombo.SelectedIndex = 0;

            // Pack fields
            ConversionRateBox.Text = existingProduct.ConversionRate.ToString("G");

            Loaded += (_, _) =>
            {
                LoadCategories(existingProduct.Category);
                PopulateParentProductCombo();

                if (existingProduct.ParentProductId.HasValue)
                {
                    var parentItem = ParentProductCombo.Items
                        .OfType<ProductComboItem>()
                        .FirstOrDefault(x => x.Id == existingProduct.ParentProductId.Value);
                    if (parentItem != null)
                        ParentProductCombo.SelectedItem = parentItem;
                }

                UpdatePackPanelVisibility();
            };
        }

        // ── Load categories from service ──────────────────────────────────────
        private void LoadCategories(string? selectName)
        {
            CategoryCombo.Items.Clear();
            foreach (var cat in _categoryService.GetAll())
                CategoryCombo.Items.Add(cat);

            if (selectName != null)
            {
                foreach (var item in CategoryCombo.Items)
                    if (item as string == selectName) { CategoryCombo.SelectedItem = item; break; }
            }
            if (CategoryCombo.SelectedItem == null && CategoryCombo.Items.Count > 0)
                CategoryCombo.SelectedIndex = 0;
        }

        // ── Pack panel visibility ─────────────────────────────────────────────
        private void UnitTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePackPanelVisibility();
        }

        private void UpdatePackPanelVisibility()
        {
            if (PackPanel is null) return;
            var selectedTag = (UnitTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            PackPanel.Visibility = selectedTag == "Pack" ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Populate parent product combo with Unit-type products ─────────────
        private void PopulateParentProductCombo()
        {
            ParentProductCombo.Items.Clear();
            ParentProductCombo.Items.Add(new ProductComboItem { Id = 0, Name = "(None)" });

            foreach (var p in _productService.GetAll())
            {
                if (p.UnitType == UnitType.Unit && p.Id != (_editingProduct?.Id ?? 0))
                    ParentProductCombo.Items.Add(new ProductComboItem { Id = p.Id, Name = p.Name });
            }

            ParentProductCombo.SelectedIndex = 0;
        }

        // ── Save ──────────────────────────────────────────────────────────────
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate())
                return;

            var name     = NameBox.Text.Trim();
            var barcode  = BarcodeBox.Text.Trim();
            var price    = decimal.Parse(PriceBox.Text.Trim(), CultureInfo.InvariantCulture);
            var stock    = decimal.Parse(StockBox.Text.Trim(), CultureInfo.InvariantCulture);
            var reorder  = int.Parse(ReorderBox.Text.Trim());
            var category = CategoryCombo.SelectedItem switch
            {
                string value => value,
                _            => CategoryCombo.Text?.Trim()
            };
            category = (category ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(category)) category = "General";

            // Selling type fields
            var selectedTag = (UnitTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Unit";
            var unitType = Enum.TryParse<UnitType>(selectedTag, out var ut) ? ut : UnitType.Unit;

            decimal conversionRate = 1m;
            int? parentProductId   = null;
            if (unitType == UnitType.Pack)
            {
                decimal.TryParse(ConversionRateBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out conversionRate);
                if (conversionRate <= 0) conversionRate = 1m;

                var parentItem = ParentProductCombo.SelectedItem as ProductComboItem;
                if (parentItem != null && parentItem.Id > 0)
                    parentProductId = parentItem.Id;
            }

            if (_editingProduct == null)
            {
                // ── Add mode ──
                var product = new Product
                {
                    Name            = name,
                    Barcode         = barcode,
                    Price           = price,
                    Stock           = stock,
                    ReorderLevel    = reorder,
                    Category        = category,
                    MaxStock        = (int)Math.Max(stock * 2, 100),
                    LastUpdated     = DateTime.Now,
                    UnitType        = unitType,
                    ConversionRate  = conversionRate,
                    ParentProductId = parentProductId
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
                _editingProduct.Name            = name;
                _editingProduct.Barcode         = barcode;
                _editingProduct.Price           = price;
                _editingProduct.Stock           = stock;
                _editingProduct.ReorderLevel    = reorder;
                _editingProduct.Category        = category;
                _editingProduct.LastUpdated     = DateTime.Now;
                _editingProduct.UnitType        = unitType;
                _editingProduct.ConversionRate  = conversionRate;
                _editingProduct.ParentProductId = parentProductId;

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
            if (!decimal.TryParse(PriceBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                PriceError.Visibility = Visibility.Visible;
                valid = false;
            }
            else
            {
                PriceError.Visibility = Visibility.Collapsed;
            }

            // Stock
            if (!decimal.TryParse(StockBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var stock) || stock < 0)
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

            // Conversion rate (only required for Pack)
            var selectedTag = (UnitTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (selectedTag == "Pack")
            {
                if (!decimal.TryParse(ConversionRateBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var cr) || cr <= 0)
                {
                    ConversionRateError.Visibility = Visibility.Visible;
                    valid = false;
                }
                else
                {
                    ConversionRateError.Visibility = Visibility.Collapsed;
                }
            }

            return valid;
        }

        // ── Helper class for ParentProductCombo items ─────────────────────────
        private sealed class ProductComboItem
        {
            public int    Id   { get; set; }
            public string Name { get; set; } = string.Empty;
            public override string ToString() => Name;
        }
    }
}
