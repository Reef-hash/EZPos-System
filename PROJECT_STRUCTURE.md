# EZPos System - Project Structure & Architecture Documentation

## 📁 Project Overview

EZPos System is a modern **Point of Sale (POS) application** built with **WPF (.NET 6.0)** using a layered architecture pattern designed to be scalable for SAAS (Software-as-a-Service) multi-user online deployment.

---

## 🏗️ Target Directory Structure

> Files marked `[EXISTS]` are already present. Files marked `[PLANNED]` must be created as part of the phased build plan below.

```
EZPos-System/
├── src/
│   ├── Models/
│   │   └── Domain/
│   │       ├── Product.cs               [EXISTS]  — Category, MaxStock, ReorderLevel, UnitType added
│   │       ├── Sale.cs                  [EXISTS]  — PaymentMethod added
│   │       ├── SaleItem.cs              [EXISTS]
│   │       └── StockMovement.cs         [EXISTS]  — audit trail for stock changes
│   │
│   ├── DataAccess/
│   │   └── Repositories/
│   │       ├── Database.cs              [EXISTS]  — StockMovements table + barcode index in schema
│   │       ├── ConfigHelper.cs          [EXISTS]  — Get/Set/GetKey/SetKey helpers
│   │       ├── ProductRepository.cs     [EXISTS]  — GetAll(), GetByBarcode(), Add(), Update(), Delete()
│   │       ├── SaleRepository.cs        [EXISTS]  — AddSale() called by SaleService, writes audit trail
│   │       └── StockMovementRepository  [EXISTS]  — Insert(), InsertWithConnection(), GetByProduct()
│   │
│   ├── Business/
│   │   └── Services/
│   │       ├── ProductService.cs        [EXISTS]  — Add, Edit, Delete, GetAll, GetByBarcode
│   │       ├── SaleService.cs           [EXISTS]  — ProcessSale → DB write + state sync
│   │       ├── StockService.cs          [EXISTS]  — AdjustStock, GetLowStockItems
│   │       └── ReportService.cs         [EXISTS]  — GetSummary, GetDailyBreakdown, GetTopProducts, GetTodaySummary, GetLowStockAlerts, GetPaymentBreakdown, GetTransactions, GetStockSnapshot
│   │
│   ├── UI/
│   │   ├── State/
│   │   │   └── PosStateStore.cs         [EXISTS]  — Load() from DB on startup, ReloadTaxConfig()
│   │   ├── Navigation/
│   │   │   └── NavigationService.cs     [EXISTS]
│   │   ├── Input/
│   │   │   └── SalesKeyboardInputService.cs  [EXISTS]  — barcode vs Enter disambiguation
│   │   ├── ViewModels/                  [PLANNED — Phase 5 wiring only]
│   │   ├── Dialogs/                     [EXISTS]
│   │   │   ├── ProductDialog.xaml       [EXISTS]  — Add/Edit product form
│   │   │   ├── StockAdjustDialog.xaml   [EXISTS]  — Stock In/Out/Manual adjust
│   │   │   ├── PaymentDialog.xaml       [EXISTS]  — payment method + amount + change
│   │   │   └── ReceiptDialog.xaml       [EXISTS]  — on-screen receipt summary
│   │   ├── Pages/
│   │   │   ├── DashboardPage.xaml       [EXISTS]  — live KPI cards + low stock alerts
│   │   │   ├── SalesPage.xaml           [EXISTS]
│   │   │   ├── ProductsPage.xaml        [EXISTS]
│   │   │   ├── StockPage.xaml           [EXISTS]
│   │   │   ├── ReportsPage.xaml         [EXISTS]  — live data + PDF/Excel export
│   │   │   └── SettingsPage.xaml        [EXISTS]  — store info, tax, printer, hotkeys, DB backup/restore
│   │   ├── Resources/
│   │   └── Styles/
│   │
│   ├── Security/
│   │   ├── Authentication/              [PLANNED — Phase 4]
│   │   └── Authorization/               [PLANNED — Phase 4]
│   │
│   ├── Hardware/                        [PLANNED — Phase 5 only]
│   │   ├── Barcode/
│   │   │   ├── BarcodeService.cs        [PLANNED] — lookup product by scanned barcode
│   │   │   └── BarcodeInputBuffer.cs    [PLANNED] — debounce + timing buffer for HID input
│   │   └── Printer/
│   │       ├── PrinterService.cs        [PLANNED] — async ESC/POS print, retry logic
│   │       ├── ReceiptBuilder.cs        [PLANNED] — formats SaleResult → ESC/POS byte[]
│   │       └── EscPosCommands.cs        [PLANNED] — ESC/POS byte sequence constants
│   │
│   └── Utilities/
│       └── Helpers/
│           ├── EscPosDocument.cs        [EXISTS]  — ESC/POS byte builder (80 mm receipt)
│           └── RawPrinterHelper.cs      [EXISTS]  — Win32 P/Invoke raw spooler wrapper
│
├── Resources/
│   ├── Icons/
│   │   └── app.ico                      [EXISTS]
│   └── Images/
│
├── Config/
│   └── config.ini                       [EXISTS]
│
├── MainWindow.xaml                      [EXISTS]
├── MainWindow.xaml.cs                   [EXISTS]
├── App.xaml                             [EXISTS]
├── App.xaml.cs                          [EXISTS]
├── Program.cs                           [EXISTS]
├── EZPos.csproj                         [EXISTS]
├── InnoSetup-EZPos.iss                  [EXISTS]
└── .github/workflows/build.yml          [EXISTS]
```

---

## 🏛️ Architecture Layers & Responsibility Boundaries

```
VIEW  (XAML + minimal code-behind)
  ✓ Layout, event wiring, display only
  ✗ Must NOT call DB, hold business logic, or mutate state directly
  Rule: code-behind only contains Loaded guard + ViewModel binding

VIEWMODEL
  ✓ Exposes ObservableCollections, Commands, bindable properties
  ✓ Talks to Services only
  ✗ Must NOT call SQLite directly or reference XAML controls

SERVICE LAYER  (Business/Services/)
  ✓ Business rules, validation, orchestration
  ✓ Talks to Repositories only; returns domain objects
  ✗ Must NOT know about UI, ViewModel, or WPF types

REPOSITORY
  ✓ Raw SQL, maps rows → domain objects
  ✗ Must NOT contain business logic

POSSTATE STORE
  ✓ Live in-memory state that the UI binds to (ObservableCollection)
  ✓ Fed by Services after DB operations
  ✗ Must NOT be modified directly from page code-behind

HARDWARE LAYER  (Hardware/) — Phase 5 only
  ✓ All hardware I/O — barcode scanner input, ESC/POS printer commands
  ✓ Plugs into existing Services and ViewModels — does NOT change them
  ✗ Must NOT be referenced by Phases 1–4 code
  ✗ Must NOT contain business logic (pricing, stock rules, tax)
  Rule: system must function completely without this layer
```

---

## 🔄 Data Flow — Single Source of Truth

