# Barcode Management Module — EZPos

> Covers barcode generation, label printing, label templates, and print history.
> Architecture: MVVM — all new files in this module use ViewModels. Existing pages are unaffected.

---

## Key Files

| File | Role |
|---|---|
| `src/UI/Pages/BarcodesPage.xaml/.cs` | Main barcode page — product selection, template config, bulk print |
| `src/UI/ViewModels/BarcodesPageViewModel.cs` | State + commands for BarcodesPage |
| `src/UI/Dialogs/QuickPrintDialog.xaml/.cs` | Single-product quick print (from ProductsPage toolbar) |
| `src/UI/ViewModels/QuickPrintDialogViewModel.cs` | State + commands for QuickPrintDialog |
| `src/UI/Dialogs/LabelTemplateEditorDialog.xaml/.cs` | Create / edit label templates |
| `src/UI/ViewModels/LabelTemplateEditorViewModel.cs` | State + commands for template editor |
| `src/UI/ViewModels/RelayCommand.cs` | ICommand implementation (shared across all ViewModels) |
| `src/Business/Services/BarcodeService.cs` | Barcode image generation (ZXing.Net), internal code generation |
| `src/Business/Services/LabelPrintService.cs` | Build FixedDocument, print to WPF printer, export to PDF |
| `src/DataAccess/Repositories/LabelTemplateRepository.cs` | Load/save label templates (JSON file) |
| `src/DataAccess/Repositories/BarcodeLabelRepository.cs` | Print history CRUD (Phase 2) |
| `src/Models/Domain/BarcodeFormat.cs` | Enum: Code128, Code39, EAN13, QR |
| `src/Models/Domain/LabelTemplate.cs` | Template definition (dimensions, field toggles, fonts) |
| `src/Models/Domain/LabelPrintJob.cs` | One print job entry: product + barcode + format + qty |
| `src/Models/Domain/BarcodeLabelRecord.cs` | Print history record (Phase 2) |

---

## Architecture Pattern

The Barcode module is the **first MVVM module** in EZPos. Existing pages remain as code-behind. New modules going forward should follow this pattern.

```
BarcodesPage.xaml
    ↕ DataContext
BarcodesPageViewModel          ← INotifyPropertyChanged + ICommand (RelayCommand)
    ↕ calls
BarcodeService                 ← stateless, no WPF dependencies
LabelPrintService              ← stateless, no WPF dependencies
LabelTemplateRepository        ← JSON-backed persistence
```

**Code-behind rule:** The `.xaml.cs` file for any Barcode module page/dialog must only:
1. Call `InitializeComponent()`
2. Assign `DataContext = viewModel`
3. Wire any events that cannot be handled in XAML (e.g., `PreviewKeyDown` for scanner)

No business logic, service calls, or state belongs in code-behind for this module.

---

## Barcode Formats

| Format | Use case | Constraints |
|---|---|---|
| **Code128** | Default. Any alphanumeric string, any length. | None — recommended for all internal products. |
| **Code39** | Older scanners and industrial labels. | Uppercase alphanumeric + a few symbols. Less dense than Code128. |
| **EAN-13** | Standard retail barcode. | Exactly 12 digits + check digit (auto-calculated). Internal-use prefix: 200–299. Must show warning to user. |
| **QR Code** | Phase 3. URL or structured data. | Only useful if a web product catalogue exists. |

**Default format: Code128.** Do not default to EAN-13. EAN-13 requires a GS1 company prefix for legitimate external use — using random values causes conflicts in multi-store or external systems.

---

## Internal Code Generation

When a product has no barcode, or the user clicks "Generate Barcode" in `ProductDialog`, `BarcodeService.GenerateInternalCode()` produces:

```
EZP + zero-padded product Id (6 digits)
Examples: EZP000001, EZP000042, EZP999999
```

- Guaranteed unique (based on product Id)
- Code128-safe (no length or check-digit constraints)
- Human-readable prefix identifies it as an EZPos internal code
- Does not require a GS1 account

---

## Workflow

### Single Product Quick Print (from ProductsPage)

