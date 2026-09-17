# Feature Status — EZPos

> Current implementation state of all modules and phases. Update this file whenever a feature is completed or started.

---

## Phase Completion

| Phase | Description | Status |
|---|---|---|
| 0 | Foundation — UI theme, icons, CI/CD, error handler | ✅ Complete |
| 5.5 | UI Modernization — MahApps.Metro + MaterialDesign | 🔲 Planned — see [ui-modernization.md](../planning/ui-modernization.md) |
| 1 | Core stability — DB schema, repositories, services, state store | ✅ Complete |
| 2 | Business logic — product CRUD, stock adjust, sale checkout, receipts | ✅ Complete |
| 3 | Data & reporting — ReportService, dashboard live data, stock audit trail | ✅ Complete |
| 4 | UI polish — settings, categories, PDF/Excel export, DB backup/restore | ✅ Complete |
| 4.5 | Licensing architecture — ILicenseService, storage, startup check wired | ✅ Complete |
| 4.7 | Data migration — %ProgramData% location, migration logic in App.xaml.cs | ✅ Complete |
| 4.8 | Auto-update — UpdaterService, UpdateAvailableDialog, Settings check button | ✅ Complete |
| 4.9 | Update manifest hosting — latest.json on GitHub Pages, CI/CD publishing | ✅ Complete |
| 5 | Trial licensing — TrialLicenseService, TrialExpiredWindow, Inno Setup trial.dat | ✅ Complete |
| 5-next | Real HWID/online licensing | ✅ Complete |
| 6 | Web backend — Stripe payment, key generation, license validate API | ✅ Complete |
| 6.1 | Admin panel — login, dashboard, deactivate/activate, reset device, dynamic pricing | ✅ Complete |

---

## Sales Module

- [x] Barcode scanner input (HID) with keyboard disambiguation
- [x] Product lookup by barcode → add to cart
- [x] Cart: add, remove, change quantity
- [x] Multi-tab sales (serve multiple customers simultaneously)
- [x] Payment dialog — Cash, QR, Card, Cheque
- [x] Tax calculation — PerReceipt, PerItem, Fake modes
- [x] Cash rounding
- [x] Sale processing — DB transaction (Sale + SaleItems + StockMovements)
- [x] Receipt dialog — on-screen display
- [x] ESC/POS thermal printer output
- [x] WPF PrintVisual fallback (PDF/laser)
- [x] Auto-print receipt on checkout (config toggle)
- [x] Keyboard hotkeys — payment method + receipt actions (configurable)
- [ ] Low stock warning during/after sale
- [ ] Unsaved cart protection (confirm before navigating away)
- [ ] Split payment (multiple methods per sale)

Status: **CORE COMPLETE**

---

## Inventory Module

- [x] Product CRUD (add, edit, delete)
- [x] Barcode field — scan auto-fill in ProductDialog (Add mode)
- [x] Scan-mode ProductDialog — pre-filled barcode from Sales "not found" flow
- [x] Unit type support — Unit and Pack
- [x] Pack conversion rate + parent product link
- [x] Category management — add, rename, delete
- [x] Stock adjustment — Stock In, Stock Out, Manual Correction
- [x] StockMovement audit trail (SALE, ADJUSTMENT, CORRECTION, OPENING_BALANCE)
- [x] Opening balance written on product registration
- [x] Low stock alerts (StockPage + Dashboard)
- [x] Reorder level per product
- [x] Optional Cost Price per product (nullable — skipped if blank)
- [x] Live profit calculator in ProductDialog (profit/unit, margin %, markup %, status label)
- [x] "Show Cost / Hide Cost" toggle column on ProductsPage grid
- [ ] Bulk import (CSV/Excel)
- [ ] Product image support

Status: **CORE COMPLETE**

---

## Reporting Module

- [x] Date-range reports
- [x] Today summary KPIs (Dashboard)
- [x] Daily breakdown chart
- [x] Top products report
- [x] Payment method breakdown
- [x] Transaction list
- [x] Stock snapshot
- [x] Excel export (ClosedXML)
- [x] PDF export (PdfSharpCore)
- [x] Estimated gross profit KPI band — shown only when cost price data exists
- [x] Profit margin % KPI card in Reports page
- [x] Excel Summary sheet — Est. Gross Profit + Profit Margin rows
- [x] Excel Stock Snapshot sheet — Cost Price, Profit/Unit, Margin % columns
- [ ] Scheduled report email
- [ ] Sales trend forecasting

Status: **CORE COMPLETE**

---

## Barcode Management Module

> Architecture: MVVM. Docs: [barcode-module.md](../features/barcodes/barcode-module.md)

### Phase 1 — MVP
- [x] `ZXing.Net` NuGet package added
- [x] `BarcodeFormat` enum (`src/Models/Domain/BarcodeFormat.cs`)
- [x] `BarcodeFormat` column migration in `Database.MigrateProductsTable()`
- [x] `LabelTemplate`, `LabelPrintJob` domain models
- [x] `BarcodeService.GenerateImage()` — Code128, Code39, EAN-13, QR
- [x] `BarcodeService.GenerateInternalCode()` — `EZP` + zero-padded Id
- [x] `LabelTemplateRepository` — JSON persistence, four seeded defaults
- [x] `RelayCommand` utility (`src/UI/ViewModels/RelayCommand.cs`)
- [x] `LabelPrintService.BuildFixedDocument()` + `PrintLabels()`
- [x] `BarcodesPageViewModel` + `BarcodesPage.xaml/.cs`
- [x] `QuickPrintDialogViewModel` + `QuickPrintDialog.xaml/.cs`
- [x] "Print Label..." button on `ProductsPage` toolbar
- [x] "Generate Barcode" button in `ProductDialog` (BarcodeBox row)
- [x] `"Barcodes"` route registered in `MainWindow.RegisterRoutes()`
- [x] Barcodes nav button in `MainWindow.xaml` sidebar

