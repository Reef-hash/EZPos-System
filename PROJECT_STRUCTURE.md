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
│   │       ├── Product.cs               [EXISTS]  — add Category, MaxStock, ReorderLevel
│   │       ├── Sale.cs                  [EXISTS]  — add PaymentMethod, CashierId
│   │       ├── SaleItem.cs              [EXISTS]
│   │       └── StockMovement.cs         [PLANNED] — audit trail for stock changes
│   │
│   ├── DataAccess/
│   │   └── Repositories/
│   │       ├── Database.cs              [EXISTS]  — add StockMovements table to schema
│   │       ├── ConfigHelper.cs          [EXISTS]
│   │       ├── ProductRepository.cs     [EXISTS]  — GetAll(), Add(), Update(), Delete()
│   │       ├── SaleRepository.cs        [EXISTS]  — fully implemented, never called yet
│   │       └── StockMovementRepository  [PLANNED] — Insert(), GetByProduct()
│   │
│   ├── Business/
│   │   └── Services/
│   │       ├── ProductService.cs        [PLANNED] — Add, Edit, Delete, GetAll
│   │       ├── SaleService.cs           [PLANNED] — ProcessSale → DB write + state sync
│   │       ├── StockService.cs          [PLANNED] — AdjustStock, GetLowStockItems
│   │       └── ReportService.cs         [PLANNED] — GetSalesByPeriod, GetTopProducts
│   │
│   ├── UI/
│   │   ├── State/
│   │   │   └── PosStateStore.cs         [EXISTS]  — add Load() from DB on startup
│   │   ├── Navigation/
│   │   │   └── NavigationService.cs     [EXISTS]
│   │   ├── ViewModels/                  [PLANNED folder]
│   │   │   ├── DashboardViewModel.cs    [PLANNED]
│   │   │   ├── SalesViewModel.cs        [PLANNED]
│   │   │   ├── ProductsViewModel.cs     [PLANNED]
│   │   │   ├── StockViewModel.cs        [PLANNED]
│   │   │   └── ReportsViewModel.cs      [PLANNED]
│   │   ├── Dialogs/                     [PLANNED folder]
│   │   │   ├── AddProductDialog.xaml    [PLANNED]
│   │   │   ├── StockAdjustDialog.xaml   [PLANNED]
│   │   │   └── ReceiptDialog.xaml       [PLANNED]
│   │   ├── Pages/
│   │   │   ├── DashboardPage.xaml       [PLANNED] — first tab, live KPIs
│   │   │   ├── SalesPage.xaml           [EXISTS]
│   │   │   ├── ProductsPage.xaml        [EXISTS]
│   │   │   ├── StockPage.xaml           [EXISTS]
│   │   │   └── ReportsPage.xaml         [EXISTS]
│   │   ├── Resources/
│   │   └── Styles/
│   │
│   ├── Security/
│   │   ├── Authentication/              [PLANNED — Phase 4]
│   │   └── Authorization/               [PLANNED — Phase 4]
│   │
│   ├── Hardware/                        [PLANNED folder] — all hardware I/O
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
│           └── CurrencyFormatter.cs     [PLANNED]
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

## ⚠️ Critical Gap — Current State vs Required State

The app currently has **two disconnected data layers running in parallel:**

| | Layer A — SQLite (DataAccess/) | Layer B — PosStateStore (in-memory) |
|---|---|---|
| Status | EXISTS but never called by UI | Used by all UI pages |
| Data | Empty on first run | 10 hardcoded seed products |
| Persistence | ✓ Survives app restarts | ✗ Resets every run |
| Checkout | `SaleRepository.AddSale()` implemented but never invoked | Only calls `ClearCart()` |
| Stock changes | Never updated | Never saved |

**Impact: Every restart loses all transactions and stock changes. The app is not production-usable until Phase 1 is complete.**

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

### 🟢 Phase 3 — Data Flow & Reporting

**Goal:** Real numbers everywhere. Dashboard shows live data. No hardcoded arrays.

| # | Task | File(s) | Status |
|---|---|---|---|
| 3.1 | `ReportService` — GetSalesByPeriod, GetTopProducts, GetDailySummary | `Business/Services/ReportService.cs` | TODO |
| 3.2 | Wire `ReportService` into `ReportsPage` — replace all hardcoded chart arrays | `UI/Pages/ReportsPage.xaml.cs` | TODO |
| 3.3 | `DashboardPage` — today's total, transaction count, low stock alerts | `UI/Pages/DashboardPage.xaml` | TODO |
| 3.4 | Register DashboardPage in `NavigationService` + add nav button in MainWindow | `UI/Navigation/NavigationService.cs`, `MainWindow.xaml` | TODO |
| 3.5 | Verify stock deduction from sales is accurate in DB and PosStateStore | `Business/Services/SaleService.cs` | TODO |
| 3.6 | Verify StockMovement audit trail written on every adjustment and sale | `DataAccess/Repositories/StockMovementRepository.cs` | TODO |

---

### 🔵 Phase 4 — UI Polish

**Goal:** System is consistent, usable, and visually solid. Fix all layout/spacing issues before adding hardware complexity.

| # | Task | File(s) | Status |
|---|---|---|---|
| 4.1 | Settings page — business name, tax rate, currency symbol | `UI/Pages/SettingsPage.xaml` | TODO |
| 4.2 | Category management (add/rename/delete categories) | `UI/Dialogs/` | TODO |
| 4.3 | Review all page layouts — fix spacing, padding, alignment consistency | All Pages | TODO |
| 4.4 | Export reports to PDF | `Utilities/Helpers/` | TODO |
| 4.5 | User login / PIN screen | `Security/Authentication/` | TODO |
| 4.6 | Role-based access control (Admin, Cashier) | `Security/Authorization/` | TODO |
| 4.7 | DB backup & restore | `Utilities/Helpers/` | TODO |

---

### ⚫ Phase 5 — Hardware Integration *(last phase only)*

**Why hardware is last:** Every hardware component depends on a stable, fully connected system. `BarcodeService` must call a working `ProductService`. `PrinterService` must receive a valid `SaleResult` from a working `SaleService`. Wiring hardware into an unstable or partially connected system creates hidden failures that are hard to diagnose — it is never the cause of a bug, but it always amplifies them. Hardware is added last to a system that is already fully functional without it.

**Goal:** Plug hardware into an already fully working system. If the scanner or printer fails, the system continues to operate normally.

| # | Task | File(s) | Status |
|---|---|---|---|
| 5.1 | `EscPosCommands.cs` — ESC/POS byte constant definitions | `Hardware/Printer/EscPosCommands.cs` | TODO |
| 5.2 | `ReceiptBuilder.cs` — formats `SaleResult` → ESC/POS byte array | `Hardware/Printer/ReceiptBuilder.cs` | TODO |
| 5.3 | `PrinterService.cs` — async raw print via Win32 spooler, 3-attempt retry, graceful fallback | `Hardware/Printer/PrinterService.cs` | TODO |
| 5.4 | Add `[Printer] Name=` to `config.ini` — skip print gracefully if blank | `Config/config.ini` | TODO |
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

### Still TODO (Phase 3)
- Reports chart data is hardcoded static arrays
- Dashboard page not yet created

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

**Last Updated**: May 2, 2026
**Version**: 1.4 — Phase 2 complete; full Product CRUD, Stock Adjust, Checkout + Receipt live
**Status**: Phase 3 next — Data Flow & Reporting (real numbers, live dashboard)
