using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using EZPos.DataAccess.Repositories;

namespace EZPos.UI.State
{
    /// <summary>How tax is calculated and applied on a sale.</summary>
    public enum TaxMode
    {
        /// <summary>Tax = Subtotal × rate. Charged to customer.</summary>
        PerReceipt,
        /// <summary>Tax = Σ(item price × qty × rate). Semantically per-item, same math for flat rate. Charged to customer.</summary>
        PerItem,
        /// <summary>Tax is shown on the receipt for display only. Customer pays Subtotal only (Total = Subtotal).</summary>
        Fake
    }

    public sealed class PosStateStore : INotifyPropertyChanged
    {
        public ObservableCollection<ProductRecord> Products { get; } = new();
        public ObservableCollection<CartLine> CartItems { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Tax rate as a fraction (e.g. 0.06 for 6%). Read from config on construction.</summary>
        public decimal TaxRate { get; private set; }

        /// <summary>How tax is calculated. Read from config on construction.</summary>
        public TaxMode TaxMode { get; private set; }

        public PosStateStore()
        {
            CartItems.CollectionChanged += OnCartCollectionChanged;
            ReloadTaxConfig();
        }

        /// <summary>Re-reads TaxRate and TaxMode from config.ini. Call after settings are saved.</summary>
        public void ReloadTaxConfig()
        {
            if (decimal.TryParse(ConfigHelper.Get("TaxRate", "6"), out var rate))
                TaxRate = Math.Clamp(rate, 0, 100) / 100m;
            else
                TaxRate = 0.06m;

            TaxMode = ConfigHelper.Get("TaxMode", "PerReceipt") switch
            {
                "PerItem" => TaxMode.PerItem,
                "Fake"    => TaxMode.Fake,
                _         => TaxMode.PerReceipt
            };
        }

        /// <summary>Replaces the in-memory product list with data loaded from the DB.</summary>
        public void LoadProducts(System.Collections.Generic.List<EZPos.Models.Domain.Product> dbProducts)
        {
            Products.Clear();
            foreach (var p in dbProducts)
            {
                Products.Add(new ProductRecord
                {
                    Id              = p.Id,
                    Barcode         = p.Barcode,
                    Name            = p.Name,
                    Category        = p.Category,
                    Price           = p.Price,
                    Stock           = p.Stock,
                    ReorderLevel    = p.ReorderLevel,
                    MaxStock        = p.MaxStock,
                    LastUpdated     = p.LastUpdated,
                    UnitType        = p.UnitType,
                    ConversionRate  = p.ConversionRate,
                    ParentProductId = p.ParentProductId
                });
            }

            // If DB is empty (first run / dev), seed with sample data so the UI is not blank
            if (Products.Count == 0)
                SeedProducts();
        }

        /// <summary>Adds a newly created product to the state store.</summary>
        public void AddProduct(EZPos.Models.Domain.Product p)
        {
            Products.Add(new ProductRecord
            {
                Id              = p.Id,
                Barcode         = p.Barcode,
                Name            = p.Name,
                Category        = p.Category,
                Price           = p.Price,
                Stock           = p.Stock,
                ReorderLevel    = p.ReorderLevel,
                MaxStock        = p.MaxStock,
                LastUpdated     = p.LastUpdated,
                UnitType        = p.UnitType,
                ConversionRate  = p.ConversionRate,
                ParentProductId = p.ParentProductId
            });
        }

        /// <summary>Replaces the matching product record in the state store with updated values.</summary>
        public void UpdateProduct(EZPos.Models.Domain.Product p)
        {
            var existing = System.Linq.Enumerable.FirstOrDefault(Products, r => r.Id == p.Id);
            if (existing == null) return;
            existing.Barcode         = p.Barcode;
            existing.Name            = p.Name;
            existing.Category        = p.Category;
            existing.Price           = p.Price;
            existing.Stock           = p.Stock;
            existing.ReorderLevel    = p.ReorderLevel;
            existing.MaxStock        = p.MaxStock;
            existing.LastUpdated     = p.LastUpdated;
            existing.UnitType        = p.UnitType;
            existing.ConversionRate  = p.ConversionRate;
            existing.ParentProductId = p.ParentProductId;
        }

        /// <summary>Removes a product from the state store by Id.</summary>
        public void RemoveProduct(int productId)
        {
            var existing = System.Linq.Enumerable.FirstOrDefault(Products, r => r.Id == productId);
            if (existing != null)
                Products.Remove(existing);
        }

        /// <summary>Number of distinct product lines in the cart.</summary>
        public int CartItemCount => CartItems.Count;
        public decimal Subtotal => CartItems.Sum(x => x.LineTotal);

        /// <summary>
        /// Displayed tax amount.
        /// PerReceipt / PerItem: Subtotal × TaxRate (for flat rate, math is identical).
        /// Fake: same display amount but not charged (Total == Subtotal).
        /// </summary>
        public decimal Tax => Math.Round(Subtotal * TaxRate, 2);

        /// <summary>
        /// Amount the customer actually pays.
        /// Fake mode: Total == Subtotal (tax is cosmetic only).
        /// All other modes: Total == Subtotal + Tax.
        /// </summary>
        public decimal Total => TaxMode == TaxMode.Fake ? Subtotal : Subtotal + Tax;

        /// <summary>
        /// Adds one unit of a Unit or Pack product to the cart.
        /// Returns false if the product is out of stock or the cart quantity would exceed stock.
        /// Do NOT call this for Weight products — use <see cref="AddWeightToCart"/> instead.
        /// </summary>
        public bool AddToCart(int productId)
        {
            var product = Products.FirstOrDefault(x => x.Id == productId);
            if (product is null || product.Stock <= 0)
            {
                return false;
            }

            var existing = CartItems.FirstOrDefault(x => x.ProductId == productId);
            if (existing is null)
            {
                CartItems.Add(new CartLine
                {
                    ProductId   = product.Id,
                    ProductName = product.Name,
                    UnitPrice   = product.Price,
                    Quantity    = 1m
                });
            }
            else if (existing.Quantity < product.Stock)
            {
                existing.Quantity += 1m;
            }
            else
            {
                return false;
            }

            NotifyCartSummaryChanged();
            return true;
        }

        /// <summary>
        /// Adds a weight-based product to the cart with the specified weight in kg.
        /// If the product is already in the cart, the weights are summed.
        /// Returns false if weight is not positive or exceeds available stock.
        /// </summary>
        public bool AddWeightToCart(int productId, decimal weightKg)
        {
            if (weightKg <= 0m) return false;

            var product = Products.FirstOrDefault(x => x.Id == productId);
            if (product is null || product.Stock <= 0)
                return false;

            var existing = CartItems.FirstOrDefault(x => x.ProductId == productId);
            if (existing is null)
            {
                if (weightKg > product.Stock) return false;
                CartItems.Add(new CartLine
                {
                    ProductId   = product.Id,
                    ProductName = product.Name,
                    UnitPrice   = product.Price,
                    Quantity    = weightKg
                });
            }
            else
            {
                if (existing.Quantity + weightKg > product.Stock) return false;
                existing.Quantity += weightKg;
            }

            NotifyCartSummaryChanged();
            return true;
        }

        public void IncreaseQuantity(int productId)
        {
            var line = CartItems.FirstOrDefault(x => x.ProductId == productId);
            var product = Products.FirstOrDefault(x => x.Id == productId);
            if (line is null || product is null)
            {
                return;
            }

            if (line.Quantity < product.Stock)
            {
                line.Quantity += 1m;
                NotifyCartSummaryChanged();
            }
        }

        public void DecreaseQuantity(int productId)
        {
            var line = CartItems.FirstOrDefault(x => x.ProductId == productId);
            if (line is null)
            {
                return;
            }

            if (line.Quantity <= 1m)
            {
                CartItems.Remove(line);
            }
            else
            {
                line.Quantity -= 1m;
            }

            NotifyCartSummaryChanged();
        }

        public void RemoveFromCart(int productId)
        {
            var line = CartItems.FirstOrDefault(x => x.ProductId == productId);
            if (line is null)
            {
                return;
            }

            CartItems.Remove(line);
            NotifyCartSummaryChanged();
        }

        public void ClearCart()
        {
            if (CartItems.Count == 0)
            {
                return;
            }

            CartItems.Clear();
            NotifyCartSummaryChanged();
        }

        private void SeedProducts()
        {
            if (Products.Count > 0)
            {
                return;
            }

            var seed = new List<ProductRecord>
            {
                new() { Id = 1, Barcode = "001001", Name = "Espresso Coffee", Category = "Beverages", Price = 5.50m, Stock = 45, ReorderLevel = 15, MaxStock = 100 },
                new() { Id = 2, Barcode = "001002", Name = "Black Tea", Category = "Beverages", Price = 4.00m, Stock = 8, ReorderLevel = 20, MaxStock = 50 },
                new() { Id = 3, Barcode = "001003", Name = "Orange Juice", Category = "Beverages", Price = 6.00m, Stock = 0, ReorderLevel = 25, MaxStock = 80 },
                new() { Id = 4, Barcode = "001004", Name = "Mineral Water", Category = "Beverages", Price = 2.50m, Stock = 120, ReorderLevel = 30, MaxStock = 150 },
                new() { Id = 5, Barcode = "002001", Name = "Cheese Sandwich", Category = "Snacks", Price = 8.50m, Stock = 22, ReorderLevel = 10, MaxStock = 50 },
                new() { Id = 6, Barcode = "002002", Name = "Chocolate Cake", Category = "Baked Goods", Price = 7.00m, Stock = 15, ReorderLevel = 8, MaxStock = 40 },
                new() { Id = 7, Barcode = "002003", Name = "Cookie Mix", Category = "Snacks", Price = 3.50m, Stock = 5, ReorderLevel = 12, MaxStock = 60 },
                new() { Id = 8, Barcode = "002004", Name = "Donut Box", Category = "Baked Goods", Price = 4.50m, Stock = 38, ReorderLevel = 15, MaxStock = 80 },
                new() { Id = 9, Barcode = "003001", Name = "Muffin Blueberry", Category = "Baked Goods", Price = 6.50m, Stock = 12, ReorderLevel = 10, MaxStock = 50 },
                new() { Id = 10, Barcode = "003002", Name = "Croissant", Category = "Baked Goods", Price = 5.00m, Stock = 0, ReorderLevel = 20, MaxStock = 60 }
            };

            foreach (var item in seed)
            {
                item.LastUpdated = DateTime.Now.AddDays(-item.Id % 4);
                Products.Add(item);
            }
        }

        private void OnCartCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (var item in e.OldItems.OfType<CartLine>())
                {
                    item.PropertyChanged -= OnCartLinePropertyChanged;
                }
            }