Status: **CORE COMPLETE** (BETA — see Phase 3 for remaining backlog)

### Phase 2 — Production Ready
- [x] `BarcodeLabels` table in `Database.Initialize()`
- [x] `BarcodeLabelRepository` + `BarcodeLabelRecord` model
- [x] Print job logging to `BarcodeLabels` after each print/PDF export
- [x] Code39 + EAN-13 format support (EAN-13 shows a non-blocking invalid-checkdigit warning)
- [x] PDF export via `PdfSharpCore` in `LabelPrintService` (`ExportToPdf()` + "Export PDF" button)
- [x] Print preview window (`DocumentViewer`) — "Preview" button on BarcodesPage
- [x] `LabelTemplateEditorDialog` + `LabelTemplateEditorViewModel` — "Edit Templates..." link on BarcodesPage
- [x] Damaged label replacement (scanner on BarcodesPage → auto-lookup → QuickPrintDialog; not-found shows a status message)
- [x] Stock receive → print labels hook (`StockPage.StockIn_Click` prompts to print after a successful Stock In)
- [x] A4 sheet template (4×6 = 24 labels per page) — seeded since Phase 1
- [x] Reprint history sub-tab (Expander panel) on BarcodesPage

Status: **CORE COMPLETE** (BETA — needs a Windows build/test pass, see [BARCODE_LOCAL_TESTING.md](../testing/BARCODE_LOCAL_TESTING.md))

### Phase 3 — Advanced
- [ ] QR Code support
- [ ] Inventory count mode
- [ ] Purchase order barcode receiving
- [ ] Price change label trigger
- [ ] Barcode CSV import/export

Status: **BACKLOG**

---

## Settings Module

- [x] Store name, currency, receipt footer
- [x] Tax rate + tax mode
- [x] Printer name + auto-detect
- [x] Auto-print receipt toggle
- [x] Keyboard shortcut configuration
- [x] Database backup (manual)
- [x] Database restore
- [x] Version display (dynamic, from assembly)
- [x] Check for updates button
- [x] Update manifest URL config (`App:UpdateManifestUrl`)

Status: **COMPLETE**

---

## Licensing Module

- [x] ILicenseService interface
- [x] TrialLicenseService (30-day date-based trial)
- [x] trial.dat written by installer (never overwritten on reinstall)
- [x] TrialExpiredWindow (Catalysm Inc branding)
- [x] Startup license check in App.xaml.cs
- [x] FileLicenseStorage
- [x] LicenseRequiredWindow (key-entry)
- [x] LicenseApiClient — real HTTP POST to `/api/licenses/validate` (8s timeout, static HttpClient)
- [x] LicenseValidationCache — 7-day grace period cache (`%ProgramData%\EZPos\license-cache.dat`)
- [x] LicenseService — real API + cache, WPF deadlock-safe (`Task.Run`)
- [x] DeviceFingerprint — MAC address → SHA256 → 16-char hex (stable device ID)
- [x] Device binding — first activation binds DeviceId; different device rejected
- [x] ShutdownMode fix — `OnMainWindowClose` so license dialog close doesn't kill app
- [x] `App:LicenseApiUrl` in config.ini (updated on deployment to production URL)

### Web Backend (EZPos-Web)
- [x] ASP.NET Core MVC + SQLite (EF Core)
- [x] Stripe checkout — one-time payment, MYR, RM 499 (configurable)
- [x] Key generation — `EZPOS-XXXX-XXXX-XXXX`, cryptographically random
- [x] `POST /api/licenses/validate` — validates key + device binding
- [x] Stripe webhook backup (`checkout.session.completed`)
- [x] Admin panel — cookie auth (`AdminCookie`, 8h sliding, HttpOnly+SameSite=Strict)
- [x] Admin dashboard — license list, stats (total/active/deactivated), search by key/email
- [x] Admin actions — deactivate, reactivate, reset device binding
- [x] Dynamic license pricing — stored in `SiteSettings` DB table, editable from admin panel
- [x] Pricing page (`/Home/Pricing`) reflects current price from DB
- [ ] FPX payment method (Stripe FPX — enable in Stripe Dashboard + add `"fpx"` to PaymentMethodTypes)
- [ ] Alternative gateway (Toyyibpay / Billplz) for local bank transfer

Status: **COMPLETE** (web + desktop + admin panel)

---

## Auto-Update System

- [x] UpdaterService — fetch, compare, download, verify
- [x] UpdateAvailableDialog
- [x] SHA256 checksum verification
- [x] Pre-update DB backup
- [x] Silent installer execution
- [x] Startup background check (non-blocking)
- [x] Manual check from Settings
- [x] CI/CD: auto-tag + build installer + publish release + sync manifest
- [x] config.ini seeded on first install (includes UpdateManifestUrl)

Status: **COMPLETE**