```
                 ┌─────────────────┐
                 │   SQLite DB     │  ← permanent storage on disk
                 │  (EZPos.db)     │
                 └────────┬────────┘
                          │  read on startup / write on every action
                 ┌────────▼────────┐
                 │  Service Layer  │  ← all business logic lives here
                 │  ProductService │
                 │  SaleService    │
                 │  StockService   │
                 └────────┬────────┘
                          │  returns domain objects
                 ┌────────▼────────┐
                 │  PosStateStore  │  ← single in-memory state
                 │  (ObservableCol)│    drives all UI bindings
                 └────────┬────────┘
                          │  INotifyPropertyChanged
           ┌──────────────┼──────────────┬──────────────┐
           ▼              ▼              ▼              ▼
       SalesPage    ProductsPage     StockPage     ReportsPage

  [Phase 5 only — added on top of stable system above]
  BarcodeInputBuffer → BarcodeService → ProductService.GetByBarcode() → AddToCart()
  SaleService result → PrinterService.PrintReceiptAsync() → ESC/POS thermal printer
```

### What triggers each state update

| User Action | DB Write | PosStateStore Update | Pages Refreshed |
|---|---|---|---|
| App startup | ✗ | Load all products from DB | All pages |
| Add product | INSERT Products | Add to Products list | ProductsPage |
| Edit product | UPDATE Products | Replace item in list | All pages |
| Delete product | DELETE Products | Remove from list | All pages |
| Checkout sale | INSERT Sale + SaleItems, UPDATE Stock | Reduce stock on each item | SalesPage, StockPage |
| Stock In / Out | UPDATE Products.Stock, INSERT StockMovement | Update item stock | StockPage |

---

## 🗺️ User Flow

```
App Startup
  └─► Database.Initialize()
  └─► ProductRepository.GetAll() → PosStateStore.Load()
  └─► Navigate to Dashboard
           │
           ├──► SALES (POS Counter)
           │       Search / filter products → Add to cart
           │       Adjust quantity (+ / -)
           │       Select payment method → Checkout
           │           ├─ SaleService.ProcessSale(cart, paymentMethod)
           │           │     ├─ INSERT Sales row
           │           │     ├─ INSERT SaleItems rows
           │           │     └─ UPDATE Products.Stock -= qty
           │           ├─ PosStateStore: update affected product stocks
           │           ├─ Clear cart
           │           └─ Show ReceiptDialog (on-screen summary)
           │           [Phase 5 only] → PrinterService.PrintReceiptAsync() → thermal printer
           │           [Phase 5 only] → BarcodeInputBuffer → BarcodeService → AddToCart()
           │
           ├──► PRODUCTS (Catalogue)
           │       Search / filter
           │       Add Product → AddProductDialog → ProductService.Add() → DB
           │       Edit Product → AddProductDialog (prefilled) → ProductService.Update() → DB
           │       Delete Product → confirm → ProductService.Delete() → DB
           │
           ├──► STOCK (Inventory)
           │       View KPI cards (In Stock / Low / Out of Stock)
           │       Filter by category / status
           │       Stock In → StockAdjustDialog → StockService.AdjustStock(+qty) → DB
           │       Stock Out → StockAdjustDialog → StockService.AdjustStock(-qty) → DB
           │
           ├──► REPORTS
           │       Date range filter
           │       ReportService.GetSalesByPeriod() → real DB data
           │       ReportService.GetTopProducts() → real DB data
           │       Export to PDF/Excel (Phase 4)
           │
           └──► SETTINGS (Phase 4)
                   Business name, tax rate, currency
                   User management (PIN / login)
```

---

## ✅ Data Layer — Connected & Working

| | Layer A — SQLite (DataAccess/) | Layer B — PosStateStore (in-memory) |
|---|---|---|
| Status | Fully connected — all CRUD operations live | Drives all UI bindings |
| Data | Loaded from DB on startup | Populated by `PosStateStore.Load()` |
| Persistence | ✓ Survives app restarts | ✓ Reloaded from DB on startup |
| Checkout | `SaleRepository.AddSale()` called by `SaleService.ProcessSale()` | Stock synced back on every sale |
| Stock changes | Written to DB + StockMovements audit table | Updated in-memory on every change |

**Phase 1 and Phase 2 are complete. The app is production-ready for sales, product management, and stock adjustment.**

---

## 🗄️ Database Schema

### Current schema (in `Database.cs`)

```sql
Products    (Id, Barcode, Name, Price, Stock)
Sales       (Id, DateTime, TotalAmount)
SaleItems   (Id, SaleId, ProductId, Quantity, Price)
```

### Required additions

```sql
-- Add to Products table:
ALTER TABLE Products ADD COLUMN Category TEXT DEFAULT 'General';
ALTER TABLE Products ADD COLUMN ReorderLevel INTEGER DEFAULT 5;
ALTER TABLE Products ADD COLUMN MaxStock INTEGER DEFAULT 100;
ALTER TABLE Products ADD COLUMN LastUpdated TEXT;

-- New table for audit trail:
CREATE TABLE IF NOT EXISTS StockMovements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    ChangeQty INTEGER NOT NULL,      -- positive = stock in, negative = stock out / sale
    Reason TEXT NOT NULL,            -- 'SALE', 'ADJUSTMENT', 'CORRECTION'
    DateTime TEXT NOT NULL,
    FOREIGN KEY(ProductId) REFERENCES Products(Id)
);

-- Performance index for barcode lookup (Phase 2 — hardware):
CREATE UNIQUE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
```

---

## � Hardware Integration Architecture

### STEP 1 — Status Check

| Hardware | Previously Planned? | Gap |
|---|---|---|
| Barcode Scanner | ⚠️ Stub only (Phase 3.5 — UI task mention) | No service, no HID design, no buffer, no architecture |
| Thermal Printer | ⚠️ Stub only (Phase 4.5 — `PrintHelper.cs`) | No PrinterService, no ESC/POS, no async/retry |

Both are now fully designed below and integrated into the phased build plan.

---

### Barcode Scanner — Design

#### Input Method
Standard retail barcode scanners operate as **HID keyboard-wedge** devices — no driver or SDK required. The scanner emits each character in the barcode as a keystroke sequence followed by a **carriage return (Enter)**. This is compatible with every scanner brand out of the box.

#### Input Capture Strategy

```
Scanner fires keystrokes → WPF Window receives PreviewTextInput + PreviewKeyDown
        │
        ▼
BarcodeInputBuffer
  - Accumulates characters until Enter key detected
  - Rejects input if time between chars > 50ms  ← human typing is slower than a scanner
  - Clears buffer after 200ms of inactivity (safety reset)
        │
        ▼  complete barcode string emitted via event
BarcodeService.LookupProductAsync(string barcode)
        │
        ├─ Calls ProductService.GetByBarcode(barcode)
        ├─ Found → raises ProductFound event with ProductRecord
        └─ Not found → raises ProductNotFound event with barcode string
        │
        ▼
SalesViewModel (subscriber)
  ├─ ProductFound → AddToCart(product)
  └─ ProductNotFound → show inline notice "Barcode XXXX not found"
```

#### C# Interface Design

```csharp
// Hardware/Barcode/BarcodeInputBuffer.cs
public class BarcodeInputBuffer
{
    public event EventHandler<string> BarcodeScanned;   // fires when Enter received
    public void ProcessKeyChar(char c);                 // called from Window.PreviewTextInput
    public void ProcessKeyDown(Key key);                // called from Window.PreviewKeyDown (Enter)
    // Internal: StringBuilder buffer + Stopwatch for timing check
}

// Hardware/Barcode/BarcodeService.cs
public class BarcodeService
{
    public event EventHandler<ProductRecord> ProductFound;
    public event EventHandler<string> ProductNotFound;

    public BarcodeService(BarcodeInputBuffer buffer, ProductService productService);
    // Subscribes to buffer.BarcodeScanned → calls ProductService → raises events
}
```

