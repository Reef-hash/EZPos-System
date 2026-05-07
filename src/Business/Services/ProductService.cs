using System;
using System.Collections.Generic;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;
using EZPos.UI.State;

namespace EZPos.Business.Services
{
    public class ProductService
    {
        private readonly PosStateStore _store;

        public ProductService(PosStateStore store)
        {
            _store = store;
        }

        /// <summary>Loads all products from DB and populates PosStateStore.</summary>
        public void LoadAll()
        {
            var products = ProductRepository.GetAll();
            _store.LoadProducts(products);
        }

        /// <summary>Adds a new product to DB and state store. Returns the new product's Id.</summary>
        public int Add(Product product)
        {
            product.LastUpdated = DateTime.Now;
            product.Id = ProductRepository.Add(product);
            _store.AddProduct(product);

            if (product.Stock > 0)
            {
                StockMovementRepository.Insert(new StockMovement
                {
                    ProductId = product.Id,
                    ChangeQty = product.Stock,
                    Reason    = "OPENING_BALANCE",
                    DateTime  = DateTime.Now
                });
            }

            return product.Id;
        }

        /// <summary>Updates an existing product in DB and state store.</summary>
        public void Update(Product product)
        {
            product.LastUpdated = DateTime.Now;
            ProductRepository.Update(product);
            _store.UpdateProduct(product);
        }

        /// <summary>Deletes a product from DB and state store.</summary>
        public void Delete(int productId)
        {
            ProductRepository.Delete(productId);
            _store.RemoveProduct(productId);
        }

        /// <summary>Returns all products from the state store (already loaded on startup).</summary>
        public IReadOnlyList<ProductRecord> GetAll()
        {
            return _store.Products;
        }

        /// <summary>Finds a product record in state store by barcode.</summary>
        public ProductRecord GetByBarcode(string barcode)
        {
            foreach (var p in _store.Products)
            {
                if (p.Barcode == barcode)
                    return p;
            }
            return null;
        }
    }
}
