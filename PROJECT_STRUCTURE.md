# EZPos System - Project Structure & Architecture Documentation

## Project Overview

EZPos System is a modern **Point of Sale (POS) application** built with **WPF (.NET 6.0)** using a layered architecture pattern designed to be scalable for SAAS (Software-as-a-Service) multi-user online deployment.

---

## Current Directory Structure

> All files below exist in the codebase as of v1.0.6. Last updated: May 2026.

```
EZPos-System/
├── src/
│   ├── Models/
│   │   └── Domain/
│   │       ├── Product.cs               — Category, MaxStock, ReorderLevel, UnitType
│   │       ├── Sale.cs                  — PaymentMethod
│   │       ├── SaleItem.cs
│   │       ├── StockMovement.cs         — audit trail for stock changes
│   │       └── UpdateManifest.cs        — deserialised shape of latest.json
│   │
│   ├── DataAccess/
│   │   └── Repositories/
│   │       ├── Database.cs              — SQLite schema init + migration
│   │       ├── ConfigHelper.cs          — Get/Set flat key-value config helpers
│   │       ├── ProductRepository.cs     — GetAll(), GetByBarcode(), Add(), Update(), Delete()
│   │       ├── CategoryRepository.cs    — GetAll(), Add(), Rename(), Delete()
│   │       ├── SaleRepository.cs        — AddSale(), writes StockMovement audit rows
│   │       └── StockMovementRepository  — Insert(), InsertWithConnection(), GetByProduct()
│   │
│   ├── Business/
│   │   └── Services/
│   │       ├── ProductService.cs        — Add, Edit, Delete, GetAll, GetByBarcode
│   │       ├── CategoryService.cs       — Add, Rename, Delete, GetAll
│   │       ├── SaleService.cs           — ProcessSale → DB write + state sync
│   │       ├── StockService.cs          — AdjustStock, GetLowStockItems
│   │       ├── ReportService.cs         — GetSummary, GetDailyBreakdown, GetTopProducts,
│   │       │                              GetTodaySummary, GetLowStockAlerts,
│   │       │                              GetPaymentBreakdown, GetTransactions, GetStockSnapshot
│   │       └── UpdaterService.cs        — CheckForUpdatesAsync(), DownloadInstallerAsync(),
│   │                                      SHA256 verification, version comparison helpers
│   │
│   ├── Core/
│   │   └── Licensing/
│   │       ├── ILicenseService.cs       — contract: LoadAndValidate(), Activate(), IsLicensed
│   │       ├── ILicenseStorage.cs       — contract: LoadKey(), SaveKey(), ClearKey()
│   │       ├── LicenseInfo.cs           — Key, Status, ExpiryDate, ActivatedAt, PlanName
│   │       ├── LicenseStatus.cs         — enum: Valid, Invalid, Expired, Missing, NotActivated
│   │       ├── LicenseService.cs        — mock impl; TODO: wire real API via LicenseApiClient
│   │       ├── FileLicenseStorage.cs    — stores key in %ProgramData%\EZPos\license.dat
│   │       └── TrialLicenseService.cs   — ACTIVE: date-based 30-day trial via trial.dat
│   │
│   ├── Infrastructure/
│   │   └── Licensing/
│   │       └── LicenseApiClient.cs      — placeholder for future Stripe/API integration
│   │
│   ├── UI/
│   │   ├── State/
│   │   │   └── PosStateStore.cs         — single in-memory state; Load() from DB on startup
│   │   ├── Navigation/
│   │   │   └── NavigationService.cs     — route → UserControl mapping
│   │   ├── Input/
│   │   │   └── SalesKeyboardInputService.cs  — HID barcode scanner vs keyboard disambiguation
│   │   ├── Licensing/
│   │   │   ├── LicenseRequiredWindow.xaml    — key-entry activation window (future use)
│   │   │   └── TrialExpiredWindow.xaml       — ACTIVE: blocks app after 30-day trial expires;
│   │   │                                        shows Catalysm Inc / Zarif El-Mansour / WhatsApp
│   │   ├── Dialogs/
│   │   │   ├── ProductDialog.xaml            — Add/Edit product form
│   │   │   ├── CategoryManagementDialog.xaml — Add/Rename/Delete categories
│   │   │   ├── StockAdjustDialog.xaml        — Stock In/Out/Manual adjust
│   │   │   ├── PaymentDialog.xaml            — payment method + amount + change calc
│   │   │   ├── ReceiptDialog.xaml            — on-screen receipt summary
│   │   │   ├── UpdateAvailableDialog.xaml    — update prompt: version, notes, Update/Skip
│   │   │   ├── WeightInputDialog.xaml        — weight/quantity input for loose-weight items
│   │   │   └── RenameDialog.xaml             — generic rename input dialog
│   │   ├── Pages/
│   │   │   ├── DashboardPage.xaml       — live KPI cards + low-stock alerts
│   │   │   ├── SalesPage.xaml           — POS cart + barcode input + checkout flow
│   │   │   ├── SalesModeControl.xaml    — reusable sales cart control
│   │   │   ├── ProductsPage.xaml        — catalogue: add/edit/delete/filter by category
│   │   │   ├── StockPage.xaml           — inventory: KPI cards, stock adjust, filter
│   │   │   ├── ReportsPage.xaml         — date-range reports + PDF/Excel export
│   │   │   └── SettingsPage.xaml        — store info, tax, printer, hotkeys,
│   │   │                                   DB backup/restore, check for updates
│   │   ├── Resources/                   — (empty; global resources in App.xaml)
│   │   └── Styles/                      — (empty; global styles in App.xaml)
│   │
│   ├── Security/
│   │   ├── Authentication/              — empty stub (future multi-user login)
│   │   └── Authorization/               — empty stub (future role-based access)
│   │
│   └── Utilities/
│       ├── Extensions/                  — (empty; reserved for extension methods)
│       └── Helpers/
│           ├── EscPosDocument.cs        — ESC/POS byte builder (80 mm receipt)
│           └── RawPrinterHelper.cs      — Win32 P/Invoke raw spooler wrapper
│
├── Resources/
│   ├── Icons/
│   │   └── app.ico
│   └── Images/
│
├── Config/
│   └── config.ini                       — StoreName, PrinterName, TaxRate, Currency,
│                                           ReceiptFooter, TaxMode, hotkeys, UpdateManifestUrl
│
├── MainWindow.xaml / .cs                — app shell, nav wiring, startup update check
├── App.xaml / .cs                       — startup: trial check → DB init → services → MainWindow
├── Program.cs
├── EZPos.csproj                         — version 1.0.6, net6.0-windows7.0
├── InnoSetup-EZPos.iss                  — installer: writes trial.dat on first install,
│                                           seeds EZPos.db, sets %ProgramData% permissions
└── .github/workflows/
    ├── build.yml                        — CI: restore + build on every push to main
    └── auto-tag-from-csproj.yml         — reads <Version> from .csproj, creates git tag,
                                           builds installer, generates latest.json,
                                           publishes GitHub Release, syncs manifest repo
```