#### Wiring in MainWindow (app-level capture)

```csharp
// MainWindow.xaml.cs — PreviewTextInput and PreviewKeyDown forwarded to BarcodeInputBuffer
// This ensures scanner input is captured regardless of which control has focus
protected override void OnPreviewTextInput(TextCompositionEventArgs e)
{
    _barcodeBuffer.ProcessKeyChar(e.Text[0]);
    base.OnPreviewTextInput(e);
}
protected override void OnPreviewKeyDown(KeyEventArgs e)
{
    if (e.Key == Key.Enter) _barcodeBuffer.ProcessKeyDown(Key.Enter);
    base.OnPreviewKeyDown(e);
}
```

> **Note:** When SalesPage is active, barcode input adds to cart. On ProductsPage, same scanner fills the barcode field in the AddProductDialog. The ViewModel decides what to do with the `ProductFound` / `BarcodeScanned` event based on which page is active.

#### Database requirement
```sql
CREATE UNIQUE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
```
This ensures `GetByBarcode()` is an O(log n) index scan — response is instant even with 100,000 products.

---

### Thermal Printer — Design

#### Protocol
**ESC/POS** (Epson Standard Code for Printers) is the universal protocol for thermal printers. Supported by: Epson, Star Micronics, Bixolon, SEWOO, and virtually all generic Chinese thermal printers. No special NuGet package required — commands are raw byte sequences sent to the Windows print queue.

#### Print Method: Raw Windows Spooler
Using Win32 `OpenPrinter` / `WritePrinter` via P/Invoke sends raw ESC/POS bytes directly to the printer driver, bypassing GDI rendering. This is the correct method for thermal printers — GDI printing (PrintDocument) does not support ESC/POS cut/feed commands.

#### ESC/POS Command Reference (subset used)

```
Command         Bytes           Purpose
─────────────────────────────────────────────────────
ESC @           1B 40           Initialize / reset printer
ESC a 1         1B 61 01        Center alignment
ESC a 0         1B 61 00        Left alignment
ESC E 1         1B 45 01        Bold ON
ESC E 0         1B 45 00        Bold OFF
ESC ! 0x30      1B 21 30        Double height + double width (logo text)
ESC ! 0x00      1B 21 00        Normal font
LF              0A              Line feed (new line)
GS V 1          1D 56 01        Partial cut (leave small tab)
GS V 0          1D 56 00        Full cut
```

#### Receipt Layout

```
================================
       [BUSINESS NAME]
    [Address] | [Phone]
================================
Ref: #0042       2026-05-02 14:30
--------------------------------
Item                  Qty   Price
--------------------------------
Nescafe 3in1 10pk      2   10.00
Milo Tin 1kg           1   25.00
--------------------------------
              Subtotal:   35.00
               Tax (6%):   2.10
             GRAND TOTAL: 37.10
================================
    Payment: CASH
    Tendered: 50.00
    Change:   12.90
================================
        Thank you! Come again
================================
            [cut]
```

#### C# Interface Design

```csharp
// Hardware/Printer/EscPosCommands.cs
public static class EscPosCommands
{
    public static readonly byte[] Initialize   = { 0x1B, 0x40 };
    public static readonly byte[] AlignCenter  = { 0x1B, 0x61, 0x01 };
    public static readonly byte[] AlignLeft    = { 0x1B, 0x61, 0x00 };
    public static readonly byte[] BoldOn       = { 0x1B, 0x45, 0x01 };
    public static readonly byte[] BoldOff      = { 0x1B, 0x45, 0x00 };
    public static readonly byte[] DoubleSize   = { 0x1B, 0x21, 0x30 };
    public static readonly byte[] NormalSize   = { 0x1B, 0x21, 0x00 };
    public static readonly byte[] PartialCut   = { 0x1D, 0x56, 0x01 };
    public static readonly byte[] FeedLine     = { 0x0A };
}

// Hardware/Printer/ReceiptBuilder.cs
public class ReceiptBuilder
{
    // Accepts ReceiptData, returns byte[] of ESC/POS commands
    public byte[] Build(ReceiptData receipt);
    private byte[] EncodeText(string text);
    private string FormatLine(string left, string right, int totalWidth = 32);
}

// Hardware/Printer/PrinterService.cs
public class PrinterService
{
    private readonly string _printerName;    // from Settings/config.ini
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 500;

    public Task<PrintResult> PrintReceiptAsync(ReceiptData receipt);
    private bool SendRawBytes(byte[] data);  // Win32 OpenPrinter + WritePrinter
}

public record PrintResult(bool Success, string ErrorMessage = null);
```

#### ReceiptData model

```csharp
public class ReceiptData
{
    public string BusinessName { get; set; }
    public string Address      { get; set; }
    public int    SaleId       { get; set; }
    public DateTime DateTime   { get; set; }
    public List<ReceiptLine> Lines { get; set; }
    public decimal Subtotal    { get; set; }
    public decimal TaxRate     { get; set; }
    public decimal TaxAmount   { get; set; }
    public decimal GrandTotal  { get; set; }
    public string PaymentMethod{ get; set; }
    public decimal Tendered    { get; set; }
    public decimal Change      { get; set; }
}

public class ReceiptLine
{
    public string ProductName  { get; set; }
    public int    Quantity     { get; set; }
    public decimal UnitPrice   { get; set; }
    public decimal LineTotal   { get; set; }
}
```

#### Async print flow (no UI freeze)

```
SaleService.ProcessSale() returns SaleResult
        │
        ▼
SalesViewModel (on UI thread)
  ├─ Show ReceiptDialog (sync — user sees it instantly)
  └─ _ = PrinterService.PrintReceiptAsync(receiptData)  ← fire-and-forget Task
            │
            ├─ Attempt 1: SendRawBytes()
            ├─ Failed → wait 500ms → Attempt 2
            ├─ Failed → wait 500ms → Attempt 3
            └─ All failed → Log error to file, show non-blocking toast notification
                            (do NOT throw exception to UI)
```

#### Printer name configuration
Printer name is stored in `Config/config.ini`:
```ini
[Printer]
Name=POS-80C
```
`PrinterService` reads this via `ConfigHelper` at startup. If blank, printing is skipped gracefully (no crash). This allows running the app without a printer connected during development.

---

### Hardware Layer Integration Map

```
src/Hardware/
├── Barcode/
│   ├── BarcodeInputBuffer.cs   — timing-aware HID input accumulator
│   └── BarcodeService.cs       — barcode → ProductService → events
└── Printer/
    ├── EscPosCommands.cs        — ESC/POS byte constant definitions
    ├── ReceiptBuilder.cs        — SaleResult → formatted ESC/POS byte[]
    └── PrinterService.cs        — async raw print + retry + config-driven printer name
```

**Integration points:**

| Hardware Component | Called By | Calls Into |
|---|---|---|
| `BarcodeInputBuffer` | `MainWindow` (PreviewTextInput / PreviewKeyDown) | `BarcodeService` |
| `BarcodeService` | `SalesViewModel`, `ProductsViewModel` | `ProductService.GetByBarcode()` |
| `ReceiptBuilder` | `PrinterService` | (pure builder — no dependencies) |
| `PrinterService` | `SalesViewModel` (after checkout) | Win32 print spooler (P/Invoke) |