```
1. User selects a product in ProductsGrid
2. Clicks "Print Label..." toolbar button
   ↓
3. QuickPrintDialog opens
   - Product name + barcode pre-filled (read-only)
   - Format dropdown (default: product's BarcodeFormat, fallback Code128)
   - Template dropdown (default: last used / config default)
   - Printer dropdown (populated from installed Windows printers)
   - Quantity spinner (default: 1)
   ↓
4. User adjusts qty → clicks Print
   ↓
5. QuickPrintDialogViewModel.PrintCommand
   → BarcodeService.GenerateImage()
   → LabelPrintService.PrintLabels()
   ↓
6. (Phase 2) BarcodeLabelRepository.Insert() — log print job
   ↓
7. Dialog closes
```

### Bulk Print (from BarcodesPage)

```
1. User opens Barcodes page from left navigation
   ↓
2. Left panel: product list with checkboxes
   - Search box + category filter
   - "Select All" / "Select by Category" buttons
   ↓
3. Right panel: template config
   - Template selector (dropdown + "Edit Templates" link)
   - Format selector (applies to all selected products)
   - Printer selector
   - Field toggles: Barcode image, Name, Price, Category, Store Name
   - Quantity table: one row per checked product, editable qty
   ↓
4. Live label preview (single label shown for the first selected product)
   ↓
5. Click "Print Labels" or "Export PDF"
   → BarcodesPageViewModel.PrintCommand / ExportPdfCommand
   → LabelPrintService.BuildFixedDocument()
   → PrintDialog.PrintDocument() or PdfSharpCore file write
   ↓
6. (Phase 2) Log each print job to BarcodeLabels table
```

### Damaged Label Replacement

```
1. User opens Barcodes page
   ↓
2. Scans damaged/faded barcode with scanner
   SalesKeyboardInputService detects scan → fires BarcodeCompleted
   ↓
3. BarcodesPageViewModel handles BarcodeCompleted:
   - Looks up product in PosStateStore by barcode value
   - If found: auto-selects product + sets qty to 1 + opens QuickPrintDialog
   - If not found: shows snackbar "Barcode not registered"
   ↓
4. User confirms → prints 1 replacement label
```

### Generate Barcode in ProductDialog (Phase 1 addition)

```
1. User opens ProductDialog (Add or Edit mode)
2. BarcodeBox is empty or user wants to replace value
3. Clicks wand icon button next to BarcodeBox
   ↓
4. BarcodeService.GenerateInternalCode(product.Id or next sequence)
5. BarcodeBox.Text populated
6. User can accept, overwrite, or scan an external barcode instead
```

---

## Label Template System

Templates are stored in `%ProgramData%\EZPos\label-templates.json`. Not in SQLite. Rationale: templates are user configuration, not transactional data. JSON is portable, human-editable, and does not require schema migrations when template fields change.

### Predefined Templates (seeded on first run)

| Template Name | Width | Height | Labels/Sheet | Use Case |
|---|---|---|---|---|
| Standard 40×30 | 40mm | 30mm | 1×1 | Label printer, general products |
| Shelf Label 100×50 | 100mm | 50mm | 1×1 | Shelf edge display |
| Price Tag 58×40 | 58mm | 40mm | 1×1 | Hanging price tags |
| A4 Sheet (24 labels) | 70mm | 37mm | 4×6 | Laser/inkjet A4 paper |

### LabelTemplate Fields

```csharp
public class LabelTemplate
{
    public string  Id               { get; set; }   // GUID
    public string  Name             { get; set; }
    public double  LabelWidthMm     { get; set; }
    public double  LabelHeightMm    { get; set; }
    public int     LabelsPerRow     { get; set; } = 1;
    public int     LabelsPerColumn  { get; set; } = 1;
    public bool    ShowBarcode      { get; set; } = true;
    public bool    ShowName         { get; set; } = true;
    public bool    ShowPrice        { get; set; } = true;
    public bool    ShowCategory     { get; set; } = false;
    public bool    ShowStoreName    { get; set; } = false;
    public string? CustomText       { get; set; }
    public double  BarcodeHeightPct { get; set; } = 0.50; // 50% of label height
    public double  FontSizeName     { get; set; } = 8;
    public double  FontSizePrice    { get; set; } = 10;
    public bool    IsDefault        { get; set; } = false;
}
```

---

## Printing Engine

