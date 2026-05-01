using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EZPos.UI.State
{
    public sealed class PosStateStore : INotifyPropertyChanged
    {
        public ObservableCollection<ProductRecord> Products { get; } = new();
        public ObservableCollection<CartLine> CartItems { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public PosStateStore()
        {
            SeedProducts();

            CartItems.CollectionChanged += OnCartCollectionChanged;
        }

        public int CartItemCount => CartItems.Sum(x => x.Quantity);
        public decimal Subtotal => CartItems.Sum(x => x.LineTotal);
        public decimal Tax => Math.Round(Subtotal * 0.06m, 2);
        public decimal Total => Subtotal + Tax;

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
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = 1
                });
            }
            else if (existing.Quantity < product.Stock)
            {
                existing.Quantity++;
            }
            else
            {
                return false;
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
                line.Quantity++;
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

            if (line.Quantity <= 1)
            {
                CartItems.Remove(line);
            }
            else
            {
                line.Quantity--;
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
        private int stock;
        private int reorderLevel;
        private int maxStock;
        private DateTime lastUpdated;

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

        public int Stock
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
        private int quantity;

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

        public int Quantity
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