            if (e.NewItems is not null)
            {
                foreach (var item in e.NewItems.OfType<CartLine>())
                {
                    item.PropertyChanged += OnCartLinePropertyChanged;
                }
            }

            NotifyCartSummaryChanged();
        }

        private void OnCartLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartLine.Quantity) || e.PropertyName == nameof(CartLine.LineTotal))
            {
                NotifyCartSummaryChanged();
            }
        }

        private void NotifyCartSummaryChanged()
        {
            OnPropertyChanged(nameof(CartItemCount));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Tax));
            OnPropertyChanged(nameof(Total));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class ProductRecord : ObservableEntity
    {
        private string barcode = string.Empty;
        private string name = string.Empty;
        private string category = string.Empty;
        private decimal price;
        private decimal stock;
        private int reorderLevel;
        private int maxStock;
        private DateTime lastUpdated;
        private EZPos.Models.Domain.UnitType unitType;
        private decimal conversionRate = 1m;
        private int? parentProductId;

        public int Id { get; set; }

        public string Barcode
        {
            get => barcode;
            set => SetProperty(ref barcode, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string Category
        {
            get => category;
            set => SetProperty(ref category, value);
        }

        public decimal Price
        {
            get => price;
            set => SetProperty(ref price, value);
        }

        public decimal Stock
        {
            get => stock;
            set
            {
                if (SetProperty(ref stock, value))
                {
                    OnPropertyChanged(nameof(StockStatus));
                    OnPropertyChanged(nameof(StockPercentage));
                }
            }
        }

        public int ReorderLevel
        {
            get => reorderLevel;
            set
            {
                if (SetProperty(ref reorderLevel, value))
                {
                    OnPropertyChanged(nameof(StockStatus));
                }
            }
        }

        public int MaxStock
        {
            get => maxStock;
            set
            {
                if (SetProperty(ref maxStock, value))
                {
                    OnPropertyChanged(nameof(StockPercentage));
                }
            }
        }

        public DateTime LastUpdated
        {
            get => lastUpdated;
            set => SetProperty(ref lastUpdated, value);
        }

        public EZPos.Models.Domain.UnitType UnitType
        {
            get => unitType;
            set => SetProperty(ref unitType, value);
        }

        /// <summary>For Pack products: how many base units are in one pack.</summary>
        public decimal ConversionRate
        {
            get => conversionRate;
            set => SetProperty(ref conversionRate, value);
        }

        /// <summary>For Pack products: Id of the base Unit product whose stock is deducted on sale.</summary>
        public int? ParentProductId
        {
            get => parentProductId;
            set => SetProperty(ref parentProductId, value);
        }

        public string StockStatus => Stock <= 0 ? "Out of Stock" : Stock <= ReorderLevel ? "Low Stock" : "In Stock";

        public int StockPercentage
        {
            get
            {
                if (MaxStock <= 0)
                {
                    return 0;
                }

                var percent = (int)Math.Round((double)(Stock * 100) / MaxStock);
                return Math.Max(0, Math.Min(100, percent));
            }
        }
    }

    public sealed class CartLine : ObservableEntity
    {
        private string productName = string.Empty;
        private decimal unitPrice;
        private decimal quantity;

        public int ProductId { get; set; }

        public string ProductName
        {
            get => productName;
            set => SetProperty(ref productName, value);
        }

        public decimal UnitPrice
        {
            get => unitPrice;
            set
            {
                if (SetProperty(ref unitPrice, value))
                {
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }

        /// <summary>
        /// Quantity sold. Whole numbers for Unit/Pack products; decimal for Weight products (kg).
        /// </summary>
        public decimal Quantity
        {
            get => quantity;
            set
            {
                if (SetProperty(ref quantity, value))
                {
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }

        public decimal LineTotal => UnitPrice * Quantity;
    }

    public abstract class ObservableEntity : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