### Direct Print (WPF Native)

Use `System.Windows.Controls.PrintDialog` + `FixedDocument`/`FixedPage`. Do **not** use WinForms `PrintDocument` — this is a WPF app.

```
LabelPrintService.BuildFixedDocument(jobs, template)
    → foreach job × quantity:
        → BarcodeService.GenerateImage() → BitmapImage
        → Build Canvas (barcode image + TextBlocks for name, price, etc.)
        → Pack labels into FixedPage (grid: LabelsPerRow × LabelsPerColumn)
        → Add FixedPage to FixedDocument
    ↓
PrintDialog.PrintDocument(fixedDoc.DocumentPaginator, "EZPos Labels")
```

### PDF Export (PdfSharpCore — already installed)

Draw equivalent layout using `PdfSharpCore.Drawing.XGraphics`. Converts mm dimensions to points (1mm = 2.835pt). Output written to a user-chosen file path via `SaveFileDialog`.

### Print Preview

Wrap `FixedDocument` in a `DocumentViewer` inside a WPF `Window`. WPF provides this natively — no third-party library required.

---

## Database Changes

### Products Table — `BarcodeFormat` column

```sql
-- Migration via TryAddColumn (zero-downtime additive)
ALTER TABLE Products ADD COLUMN BarcodeFormat TEXT NOT NULL DEFAULT 'Code128';
```

Added in `Database.MigrateProductsTable()` alongside existing migrations.

### BarcodeLabels Table (Phase 2)

```sql
CREATE TABLE IF NOT EXISTS BarcodeLabels (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId     INTEGER NOT NULL,
    PrintedAt     TEXT    NOT NULL,
    Quantity      INTEGER NOT NULL DEFAULT 1,
    TemplateName  TEXT    NOT NULL DEFAULT 'Standard',
    BarcodeFormat TEXT    NOT NULL DEFAULT 'Code128',
    FOREIGN KEY(ProductId) REFERENCES Products(Id)
);
CREATE INDEX IF NOT EXISTS idx_barcodelabels_product ON BarcodeLabels(ProductId);
CREATE INDEX IF NOT EXISTS idx_barcodelabels_date    ON BarcodeLabels(PrintedAt);
```

No other table changes. `Products.Barcode` remains the single source of truth for the scannable value.

---

## Scanner Compatibility

No changes to `SalesKeyboardInputService`. Existing 150ms scan threshold is unchanged. The scanner still reads the `Products.Barcode` TEXT value — the visual render format (Code128 vs Code39) does not affect what string the scanner returns.

**Risk to communicate to users:** if a product's `Barcode` value is changed after labels are printed, old physical labels become stale. The print history log (Phase 2) provides a trail to identify which products need relabelling.

---

## UI Layout — BarcodesPage

```
┌─ Left Panel (40%) ──────────────────────┬─ Right Panel (60%) ──────────────────┐
│ [Search box]       [Category ▼]         │ ┌─ Template Config ──────────────────┐│
│ ─────────────────────────────────────── │ │ Template: [Standard 40×30     ▼]   ││
│ ☐ Product Name         Barcode  Format  │ │ Format:   [Code128            ▼]   ││
│ ☑ Coca Cola 330ml      8888001  Code128 │ │ Printer:  [HP LaserJet M402   ▼]   ││
│ ☐ Mineral Water 500ml  8888002  Code128 │ │ Fields:   ☑ Barcode  ☑ Name        ││
│ ☑ Nescafé 3-in-1       8888003  Code128 │ │           ☑ Price    ☐ Category    ││
│ ─────────────────────────────────────── │ │           ☐ Store Name             ││
│ [Select All]  [Select by Category]      │ └────────────────────────────────────┘│
│                                         │ ┌─ Quantity ─────────────────────────┐│
│                                         │ │ Coca Cola 330ml       qty: [  1 ]  ││
│                                         │ │ Nescafé 3-in-1        qty: [  1 ]  ││
│                                         │ └────────────────────────────────────┘│
│                                         │ ┌─ Label Preview ────────────────────┐│
│                                         │ │  ▐▌▌▐▐▌▌▐▌▐▐▌▐▌▌                  ││
│                                         │ │  8888001                            ││
│                                         │ │  Coca Cola 330ml    RM 2.50         ││
│                                         │ └────────────────────────────────────┘│
│                                         │  [Print Labels]   [Export PDF]        │
└─────────────────────────────────────────┴──────────────────────────────────────┘
```

