using System;
using System.Collections.Generic;
using System.Linq;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;
using EZPos.UI.State;

namespace EZPos.Business.Services
{
    public class StockService
    {
        private readonly PosStateStore _store;

        public StockService(PosStateStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Adjusts stock for a product (positive = stock in, negative = stock out).
        /// Writes to DB, inserts a StockMovement audit record, and updates PosStateStore.
        /// </summary>
        public bool AdjustStock(int productId, int changeQty, string reason)
        {
            if (changeQty == 0) return false;

            var record = _store.Products.FirstOrDefault(p => p.Id == productId);
            if (record == null) return false;

            int newStock = record.Stock + changeQty;
            if (newStock < 0) newStock = 0;

            ProductRepository.UpdateStock(productId, newStock);

            StockMovementRepository.Insert(new StockMovement
            {
                ProductId = productId,
                ChangeQty = changeQty,
                Reason    = reason?.ToUpperInvariant() ?? "ADJUSTMENT",
                DateTime  = DateTime.Now
            });

            // Sync to state store
            record.Stock       = newStock;
            record.LastUpdated = DateTime.Now;

            return true;
        }

        /// <summary>Returns all products currently below their reorder level.</summary>
        public IEnumerable<ProductRecord> GetLowStockItems()
        {
            return _store.Products.Where(p => p.Stock <= p.ReorderLevel && p.Stock > 0);
        }

        /// <summary>Returns all products with zero stock.</summary>
        public IEnumerable<ProductRecord> GetOutOfStockItems()
        {
            return _store.Products.Where(p => p.Stock <= 0);
        }
    }
}
