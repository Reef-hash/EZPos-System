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
| 5-next | Real HWID/online licensing | 🔲 Pending |

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
- [x] LicenseService (mock, wired for future API)
- [x] FileLicenseStorage
- [x] LicenseApiClient stub
- [x] LicenseRequiredWindow (key-entry, ready for real licensing)
- [ ] Real HWID/online activation

Status: **TRIAL COMPLETE — Real licensing pending**

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