---

## NuGet Dependencies

| Package | Purpose | Status |
|---|---|---|
| `ZXing.Net` | Barcode image generation (Code128, Code39, EAN-13, QR) | **To add** |
| `PdfSharpCore` | PDF export of label sheets | Already installed (v1.3.67) |
| `MaterialDesignThemes` | Dialog host, controls styling | Already installed |
| `FontAwesome.Sharp` | Barcode + wand icons for toolbar | Already installed |

---

## Services

### BarcodeService

Stateless. No WPF dependencies — safe to unit test without a UI.

| Method | Returns | Notes |
|---|---|---|
| `GenerateImage(string data, BarcodeFormat format, int widthPx, int heightPx)` | `BitmapImage` | Converts ZXing Bitmap → MemoryStream → BitmapImage |
| `GenerateInternalCode(int productId)` | `string` | `EZP` + productId zero-padded to 6 digits |
| `IsBarcodeUnique(string barcode, int excludeProductId)` | `bool` | Delegates to `ProductRepository.GetByBarcode()` |
| `ValidateEan13(string value)` | `bool` | Check-digit validation for EAN-13 inputs |

### LabelPrintService

Stateless. References `System.Windows.Controls` for WPF printing only.

| Method | Returns | Notes |
|---|---|---|
| `BuildFixedDocument(IEnumerable<LabelPrintJob> jobs, LabelTemplate template)` | `FixedDocument` | Core layout engine |
| `PrintLabels(IEnumerable<LabelPrintJob> jobs, LabelTemplate template, string printerName)` | `void` | Wraps BuildFixedDocument → PrintDialog |
| `ExportToPdf(IEnumerable<LabelPrintJob> jobs, LabelTemplate template, string filePath)` | `void` | PdfSharpCore render |
| `GetInstalledPrinters()` | `List<string>` | `PrinterSettings.InstalledPrinters` |

### LabelTemplateRepository

JSON-backed. File: `%ProgramData%\EZPos\label-templates.json`.

| Method | Notes |
|---|---|
| `GetAll()` | Deserialise JSON. If file absent, seed defaults and save. |
| `GetDefault()` | First template with `IsDefault = true`, or first in list. |
| `Save(LabelTemplate t)` | Upsert by `Id`. Serialise full list back to file. |
| `Delete(string id)` | Remove by `Id`. Cannot delete last remaining template. |

---

## ViewModels

### BarcodesPageViewModel

| Member | Type | Notes |
|---|---|---|
| `FilteredProducts` | `ICollectionView` | Wraps `PosStateStore.Products`. Filtered by `SearchText` + `SelectedCategory`. |
| `PrintJobs` | `ObservableCollection<LabelPrintJob>` | Built from checked products. |
| `Templates` | `List<LabelTemplate>` | Loaded from `LabelTemplateRepository`. |
| `SelectedTemplate` | `LabelTemplate` | Two-way binding. Changing triggers preview refresh. |
| `SelectedFormat` | `BarcodeFormat` | Applies to all jobs in `PrintJobs`. |
| `SearchText` | `string` | Triggers `FilteredProducts` refresh. |
| `SelectedCategory` | `string` | Triggers `FilteredProducts` refresh. |
| `PrintCommand` | `RelayCommand` | Enabled when `PrintJobs.Any()`. Calls `LabelPrintService.PrintLabels()`. |
| `ExportPdfCommand` | `RelayCommand` | Enabled when `PrintJobs.Any()`. Calls `LabelPrintService.ExportToPdf()`. |
| `SelectAllCommand` | `RelayCommand` | Adds all filtered products to `PrintJobs`. |
| `SelectByCategoryCommand` | `RelayCommand` | Adds all products in `SelectedCategory` to `PrintJobs`. |
| `RemoveJobCommand` | `RelayCommand<LabelPrintJob>` | Removes one job from `PrintJobs`. |
| `HandleBarcodeScanned(string barcode)` | `void` | Called from code-behind scanner event. Lookup product → open QuickPrint. |

