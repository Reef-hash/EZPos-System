using System;
using System.Collections.Generic;
using System.Data.SQLite;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;
using EZPos.UI.State;

namespace EZPos.Business.Services
{
    public class SaleResult
    {
        public bool Success { get; set; }
        public int SaleId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        /// <summary>Cash rounding adjustment applied at checkout (e.g. +0.01, -0.02). Zero for non-cash.</summary>
        public decimal RoundingAdj { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Tendered { get; set; }
        public decimal Change { get; set; }
        public List<CartLine> Lines { get; set; }
        public DateTime DateTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SaleService
    {
        private readonly PosStateStore _store;

        public SaleService(PosStateStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Processes the current cart as a completed sale.
        /// Writes Sale + SaleItems to DB, decrements stock in DB and state store, clears cart.
        /// </summary>
        /// <param name="paymentMethod">Cash / Card / QR Code / Cheque</param>
        /// <param name="tendered">Amount physically handed over by the customer.</param>
        /// <param name="roundingAdj">
        /// Cash rounding adjustment added to the pre-tax total (e.g. +0.01 or -0.02).
        /// Pass 0 for non-cash payments.
        /// </param>
        public SaleResult ProcessSale(string paymentMethod, decimal tendered, decimal roundingAdj = 0m)
        {
            if (_store.CartItems.Count == 0)
                return new SaleResult { Success = false, ErrorMessage = "Cart is empty." };

            var subtotal    = _store.Subtotal;
            var tax         = _store.Tax;
            var baseTotal   = _store.Total;
            var total       = baseTotal + roundingAdj;   // actual amount charged (rounded for cash)

            if (tendered < total)
                return new SaleResult { Success = false, ErrorMessage = "Tendered amount is less than total." };

            var sale = new Sale
            {
                DateTime      = DateTime.Now,
                TotalAmount   = total,
                PaymentMethod = paymentMethod
            };

            var items = new List<SaleItem>();
            foreach (var line in _store.CartItems)
            {
                items.Add(new SaleItem
                {
                    ProductId = line.ProductId,
                    Quantity  = line.Quantity,
                    Price     = line.UnitPrice
                });
            }

            // Capture cart snapshot before clearing
            var cartSnapshot = new List<CartLine>(_store.CartItems);

            int saleId;
            try
            {
                saleId = SaleRepository.AddSale(sale, items);
            }
            catch (Exception ex)
            {
                return new SaleResult { Success = false, ErrorMessage = $"Database error: {ex.Message}" };
            }

            // Sync stock reductions back to PosStateStore so UI reflects new stock levels
            foreach (var line in cartSnapshot)
            {
                var product = _store.Products.IndexOf(
                    System.Linq.Enumerable.FirstOrDefault(_store.Products, p => p.Id == line.ProductId));
                var record = System.Linq.Enumerable.FirstOrDefault(_store.Products, p => p.Id == line.ProductId);
                if (record != null)
                    record.Stock -= line.Quantity;
            }

            _store.ClearCart();

            return new SaleResult
            {
                Success       = true,
                SaleId        = saleId,
                Subtotal      = subtotal,
                Tax           = tax,
                Total         = total,
                RoundingAdj   = roundingAdj,
                PaymentMethod = paymentMethod,
                Tendered      = tendered,
                Change        = tendered - total,
                Lines         = cartSnapshot,
                DateTime      = sale.DateTime
            };
        }
    }
}