---

### ✅ Phase 0 — Foundation (COMPLETE)
- [x] All tab crashes fixed (Sales, Products, Stock, Reports)
- [x] Dark UI theme — ComboBox, DataGrid, buttons fully styled
- [x] App icon generated and wired in
- [x] InnoSetup installer script (production-grade)
- [x] GitHub Actions CI/CD producing installer artifact
- [x] `DispatcherUnhandledException` global error handler

---

### 🔴 Phase 1 — Core Stability Layer ✅ COMPLETE

**Goal:** App reads from and writes to SQLite. Services and MVVM structure are solid. No hardware dependencies anywhere.

| # | Task | File(s) | Status |
|---|---|---|---|
| 1.1 | Migrate `Product.cs` — add Category, ReorderLevel, MaxStock, LastUpdated | `Models/Domain/Product.cs` | ✅ Done |
| 1.2 | Update DB schema — add new columns + StockMovements table + barcode index | `DataAccess/Repositories/Database.cs` | ✅ Done |
| 1.3 | Create `StockMovement.cs` domain model | `Models/Domain/StockMovement.cs` | ✅ Done |
| 1.4 | Complete `ProductRepository` — GetAll, GetByBarcode, Add, Update, Delete | `DataAccess/Repositories/ProductRepository.cs` | ✅ Done |
| 1.5 | Create `StockMovementRepository` — Insert, GetByProduct | `DataAccess/Repositories/StockMovementRepository.cs` | ✅ Done |
| 1.6 | Create `ProductService` — wraps repository, updates PosStateStore | `Business/Services/ProductService.cs` | ✅ Done |
| 1.7 | Create `SaleService` — ProcessSale calls SaleRepository + updates store | `Business/Services/SaleService.cs` | ✅ Done |
| 1.8 | Create `StockService` — AdjustStock, GetLowStockItems | `Business/Services/StockService.cs` | ✅ Done |
| 1.9 | `PosStateStore.Load()` — populate from DB on app startup | `UI/State/PosStateStore.cs` | ✅ Done |
| 1.10 | Wire `Database.Initialize()` + `PosStateStore.Load()` into `App.xaml.cs` | `App.xaml.cs` | ✅ Done |

---

### 🟡 Phase 2 — Core Business Logic ✅ COMPLETE

**Goal:** Users can add/edit/delete products, adjust stock, and complete a sale end-to-end. No printer or scanner dependency — checkout works fully without hardware.

| # | Task | File(s) | Status |
|---|---|---|---|
| 2.1 | `ProductDialog` — Add/Edit form: name, barcode, price, category, stock, reorder level | `UI/Dialogs/ProductDialog.xaml` | ✅ Done |
| 2.2 | Wire Add/Edit buttons in ProductsPage to `ProductDialog` + `ProductService` | `UI/Pages/ProductsPage.xaml.cs` | ✅ Done |
| 2.3 | Wire Delete in ProductsPage to `ProductService.Delete()` — real DB delete | `UI/Pages/ProductsPage.xaml.cs` | ✅ Done |
| 2.4 | `StockAdjustDialog` — Stock In/Out/Manual with quantity + reason + live preview | `UI/Dialogs/StockAdjustDialog.xaml` | ✅ Done |
| 2.5 | Wire Stock In / Stock Out buttons to `StockAdjustDialog` + `StockService` | `UI/Pages/StockPage.xaml.cs` | ✅ Done |
| 2.6 | Wire `SaleService.ProcessSale()` into `SalesPage` checkout button | `UI/Pages/SalesPage.xaml.cs` | ✅ Done |
| 2.7 | `ReceiptDialog` — on-screen transaction summary: items, totals, payment, change | `UI/Dialogs/ReceiptDialog.xaml` | ✅ Done |
| 2.8 | Wire ReceiptDialog into `SalesPage` checkout completion | `UI/Pages/SalesPage.xaml.cs` | ✅ Done |

---

### 🟢 Phase 3 — Data Flow & Reporting ✅ COMPLETE

**Goal:** Real numbers everywhere. Dashboard shows live data. No hardcoded arrays.

| # | Task | File(s) | Status |
|---|---|---|---|
| 3.1 | `ReportService` — GetSummary, GetDailyBreakdown, GetTopProducts, GetTodaySummary, GetHourlySales, GetLowStockAlerts, GetPaymentBreakdown, GetTransactions, GetStockSnapshot | `Business/Services/ReportService.cs` | ✅ Done |
| 3.2 | Wire `ReportService` into `ReportsPage` — real DB data, date range filter, Excel export | `UI/Pages/ReportsPage.xaml.cs` | ✅ Done |
| 3.3 | `DashboardPage` — today's revenue, order count, avg order, low stock alerts grid | `UI/Pages/DashboardPage.xaml` | ✅ Done |
| 3.4 | Register DashboardPage in `NavigationService` + nav button in MainWindow (default route) | `UI/Navigation/NavigationService.cs`, `MainWindow.xaml` | ✅ Done |
| 3.5 | Stock deduction from sales accurate in DB (`SaleRepository`) and `PosStateStore` (`SaleService`) | `Business/Services/SaleService.cs` | ✅ Done |
| 3.6 | `StockMovement` audit trail written on every sale (`SaleRepository`) and every manual adjustment (`StockService`) | `DataAccess/Repositories/SaleRepository.cs` | ✅ Done |

---

### 🔵 Phase 4 — UI Polish

**Goal:** System is consistent, usable, and visually solid. Fix all layout/spacing issues before adding hardware complexity.

| # | Task | File(s) | Status |
|---|---|---|---|
| 4.1 | Settings page — store name, receipt footer, printer name, tax rate/mode, currency, keyboard shortcuts, DB backup/restore | `UI/Pages/SettingsPage.xaml` | ✅ Done |
| 4.2 | Category management (add/rename/delete categories) — `CategoryManagementDialog`, `CategoryRepository`, `CategoryService`; categories stored in DB; `ProductDialog` and `SalesPage` filter load dynamically; Products toolbar has "Categories" button | `UI/Dialogs/`, `Business/Services/`, `DataAccess/Repositories/` | ✅ Done |
| 4.3 | Review all page layouts — fix spacing, padding, alignment consistency | All Pages | ✅ Done |
| 4.4 | Export reports to PDF from `ReportsPage` alongside Excel export | `UI/Pages/ReportsPage.xaml.cs` | ✅ Done |
| 4.5 | Licensing architecture — `ILicenseService`, `LicenseService` (mock, always Valid), `LicenseInfo`, `FileLicenseStorage`, `LicenseRequiredWindow`; startup check wired in `App.xaml.cs`; real API/Stripe integration left as TODO | `Core/Licensing/`, `Infrastructure/Licensing/`, `UI/Licensing/` | TODO (mock done, real validation pending) |
| 4.6 | DB backup & restore from Settings — backup current `EZPos.db`, validate restore file, auto-create pre-restore safety backup, then restart app | `UI/Pages/SettingsPage.xaml(.cs)` | ✅ Done |

> **Nice to have (future client request):** Role-based access control (Admin vs Cashier) — `Security/Authorization/`. Not in scope for current build. Add if client requests multi-operator restrictions.

