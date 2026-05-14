# Sales Module — EZPos

---

## Purpose

The Sales module is the primary cashier interface. It handles product lookup by barcode, cart management, checkout, payment, and receipt generation.

---

## Key Files

| File | Role |
|---|---|
| `src/UI/Pages/SalesPage.xaml/.cs` | Main POS page — cart, scanner, checkout |
| `src/UI/Pages/SalesModeControl.xaml/.cs` | Reusable cart control (supports multiple tabs) |
| `src/UI/Dialogs/PaymentDialog.xaml/.cs` | Payment method selection + amount + change calc |
| `src/UI/Dialogs/ReceiptDialog.xaml/.cs` | On-screen receipt display + print |
| `src/UI/Input/SalesKeyboardInputService.cs` | HID barcode scanner vs keyboard disambiguation |
| `src/Business/Services/SaleService.cs` | ProcessSale — DB write + stock deduction |
| `src/Utilities/Helpers/EscPosDocument.cs` | ESC/POS byte builder for thermal printer |
| `src/Utilities/Helpers/RawPrinterHelper.cs` | Win32 P/Invoke raw spooler |

---

## Workflow

```
1. Cashier scans barcode (or types product name in search box)
   ↓
2. SalesKeyboardInputService detects scan (timing-based)
   ↓
3. HandleBarcodeCompleted(barcode)
   ├── Product found in PosStateStore → AddProductToCart(product)
   └── Not found → MessageBox "Barcode not found"
   ↓
4. Cart updates: product row added (or quantity incremented if already in cart)
   ↓
5. Cashier presses checkout hotkey (Enter) or clicks Checkout button
   ↓
6. PaymentDialog opens
   ├── Select payment method (Cash / QR / Card / Cheque)
   ├── Enter tendered amount (cash only)
   └── Confirm → SaleService.ProcessSale()
         ├── INSERT into Sales table
         ├── INSERT SaleItems rows
         ├── INSERT StockMovements (SALE) + UPDATE product stock
         └── Returns SaleResult
   ↓
7. ReceiptDialog opens with SaleResult
   ├── Auto-print if AutoPrint=true in config
   ├── Manual Print button: ESC/POS raw bytes → fallback to WPF PrintVisual
   └── New Sale button (or PageUp hotkey) → reset cart
```

---

## Barcode Scanner Integration

`SalesKeyboardInputService` distinguishes scanner from human typing using:
- **Inter-key threshold:** 60ms — if gap between chars exceeds this, buffer resets
- **Total scan threshold:** 150ms — entire barcode must arrive within this window
- **Enter key:** signals end of barcode stream

**Wiring pattern (SalesPage):**
```csharp
// Attach on page load
hostWindow.PreviewTextInput += HostWindow_PreviewTextInput;
hostWindow.PreviewKeyDown   += HostWindow_PreviewKeyDown;

// Detach on page unload
hostWindow.PreviewTextInput -= HostWindow_PreviewTextInput;
hostWindow.PreviewKeyDown   -= HostWindow_PreviewKeyDown;
```

The scope guard `IsKeyboardShortcutScopeActive()` ensures scanner events are only processed when SalesPage is visible and no text input has focus.

---

## Payment Methods

Configured hotkeys (default, overridable in Settings):
| Method | Default Key |
|---|---|
| Cash | F1 |
| QR | F2 |
| Card | F3 |
| Cheque | F4 |

---

## Receipt Printing

**ESC/POS path (thermal printers):**
1. `EscPosDocument.Build(saleResult, storeName)` → `byte[]`
2. `RawPrinterHelper.SendBytes(printerName, bytes)` → Win32 raw spooler

**Fallback path (PDF / laser / inkjet):**
- WPF `PrintDialog.PrintVisual()` — renders the on-screen receipt panel

**Auto-print:** If `AutoPrint=true` in `config.ini`, `ReceiptDialog_Loaded` fires print automatically.

**No printer configured:** If `PrinterName` is blank in config, shows friendly message — no crash.

---

## Tax Modes

| Mode | Behavior |
|---|---|
| `PerReceipt` | Tax applied to the total once (default) |
| `PerItem` | Tax applied per line item |
| `Fake` | Tax shown on receipt but not charged |

---

## Multi-Tab Sales

`SalesPage` supports multiple simultaneous sale tabs (e.g. serving multiple customers). Each tab has its own cart state via `SalesModeControl`.

---

## Business Rules

- Cart quantity minimum: 1 (cannot go below)
- If barcode not found: message shown, no dialog opened
- Checkout blocked if cart is empty
- `SaleService.ProcessSale()` runs as a single SQLite transaction — partial writes never occur
- Stock is deducted at checkout, not at cart-add time

---

## Edge Cases

| Scenario | Behavior |
|---|---|
| Barcode scanned, product not found | MessageBox "Barcode not found in products" |
| Printer not configured | Warning message shown, no crash |
| ESC/POS printing fails | Silent fallback to WPF PrintVisual |
| Scanner fires while text input focused | `IsKeyboardShortcutScopeActive()` returns false, scanner ignored |
| Duplicate barcode scan | Quantity incremented on existing cart row |