---

## Architecture Layers

```
VIEW  (XAML + minimal code-behind)
  ✓ Layout, event wiring, display only
  ✗ Must NOT call DB directly or hold business logic

SERVICE LAYER  (Business/Services/)
  ✓ Business rules, validation, orchestration
  ✓ Talks to Repositories only; returns domain objects
  ✗ Must NOT know about UI or WPF types

REPOSITORY  (DataAccess/Repositories/)
  ✓ Raw SQL, maps rows → domain objects
  ✗ Must NOT contain business logic

POSSTATE STORE  (UI/State/PosStateStore.cs)
  ✓ Live in-memory state driving all UI bindings
  ✓ Fed by Services after DB operations
  ✗ Must NOT be modified directly from page code-behind

CORE/LICENSING
  ✓ ILicenseService contract — checked on startup in App.xaml.cs
  ✓ TrialLicenseService is the active implementation (date-based 30-day trial)
  ✓ LicenseService (mock) + LicenseApiClient stub ready for future HWID/API licensing
```

---

## Data Storage - Runtime Paths

```
C:\Program Files\EZPos\          ← read-only binaries (installer target)
  ├── EZPos.exe
  └── *.dll

C:\ProgramData\EZPos\            ← read-write app data (survives all updates)
  ├── EZPos.db                   ← live SQLite database
  ├── config.ini                 ← store settings (migrated from app folder on first run)
  ├── trial.dat                  ← trial install date (written by installer, never overwritten)
  ├── license.dat                ← license key file (future real licensing)
  └── Backups\
      ├── EZPos_Backup_*.db      ← manual backups from Settings
      └── EZPos_PreUpdate_*.db   ← auto-backup created before each update
```

---

## Trial Licensing System (Active - v1.0.6)

**Implementation:** `TrialLicenseService` in `src/Core/Licensing/`

**Flow on every startup:**
1. `App.xaml.cs` instantiates `ILicenseService licenseService = new TrialLicenseService()`
2. `LoadAndValidate()` reads `C:\ProgramData\EZPos\trial.dat`
3. Computes `INSTALL_DATE + 30 days` vs `UtcNow`
4. If **Valid** → app continues normally
5. If **Expired** → `TrialExpiredWindow` opens (modal, cannot bypass) → app shuts down

**trial.dat format** (written by Inno Setup on first install, never overwritten on reinstall):
```
INSTALL_DATE=2026-05-07 14:30:00
```

**Expiry window** (`TrialExpiredWindow`) shows:
- Expiry date
- Catalysm Inc / Zarif El-Mansour
- WhatsApp: 019-5778954

**Migration to real licensing:** change one line in `App.xaml.cs`:
```csharp
// Current (trial):
ILicenseService licenseService = new TrialLicenseService();

// Future (HWID/API):
ILicenseService licenseService = new LicenseService(new FileLicenseStorage(), ...);
```
Everything else — contract, App routing, UI — remains unchanged.

---

## Auto-Update System (Active - v1.0.6)

**Manifest URL:** `https://reef-hash.github.io/EZPos-Update-System/latest.json`
**Config key:** `App:UpdateManifestUrl` in `%ProgramData%\EZPos\config.ini`