---

## 🔄 Update System Architecture *(Professional Self-Update)*

### Design Decision: Option 1 — In-App Updater + Hosted Installer

**Why this approach was chosen:**
- ✅ Works seamlessly with current Inno Setup installer pipeline
- ✅ Preserves SQLite database across updates (critical for offline POS)
- ✅ Professional but low-complexity for current deployment model
- ✅ Aligns with GitHub Releases CI/CD workflow already in place
- ✅ Supports Windows 7+ targets (no MSIX or modern package dependencies)
- ✅ Gives users control over update timing (never interrupts active sale)

**Rejected alternatives:**
- ❌ ClickOnce — too rigid for POS, doesn't feel professional, poor control
- ❌ MSIX / App Installer — requires Win10+, incompatible with net6.0-windows7.0 target
- ❌ Fully custom backend — overkill for current stage, defer to Phase 6 if licensing/subscriptions added later

### Data Storage Architecture

**Before:** Mutable data (DB, config) lives next to .exe in install folder
**After:** Mutable data lives in `%ProgramData%\EZPos\` (Windows standard)

```
Program Files\EZPos\          ← Read-only binaries
  ├── EZPos.exe
  ├── ClosedXML.dll
  ├── FontAwesome.Sharp.dll
  ├── System.Data.SQLite.dll
  └── ... (other runtime DLLs)

%ProgramData%\EZPos\          ← Read-write app data (survives updates)
  ├── EZPos.db              ← Live transaction database
  ├── config.ini            ← Store name, tax rate, printer, hotkeys
  ├── license.dat           ← License key file (when licensing is real)
  ├── Backups/              ← DB backup copies
  │   ├── EZPos_Backup_20260506_120000.db
  │   └── EZPos_PreRestore_20260506_130000.db
  └── Logs/                 ← Optional app logs (future)
```

**Rationale:**
- Updates can safely replace binaries without risking data corruption
- Backup/Restore operations work from a standard, predictable location
- Follows Windows best practices (system folder separation)
- Cleaner permissions: `Program Files` is read-only, `%ProgramData%` is read-write
- Makes it obvious to IT/support where "live data" lives

### Update Flow — End-to-End

```
1. User Action (or Auto-Check)
   └─► Settings → About → "Check for Updates" button
       OR: App checks on startup (configurable)

2. Version Check
  App calls: GET https://reef-hash.github.io/EZPos-Update-System/latest.json
   
   Response:
   {
     "version": "1.0.1",
     "changesSince": "1.0.0",
     "releaseNotes": "Fixed: category import bug; Added: PDF report export",
     "downloadUrl": "https://github.com/releases/download/v1.0.1/EZPos-Setup-v1.0.1.exe",
     "checksum": "sha256:abc123def456...",
     "mandatory": false,
     "minimumVersion": "1.0.0",  ← older versions must update if they are below this
     "publishedDate": "2026-05-06T10:30:00Z"
   }

3. Comparison
   Local version: 1.0.0
   Remote version: 1.0.1
   
   IF remote > local:
     SHOW update dialog with changelog
     ALLOW user to "Update Now" or "Skip for now"
   ELSE:
     SHOW "You are on the latest version"

4. Download & Prepare (if user clicks "Update Now")
   - Download installer to: %TEMP%\EZPos-Setup-v1.0.1.exe
   - Verify checksum matches remote checksum
   - Create safety backup: %ProgramData%\EZPos\Backups\EZPos_PreUpdate_[timestamp].db

5. Exit & Update
   - Close all open sales (prompt user to finish checkout)
   - Close app cleanly
   - Run installer silently or semi-silently:
     EZPos-Setup-v1.0.1.exe /SILENT /NORESTART
   - Installer detects existing EZPos.db and skips overwrite (Inno flag: onlyifdoesntexist)

6. Post-Update Launch
   - User manually restarts EZPos.exe OR installer auto-launches
   - App detects DB schema version in PRAGMA schema_version
   - IF schema changed: run migration in Database.cs
   - App loads with updated binaries + preserved data
   - Migration succeeds or rolls back to pre-update backup

7. Verify & Clean Up
   - User verifies everything looks right
   - Old %TEMP% installer can be deleted (or cleaned by Windows)
   - Pre-update backup stays in %ProgramData%\EZPos\Backups\ for manual recovery if needed
```

### Implementation Components

**Phase 4.7 — Data Migration (Prerequisite)**
- Move `EZPos.db` initialization from `AppDomain.CurrentDomain.BaseDirectory` to `%ProgramData%\EZPos\`
- Move `config.ini` location to `%ProgramData%\EZPos\`
- Move `license.dat` location to `%ProgramData%\EZPos\`
- Update Inno Setup to create `%ProgramData%\EZPos\` with correct permissions
- Add migration logic in `App.xaml.cs` to detect old location and copy data on first run

**Phase 4.8 — Updater Service & UI**
- Create `UpdaterService.cs` in `src/Business/Services/`
- Implement version check: `async Task<UpdateManifest> CheckForUpdatesAsync()`
- Implement download with checksum: `async Task DownloadInstallerAsync(string url, string checksum)`
- Add "Check for Updates" button in Settings About section
- Add update dialog showing version, changelog, and prompts
- Add "Update Now" / "Skip" / "Remind Later" buttons
- Handle mandatory updates (block app if client is too old, show warning)
- Resolve current app version dynamically from assembly metadata (no hardcoded version strings)

**Phase 4.9 — Update Manifest Hosting**
- Create/host `latest.json` on GitHub Releases OR your server
- GitHub Actions build should publish `latest.json` alongside installer
- Format: include version, download URL, checksum, release notes, mandatory flag
- Support rollback: older versions can check and refuse to run if below `minimumVersion`

**Phase 4.10 — Database Schema Versioning**
- Add `PRAGMA schema_version` or custom `_schema_info` table tracking
- On app startup after update, check if schema changed
- If schema version is newer: run migration scripts in Database.cs
- If migration fails: restore from pre-update backup and show error

### Files Involved

```
src/Business/Services/UpdaterService.cs       [NEW]  — version check, download, verification
src/UI/Dialogs/UpdateAvailableDialog.xaml    [NEW]  — update prompt with changelog
src/UI/Pages/SettingsPage.xaml.cs            [EDIT] — add "Check for Updates" handler
src/DataAccess/Repositories/Database.cs      [EDIT] — add schema version + migration support
src/App.xaml.cs                               [EDIT] — add %ProgramData% folder detection + data migration
Config/config.ini                             [EDIT] — document new ProgramData location
InnoSetup-EZPos.iss                           [EDIT] — create %ProgramData% folder, set permissions
.github/workflows/build.yml                   [EDIT] — generate latest.json after build
```

### Data Migration Strategy (App Startup)

```csharp
// In App.xaml.cs Constructor or App_Startup
private void MigrateToNewDataLocation()
{
    // Old location: next to .exe
    var oldDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EZPos.db");
    var oldConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
    
    // New location: %ProgramData%\EZPos\
    var programDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EZPos");
    var newDbPath = Path.Combine(programDataDir, "EZPos.db");
    var newConfigPath = Path.Combine(programDataDir, "config.ini");
    
    // Ensure new directory exists
    Directory.CreateDirectory(programDataDir);
    
    // If old location has DB and new location doesn't: copy (one-time migration)
    if (File.Exists(oldDbPath) && !File.Exists(newDbPath))
    {
        File.Copy(oldDbPath, newDbPath);
    }
    
    // Same for config
    if (File.Exists(oldConfigPath) && !File.Exists(newConfigPath))
    {
        File.Copy(oldConfigPath, newConfigPath);
    }
    
    // Update Database.DbFile to new location
    Database.DbFile = newDbPath;
    ConfigHelper.ConfigPath = newConfigPath;
}
```

### Version Manifest Format (latest.json)

```json
{
  "version": "1.0.1",
  "name": "EZPos v1.0.1",
  "publishedDate": "2026-05-06T10:30:00Z",
  "releaseNotes": "• Fixed: category import bug when merging duplicates\n• Added: PDF export for analytics reports\n• Improved: database backup/restore safety with pre-restore snapshots",
  "downloadUrl": "https://github.com/YourOrg/EZPos/releases/download/v1.0.1/EZPos-Setup-v1.0.1.exe",
  "checksum": {
    "algorithm": "sha256",
    "value": "abc123def456789..."
  },
  "mandatory": false,
  "minimumVersion": "1.0.0",
  "targetFramework": "net6.0-windows7.0",
  "updatedComponents": {
    "binaries": true,
    "schema": false
  }
}
```

### Configuration (config.ini)

Add these keys to track versions and update behavior:

```ini
[App]
Version=1.0.0
LastUpdateCheck=2026-05-06T10:30:00Z
UpdateChannel=stable          ; stable, beta, dev (for future phased rollouts)
AutoCheckUpdates=true         ; check on app startup
UpdateNotificationStyle=popup ; popup or notification

