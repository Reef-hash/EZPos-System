using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace EZPos.UI.Input
{
    /// <summary>
    /// Interprets keyboard input for the sales flow.
    /// - Fast text stream + Enter => barcode scan
    /// - Enter without a valid scan => checkout request
    /// </summary>
    public sealed class SalesKeyboardInputService
    {
        private readonly StringBuilder barcodeBuffer = new();
        private readonly Dictionary<Key, Action> checkoutShortcuts = new();

        private DateTime firstCharTimeUtc = DateTime.MinValue;
        private DateTime lastCharTimeUtc = DateTime.MinValue;
        private DateTime lastCheckoutTimeUtc = DateTime.MinValue;
        private bool checkoutDispatchInProgress;

        public event Action<string>? BarcodeCompleted;
        public event Action? CheckoutRequested;

        /// <summary>Maximum total duration for a stream to be treated as scanner input.</summary>
        public TimeSpan ScanTotalThreshold { get; set; } = TimeSpan.FromMilliseconds(150);

        /// <summary>Maximum gap between chars before stream is reset as a new input sequence.</summary>
        public TimeSpan ScanInterKeyThreshold { get; set; } = TimeSpan.FromMilliseconds(60);

        /// <summary>Prevents duplicate checkout from repeated Enter keypresses.</summary>
        public TimeSpan CheckoutDebounce { get; set; } = TimeSpan.FromMilliseconds(350);

        public SalesKeyboardInputService()
        {
            checkoutShortcuts[Key.Enter] = RequestCheckout;
        }

        public void RegisterTextInput(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var now = DateTime.UtcNow;
            foreach (var c in text)
            {
                if (char.IsControl(c))
                    continue;

                if (lastCharTimeUtc != DateTime.MinValue && now - lastCharTimeUtc > ScanInterKeyThreshold)
                    ClearBarcodeBuffer();

                if (firstCharTimeUtc == DateTime.MinValue)
                    firstCharTimeUtc = now;

                barcodeBuffer.Append(c);
                lastCharTimeUtc = now;
            }
        }

        /// <summary>
        /// Handles keydown and returns true when the key was consumed by the sales input workflow.
        /// </summary>
        public bool TryHandleKeyDown(Key key)
        {
            if (key == Key.Enter)
            {
                if (TryCompleteBarcode(out var barcode))
                {
                    BarcodeCompleted?.Invoke(barcode);
                    return true;
                }
            }

            if (!checkoutShortcuts.TryGetValue(key, out var action))
                return false;

            action();
            return true;
        }

        public void RegisterCheckoutShortcut(Key key, Action action)
        {
            checkoutShortcuts[key] = action;
        }

        public void Reset()
        {
            ClearBarcodeBuffer();
            checkoutDispatchInProgress = false;
        }

        private bool TryCompleteBarcode(out string barcode)
        {
            barcode = string.Empty;
            if (barcodeBuffer.Length == 0)
                return false;

            var now = DateTime.UtcNow;
            var duration = now - firstCharTimeUtc;
            var idle = now - lastCharTimeUtc;

            barcode = barcodeBuffer.ToString();
            ClearBarcodeBuffer();

            // Fast burst + immediate Enter => scanner stream.
            return duration <= ScanTotalThreshold && idle <= ScanInterKeyThreshold;
        }

        private void RequestCheckout()
        {
            if (checkoutDispatchInProgress)
                return;

            var now = DateTime.UtcNow;
            if (lastCheckoutTimeUtc != DateTime.MinValue && now - lastCheckoutTimeUtc < CheckoutDebounce)
                return;

            checkoutDispatchInProgress = true;
            try
            {
                lastCheckoutTimeUtc = now;
                CheckoutRequested?.Invoke();
            }
            finally
            {
                checkoutDispatchInProgress = false;
            }
        }

        private void ClearBarcodeBuffer()
        {
            barcodeBuffer.Clear();
            firstCharTimeUtc = DateTime.MinValue;
            lastCharTimeUtc = DateTime.MinValue;
        }
    }
}