### QuickPrintDialogViewModel

| Member | Type | Notes |
|---|---|---|
| `ProductName` | `string` | Read-only display. |
| `Barcode` | `string` | Read-only display. |
| `SelectedFormat` | `BarcodeFormat` | Editable. Saved back to `Product.BarcodeFormat` on print. |
| `SelectedTemplate` | `LabelTemplate` | Editable. |
| `SelectedPrinter` | `string` | From `LabelPrintService.GetInstalledPrinters()`. |
| `Quantity` | `int` | Min 1. |
| `PrintCommand` | `RelayCommand` | Calls `LabelPrintService.PrintLabels()` with single job. |

---

## Navigation Registration

```csharp
// MainWindow.RegisterRoutes()
navigationService.Register("Barcodes", () =>
{
    var barcodeService   = new BarcodeService();
    var printService     = new LabelPrintService();
    var templateRepo     = new LabelTemplateRepository();
    var vm = new BarcodesPageViewModel(barcodeService, printService, templateRepo, stateStore);
    return new BarcodesPage(vm);
});
```

Add "Barcodes" nav button to `MainWindow.xaml` sidebar between Products and Stock.
Icon: `FontAwesome.Sharp.IconChar.Barcode`.

---

## Implementation Roadmap

### Phase 1 — MVP

Goal: generate and print a barcode label for any product without leaving EZPos.

- [ ] Add `ZXing.Net` NuGet package
- [ ] Add `BarcodeFormat` enum (`src/Models/Domain/BarcodeFormat.cs`)
- [ ] Add `BarcodeFormat` column migration in `Database.MigrateProductsTable()`
- [ ] Add `LabelTemplate`, `LabelPrintJob` models
- [ ] Implement `BarcodeService.GenerateImage()` — Code128 only
- [ ] Implement `BarcodeService.GenerateInternalCode()`
- [ ] Implement `LabelTemplateRepository` with four seeded defaults
- [ ] Add `RelayCommand` utility (`src/UI/ViewModels/RelayCommand.cs`)
- [ ] Implement `LabelPrintService.BuildFixedDocument()` + `PrintLabels()`
- [ ] Build `BarcodesPageViewModel` (product list, print jobs, print command)
- [ ] Build `BarcodesPage.xaml` (two-panel layout as designed above)
- [ ] Build `QuickPrintDialogViewModel` + `QuickPrintDialog.xaml`
- [ ] Add "Print Label..." button to `ProductsPage` toolbar
- [ ] Add "Generate Barcode" wand button to `ProductDialog` (BarcodeBox row)
- [ ] Register `"Barcodes"` route in `MainWindow.RegisterRoutes()`
- [ ] Add Barcodes nav button in `MainWindow.xaml` sidebar

### Phase 2 — Production Ready

Goal: complete label management with history, multiple formats, and PDF export.

- [ ] Create `BarcodeLabels` table in `Database.Initialize()`
- [ ] Implement `BarcodeLabelRepository` + `BarcodeLabelRecord` model
- [ ] Log all print jobs to `BarcodeLabels` after each print
- [ ] Code39 and EAN-13 format support (EAN-13 shows internal-use warning)
- [ ] PDF export via `PdfSharpCore` in `LabelPrintService.ExportToPdf()`
- [ ] Print preview window using `DocumentViewer`
- [ ] `LabelTemplateEditorDialog` — edit dimensions, field toggles, font sizes
- [ ] `LabelTemplateEditorViewModel`
- [ ] Damaged label replacement flow (scanner → auto-select → quick print)
- [ ] "Print labels after stock receive" hook in `StockAdjustDialog`
- [ ] A4 sheet template (4×6 = 24 labels per page)
- [ ] Reprint history sub-tab on BarcodesPage showing `BarcodeLabels` records

### Phase 3 — Advanced Features

- [ ] QR Code format support (requires web product catalogue to be useful)
- [ ] Inventory count mode — scan session, count discrepancy report vs `Products.Stock`
- [ ] Purchase order barcode receiving — scan incoming goods → auto stock-in
- [ ] Price change label trigger — when `Product.Price` changes, prompt to reprint
- [ ] Barcode CSV import/export