; App uses a flat key format in current ConfigHelper implementation:
App:UpdateManifestUrl=https://reef-hash.github.io/EZPos-Update-System/latest.json
; Leave blank to disable auto-update and show "Updates Disabled"
```

Current hosted manifest in use for development/testing:

```text
GitHub Pages URL:
https://reef-hash.github.io/EZPos-Update-System/latest.json

GitHub blob URL (DO NOT use in app config):
https://github.com/Reef-hash/EZPos-Update-System/blob/main/latest.json
```

Why the Pages URL is preferred:
- The updater downloads JSON directly with `HttpClient`
- GitHub `blob` URLs return an HTML page, not raw JSON
- GitHub Pages gives a stable public manifest URL that is cleaner for production use
- `raw.githubusercontent.com` is acceptable as a temporary fallback, but Pages is the intended hosted endpoint

### Important Notes for Developers

1. **DB Preservation is Sacred** — Never, ever overwrite `EZPos.db` during install/update. Our Inno flag `onlyifdoesntexist` enforces this, but verify in review.

2. **Schema Versioning Required** — If you add/remove/rename DB columns in a future version, increment `PRAGMA schema_version` and write a migration in `Database.cs`. Failing to do this = corrupted data on update.

3. **Always Backup Before Update** — The updater creates `EZPos_PreUpdate_[timestamp].db` automatically. If migration fails, users can manually restore via Settings → Database Maintenance.

4. **Checksum Verification is Mandatory** — Never skip SHA256 verification. It's your defense against corrupted downloads or man-in-the-middle attacks. Even in private networks, verify.

5. **Mandatory Update Logic** — `minimumVersion` field blocks old clients from running if they fall below a security threshold. Use sparingly, only for critical bugs.

6. **No Updates During Active Sale** — In the update dialog, check `SalesPage` state. If active, show "Finish current sale before updating" and block Update button.

### Release & Update SOP (After Push / After Update)

Use this checklist every time you push release-related code.

#### A) After Any Push to `main`

1. Confirm local quality gates before push:
  - `dotnet restore`
  - `dotnet build --configuration Debug`
  - `dotnet build --configuration Release`

2. Push your branch and verify GitHub Actions passed:
  - Open Actions tab and confirm workflow succeeded
  - Confirm installer artifact/release was created

3. Verify release metadata is correct:
  - Confirm tag/version naming matches intended release
  - Confirm release title/changelog is readable for clients
  - Confirm installer filename and version are aligned

4. Update hosted `latest.json` manifest (required for in-app updater):
  - Set `version` to new app version
  - Set `downloadUrl` to the new installer asset URL
  - Set `releaseNotes` to user-facing change notes
  - Set `minimumVersion` only if update is mandatory
  - Set `mandatory` true only for critical/security cases

5. Generate and publish checksum:
  - Compute installer SHA256
  - Put checksum into `latest.json`
  - Re-check checksum string matches exactly

6. Smoke-test updater end-to-end on one staging machine:
  - Launch current old version
  - Click Settings -> About -> Check for Updates
  - Verify update dialog appears with correct version/notes
  - Click Update Now and complete install
  - Verify app restarts and data is preserved

#### B) Client Update Procedure (Ops / Support)

1. Before update:
  - Ensure cashier is not in an active sale
  - Trigger manual backup: Settings -> Database Maintenance -> Backup Database
  - Confirm backup file exists in a safe location

2. Run update:
  - Open Settings -> About -> Check for Updates
  - Review release notes
  - Click Update Now
  - App downloads installer, creates pre-update backup, and exits
  - Installer runs and updates binaries only

3. After update:
  - Re-open app and verify login/startup is normal
  - Verify product list loads
  - Create a test sale and complete payment
  - Verify receipt/print flow
  - Verify reports page can load today's data

4. Data integrity checks:
  - Confirm `%ProgramData%\EZPos\EZPos.db` still exists and timestamp is valid
  - Confirm `%ProgramData%\EZPos\Backups\EZPos_PreUpdate_*.db` was created
  - Confirm store settings still present (`StoreName`, `TaxRate`, printer)

5. Rollback procedure (if issue found):
  - Close EZPos
  - Use Settings -> Database Maintenance -> Restore from Backup
  - Select latest pre-update backup
  - Re-launch app and verify restored state

#### C) Developer Checklist After Any Update Implementation

1. Update version in installer/config/docs consistently.
2. If schema changed, add migration script and test with real old database.
3. Confirm Inno Setup still uses `onlyifdoesntexist` for DB seed.
4. Confirm updater checksum verification is not bypassed.
5. Add release notes entry in project docs/changelog.
6. Push only after staging verification passes.

#### D) CI/CD Release Automation (Implemented)

GitHub Actions now follows semantic versioned release flow on tag pushes (`v*`):

1. Resolve app version from release tag (example: `v1.0.1` -> `1.0.1`)
2. Build installer with Inno Setup using injected `AppVersion`
3. Output installer as `EZPos-Setup-v<version>.exe`
4. Generate `latest.json` automatically with:
  - `version`
  - `downloadUrl`
  - `checksum.sha256`
  - `publishedDate`
5. Publish installer and `latest.json` to GitHub Release assets
6. Optionally sync `latest.json` into hosted manifest repository (`Reef-hash/EZPos-Update-System`) when secret `UPDATE_MANIFEST_REPO_TOKEN` is configured

Result: installer filename, release tag, manifest version, and app-reported version are aligned by design.

#### E) Auto-Tag from `EZPos.csproj` Version (Implemented)

Tag creation is now automated via workflow:

- Workflow file: `.github/workflows/auto-tag-from-csproj.yml`
- Trigger: push to `main` when `EZPos.csproj` changes
- Logic:
  1. Read `<Version>` from `EZPos.csproj`
  2. Build tag name as `v<Version>` (example: `v1.0.1`)
  3. If tag does not exist, create and push it automatically
  4. If tag already exists, skip safely (no duplicate tag)

What is automatic now:
1. You update `EZPos.csproj` version and push to `main`
2. Auto-tag workflow creates `v<Version>`
3. Release workflow detects that tag and runs installer + manifest publish

What is NOT automatic:
1. If `EZPos.csproj` version is unchanged, no new tag is created
2. If you want emergency re-release with same version, you must bump version first (recommended) instead of reusing tag

### Workflow Summary (System vs Developer)

#### 1) System Runtime Workflow (What the app does)

1. User clicks Settings -> About -> Check for Updates.
2. App reads manifest URL from `App:UpdateManifestUrl` in `config.ini`.
3. App sends HTTP GET to hosted `latest.json`.
4. App compares `latest.json.version` with local assembly version.
5. If no newer version: show "You are on the latest version".
6. If newer version:
  - Show update dialog (version + release notes)
  - On Update Now: download installer to `%TEMP%`
  - Verify SHA256 checksum
  - Create pre-update DB backup in `%ProgramData%\EZPos\Backups\`
  - Launch installer silently and close app
7. After install: app restarts with updated binaries while keeping `%ProgramData%\EZPos\EZPos.db` intact.

#### 2) Developer Release Workflow (What devs do)

1. Update app version in `EZPos.csproj` (semantic versioning).
2. Commit and push code to `main`.
3. Auto-tag workflow creates and pushes release tag (example: `v1.0.1`) when version changed.
4. GitHub Actions release pipeline then runs automatically:
  - Builds release binaries
  - Builds installer with injected `AppVersion`
  - Computes SHA256
  - Generates `latest.json`
  - Publishes installer + `latest.json` to GitHub Release
  - Optionally syncs `latest.json` to `Reef-hash/EZPos-Update-System` (if secret exists)
5. Verify hosted manifest URL returns updated JSON.
6. Smoke-test updater from an older installed build.

#### 3) Secrets/Infra Required for Full Automation

1. `UPDATE_MANIFEST_REPO_TOKEN` secret in main repo (PAT with push access to `Reef-hash/EZPos-Update-System`).
2. GitHub Pages enabled in `Reef-hash/EZPos-Update-System`.
3. App config points to:
  - `https://reef-hash.github.io/EZPos-Update-System/latest.json`