**Flow on every startup:**
1. `MainWindow_Loaded` fires → `CheckForUpdatesOnStartupAsync()` runs silently in background
2. Fetches `latest.json` (10s timeout — fails silently if offline)
3. Compares remote `version` vs local assembly version
4. If same → nothing shown
5. If newer → `UpdateAvailableDialog` shown with release notes + Update Now / Skip
6. On Update Now → downloads installer to `%TEMP%`, verifies SHA256, creates pre-update DB backup, runs installer silently, closes app

**Release pipeline (automated):**
1. Bump `<Version>` in `EZPos.csproj` → push to `main`
2. `auto-tag-from-csproj.yml` creates git tag `v<Version>`
3. Builds installer → computes SHA256 → generates `latest.json`
4. Publishes installer + `latest.json` to GitHub Release
5. Syncs `latest.json` to `Reef-hash/EZPos-Update-System` (GitHub Pages) via `UPDATE_MANIFEST_REPO_TOKEN` secret

---

## Phase Completion Status

| Phase | Description | Status |
|---|---|---|
| 0 | Foundation — UI theme, icons, CI/CD, error handler | ✅ Complete |
| 1 | Core stability — DB schema, repositories, services, state store | ✅ Complete |
| 2 | Business logic — product CRUD, stock adjust, sale checkout, receipts | ✅ Complete |
| 3 | Data & reporting — ReportService, dashboard live data, stock audit trail | ✅ Complete |
| 4 | UI polish — settings, categories, PDF/Excel export, DB backup/restore | ✅ Complete |
| 4.5 | Licensing architecture — ILicenseService, storage, startup check wired | ✅ Complete |
| 4.7 | Data migration — %ProgramData% location, migration logic in App.xaml.cs | ✅ Complete |
| 4.8 | Auto-update — UpdaterService, UpdateAvailableDialog, Settings check button | ✅ Complete |
| 4.9 | Update manifest hosting — latest.json on GitHub Pages, CI/CD publishing | ✅ Complete |
| 5 | Trial licensing — TrialLicenseService, TrialExpiredWindow, Inno Setup trial.dat | ✅ Complete |
| 5-next | Real HWID/online licensing - replace TrialLicenseService internals only | Pending |

---

## NuGet Dependencies

| Package | Version | Purpose |
|---|---|---|
| `System.Data.SQLite` | 1.0.117 | SQLite database |
| `FontAwesome.Sharp` | 6.3.0 | WPF icon library |
| `ClosedXML` | 0.102.2 | Excel (.xlsx) export |
| `PdfSharpCore` | 1.3.67 | PDF export |
| `Microsoft.VisualBasic` | 10.3.0 | FileSystem helpers |

---

## Design System

**Colors:**
- Primary: `#00D9FF` (Cyan) / Dark: `#00B8D4`
- Sidebar: `#0F172A` / Content: `#1E293B` / Cards: `#334155`
- Text: `#F1F5F9` / Secondary: `#94A3B8` / Muted: `#64748B`
- Success: `#10B981` / Warning: `#F59E0B` / Error: `#EF4444`

**Icons:** FontAwesome.Sharp v6.3.0

---

## Build & Release

```bash
# Local build
dotnet build --configuration Release

# Publish for installer
dotnet publish -c Release -o publish

# Build installer (requires Inno Setup)
ISCC.exe /DAppVersion=1.0.6 InnoSetup-EZPos.iss
```

**To release a new version:**
1. Update `<Version>` in `EZPos.csproj`
2. Commit + push to `main`
3. CI creates tag, builds installer, publishes release and manifest automatically

---

## Future Work

### Planned (next release)

| # | Feature | Notes |
|---|---------|-------|
| 1 | **Auto-print receipt after checkout** | Toggle in Settings ? Receipt Printer section. When enabled, `ReceiptDialog` fires ESC/POS print automatically on open � no manual button click needed. |
| 2 | **StockMovement audit on initial product registration** | When a product is added with opening stock > 0, write a `StockMovements` row with `Reason = OPENING_BALANCE`. History tab on Stock page will show the opening entry instead of starting blank. |
| 3 | **Barcode scan pre-fills BarcodeBox in ProductDialog (Add mode)** | Wire `SalesKeyboardInputService` into `ProductDialog` so scanning while the "Add Product" dialog is open auto-fills the barcode field, even when focus is on another field (e.g. Name). |
| 4 | **Low stock warning during sale** | After checkout, if any sold product drops to = `ReorderLevel`, show a non-blocking toast on the receipt: "Milo Tin: 3 units left, reorder soon." |
| 5 | **Unsaved cart protection** | If client navigates away from Sales page with items in cart, show a confirmation dialog before discarding. |

### Long-term

- **Real licensing** - HWID/online activation via `LicenseService` + `LicenseApiClient`
- **Role-based access** - `Security/Authentication/` + `Security/Authorization/` stubs ready
- **Hardware layer** - `BarcodeService` (HID input buffer) + `PrinterService` (ESC/POS retry wrapper) around existing `EscPosDocument` + `RawPrinterHelper`
- **Multi-user / SAAS** - cloud DB sync, per-user sessions (long-term)