### EZPos.csproj Update Guide (What To Change Per Release)

File reference: `EZPos.csproj`

Purpose of this file:
- `EZPos.csproj` is the .NET project definition for the desktop app.
- It controls build target, app metadata, package dependencies, embedded resources, and publish content.
- CI/CD and installer output versioning depend on values from this file (especially `Version`).

#### PropertyGroup Meaning (Current Project)

1. `<OutputType>WinExe</OutputType>`
- Builds a Windows GUI executable (no console window).
- Keep as-is for POS desktop app.

2. `<TargetFramework>net6.0-windows7.0</TargetFramework>`
- Targets .NET 6 with Windows 7 API floor.
- Change only when deliberately upgrading runtime support policy.

3. `<UseWPF>true</UseWPF>`
- Enables WPF compile targets.
- Must remain true for this UI architecture.

4. `<RootNamespace>EZPos</RootNamespace>`
- Default root namespace for generated code.
- Do not change unless performing a full namespace migration.

5. `<Nullable>enable</Nullable>`
- Enables nullable reference type analysis.
- Keep enabled (quality guardrail).

6. `<ApplicationIcon>Resources\\Icons\\app.ico</ApplicationIcon>`
- Executable icon used by Windows shell.
- Change only when branding icon is updated.

7. `<AssemblyTitle>`, `<Product>`, `<Company>`, `<Copyright>`
- Assembly metadata shown in file properties/add-remove programs.
- Update only for branding/legal changes.

8. `<Version>1.0.0</Version>`
- Main semantic app version.
- Updater comparison and release alignment should follow this value.
- Increase on every production release.

9. `<FileVersion>1.0.0.0</FileVersion>`
- Windows file version metadata.
- Keep aligned with `Version` (4-part format).

#### ItemGroup Meaning

1. `<PackageReference ... />`
- NuGet dependencies used by the app.
- Update only when intentionally upgrading libraries.
- Always run full build + smoke test after package upgrades.

2. `<Resource Include="Resources\\Icons\\app.ico" />`
- Embeds icon resource into build output.
- Keep path valid if icon is renamed/moved.

3. `<Content Include="EZPos.db"> ... </Content>`
- Ensures seed DB is copied to output/publish artifacts.
- Installer uses `onlyifdoesntexist`, so existing client DB is preserved.
- Do not remove unless installer/data strategy is redesigned.

### Release-Time Changes in EZPos.csproj

For each new release (example 1.0.0 -> 1.0.1):

1. Update `<Version>` to new semantic version.
2. Update `<FileVersion>` to matching 4-part value.
3. Keep framework and WPF settings unchanged unless this release explicitly upgrades platform support.
4. If package versions changed, verify no runtime regressions in sales/payment/report flows.

### Versioning Rules (Team Standard)

1. Patch (`x.y.Z`): bug fixes, no breaking behavior.
2. Minor (`x.Y.z`): new features, backward compatible.
3. Major (`X.y.z`): breaking behavior or migration-heavy changes.

Tagging and CI alignment:
1. Git tag must match csproj version (example csproj `1.0.1` -> tag `v1.0.1`).
2. Installer output version must match same semantic version.
3. Hosted `latest.json` version must match same semantic version.

If versions diverge across these 3 artifacts, updater behavior becomes inconsistent.

---

**Why hardware is last:** Every hardware component depends on a stable, fully connected system. `BarcodeService` must call a working `ProductService`. `PrinterService` must receive a valid `SaleResult` from a working `SaleService`. Wiring hardware into an unstable or partially connected system creates hidden failures that are hard to diagnose — it is never the cause of a bug, but it always amplifies them. Hardware is added last to a system that is already fully functional without it.

**Goal:** Plug hardware into an already fully working system. If the scanner or printer fails, the system continues to operate normally.

| # | Task | File(s) | Status |
|---|---|---|---|
| 5.1 | `EscPosCommands.cs` — ESC/POS byte constant definitions | `Hardware/Printer/EscPosCommands.cs` | ⏳ Foundation in `Utilities/Helpers/EscPosDocument.cs` — move/refactor to Hardware layer |
| 5.2 | `ReceiptBuilder.cs` — formats `SaleResult` → ESC/POS byte array | `Hardware/Printer/ReceiptBuilder.cs` | ⏳ Foundation in `Utilities/Helpers/EscPosDocument.cs` — move/refactor to Hardware layer |
| 5.3 | `PrinterService.cs` — async raw print via Win32 spooler, 3-attempt retry, graceful fallback | `Hardware/Printer/PrinterService.cs` | ⏳ P/Invoke layer in `Utilities/Helpers/RawPrinterHelper.cs` — PrinterService wrapper with retry still needed |
| 5.4 | Add `PrinterName=` to `config.ini` — skip print gracefully if blank | `Config/config.ini` | ✅ Done — `PrinterName=` key present, Settings page exposes it with Detect button |
| 5.5 | Wire `PrinterService.PrintReceiptAsync()` into `SalesViewModel` after checkout | `UI/ViewModels/SalesViewModel.cs` | TODO |
| 5.6 | `BarcodeInputBuffer.cs` — HID timing-aware input accumulator | `Hardware/Barcode/BarcodeInputBuffer.cs` | TODO |
| 5.7 | `BarcodeService.cs` — barcode → `ProductService.GetByBarcode()` → events | `Hardware/Barcode/BarcodeService.cs` | TODO |
| 5.8 | Wire `BarcodeInputBuffer` into `MainWindow` PreviewTextInput/PreviewKeyDown | `MainWindow.xaml.cs` | TODO |
| 5.9 | Wire `BarcodeService.ProductFound` event into `SalesViewModel.AddToCart()` | `UI/ViewModels/SalesViewModel.cs` | TODO |
| 5.10 | Wire `BarcodeService.BarcodeScanned` event into `AddProductDialog` barcode field | `UI/Dialogs/AddProductDialog.xaml.cs` | TODO |

---

### Color Scheme - Modern Minimal Dark Mode
- **Primary**: `#00D9FF` (Cyan)
- **Primary Dark**: `#00B8D4` (Teal)
- **Sidebar**: `#0F172A` (Dark Blue)
- **Content**: `#1E293B` (Dark Gray)
- **Cards**: `#334155` (Medium Gray)
- **Text Primary**: `#F1F5F9` (Light Gray)
- **Text Secondary**: `#94A3B8` (Medium Gray)
- **Success**: `#10B981` (Green)
- **Warning**: `#F59E0B` (Orange)
- **Error**: `#FFEF4444` (Red)

### Icons
- **Library**: FontAwesome.Sharp v6.3.0
- **Icons Used**:
  - Sales: `DollarSign`
  - Products: `Box`
  - Stock: `Warehouse`
  - Reports: `ChartLine`
  - Settings: `Cog`
  - Search: `MagnifyingGlass`
  - Edit: `PencilAlt`
  - Delete: `TrashAlt`
  - Refresh: `RotateRight`

---

## 📦 NuGet Dependencies

```xml
<ItemGroup>
    <PackageReference Include="System.Data.SQLite" Version="1.0.117" />
    <PackageReference Include="FontAwesome.Sharp" Version="6.3.0" />
</ItemGroup>
```

- **System.Data.SQLite** — database persistence (SQLite3)
- **FontAwesome.Sharp** — professional icon library for WPF

> **Thermal printer** does NOT require a NuGet package. Raw ESC/POS bytes are sent via Win32 P/Invoke (`OpenPrinter` / `WritePrinter`). `EscPosCommands.cs` is a self-contained byte constant file.

---

## 🔄 Namespace Structure

```
namespace EZPos.Models.Domain { }
namespace EZPos.DataAccess.Repositories { }
namespace EZPos.Business.Services { }          ← Phase 1+
namespace EZPos.UI { }
namespace EZPos.UI.Pages { }
namespace EZPos.UI.ViewModels { }              ← Phase 1+
namespace EZPos.UI.Dialogs { }                 ← Phase 2+
namespace EZPos.Security.Authentication { }    ← Phase 4
namespace EZPos.Security.Authorization { }     ← Phase 4
namespace EZPos.Utilities.Helpers { }          ← Phase 4
namespace EZPos.Hardware.Barcode { }           ← Phase 5 only
namespace EZPos.Hardware.Printer { }           ← Phase 5 only
```

---

## 🚀 Getting Started

### Build the Project
```bash
dotnet build
```

### Run the Application
```bash
dotnet run
```

### Build Installer
```bash
iscc InnoSetup-EZPos.iss
```

### Database
- Initialized automatically on first run via `Database.Initialize()`
- SQLite file: `EZPos.db` (created next to the exe)
- Config: `Config/config.ini`

---

## 🎨 Design System

### Color Scheme — Dark Mode
- **Primary**: `#00D9FF` (Cyan)
- **Sidebar**: `#0F172A` (Dark Navy)
- **Content BG**: `#1E293B` (Dark Slate)
- **Cards**: `#334155` (Medium Slate)
- **Text Primary**: `#F1F5F9`
- **Text Secondary**: `#94A3B8`
- **Success**: `#10B981` (Green)
- **Warning**: `#F59E0B` (Amber)
- **Error**: `#EF4444` (Red)

### Icons
- **Library**: FontAwesome.Sharp v6.3.0
- Sales: `DollarSign` | Products: `Box` | Stock: `Warehouse` | Reports: `ChartLine`

---

## 📚 Coding Standards

### Naming Conventions
- **Classes/Methods/Properties**: PascalCase
- **Private Fields**: `_camelCase`
- **Constants**: `UPPER_CASE`

### Key Rules
- One class per file
- Namespace hierarchy matches folder structure
- XAML and code-behind in the same folder
- No business logic in page code-behind — use Services
- No direct DB calls from UI — use Services → Repositories

---

## 📋 Known Issues

### ✅ Fixed in Phase 1
- ~~All product data is hardcoded seed — resets every run~~  →  DB-backed via `ProductService.LoadAll()`
- ~~`Database.Initialize()` is never called on startup~~  →  wired in `App.xaml.cs`
- ~~`SaleRepository.AddSale()` never invoked~~  →  `SaleService.ProcessSale()` calls it (Phase 2 wires it into UI)

### ✅ Fixed in Phase 2
- ~~Checkout button was a stub~~ → calls `SaleService.ProcessSale()`, saves to DB, shows `ReceiptDialog`
- ~~Add/Edit/Delete product buttons were MessageBox stubs~~ → `ProductDialog` + `ProductService` fully wired
- ~~Stock In/Out buttons were MessageBox stubs~~ → `StockAdjustDialog` + `StockService` fully wired
- ~~Stock changes from sales not reflected in UI~~ → `SaleService` syncs stock back to `PosStateStore`

### ✅ Fixed in Phase 3
- ~~Reports chart data is hardcoded static arrays~~ → `ReportService` queries real SQLite data; date-range filter + PDF/Excel export live
- ~~Dashboard page not yet created~~ → `DashboardPage` live with today's revenue, order count, avg order value, low stock alerts grid

### Still TODO (Phase 4)
- Category management UI (add/rename/delete) ✅ done
- Page layout consistency review ✅ done
- PDF export ✅ done
- Licensing real API/Stripe validation (mock layer already in place — `Core/Licensing/`, `Infrastructure/Licensing/`, `UI/Licensing/`)

> **Nice to have (future):** Role-based access control (Admin vs Cashier) — not in current scope.

---

## 📞 Stack Reference

- **Language**: C# 10
- **Framework**: .NET 6.0-windows, WPF
- **Database**: SQLite via `System.Data.SQLite 1.0.117`
- **Icons**: FontAwesome.Sharp 6.3.0
- **Installer**: Inno Setup 6
- **CI/CD**: GitHub Actions
- **Target OS**: Windows 7+ x64
- **Barcode Scanner**: HID keyboard-wedge (universal, no SDK)
- **Thermal Printer**: ESC/POS via Win32 raw spooler (no NuGet)

---

**Last Updated**: May 6, 2026
**Version**: 1.7 — Phase 4 in progress; RBAC removed from scope (nice-to-have for future)
**Status**: Phase 4 in progress — licensing real validation remains; layout review, PDF export, and DB backup/restore are done
