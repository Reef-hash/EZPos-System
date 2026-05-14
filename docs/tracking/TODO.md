# TODO — EZPos

> Near-term implementation tasks. Move completed items to FEATURE_STATUS.md.

---

## In Progress

_Nothing in progress currently._

---

## Next Up

### UI Modernization — MahApps + MaterialDesign
- **Plan doc:** [ui-modernization.md](../planning/ui-modernization.md)
- **Branch:** `feature/ui-modernization` (create before starting)
- **Phase 1:** Install packages + merge ResourceDictionaries — additive, low risk
- **Phase 2:** Migrate `MainWindow` to `mah:MetroWindow`
- **Phase 3:** Replace button/input styles with MaterialDesign equivalents across all 6 pages + 6 dialogs
- **Phase 4:** Migrate 6 dialogs to `MaterialDesign.DialogHost` overlay pattern
- **Phase 5:** Apply `MaterialDesignDataGrid` style to ProductsPage, StockPage, ReportsPage
- **Phase 6:** Add `Snackbar` notification system — replace non-destructive `MessageBox` calls
- **Do not change:** `ReceiptDialog`, `UpdateAvailableDialog`, `TrialExpiredWindow`, `NavButtonStyle` — see plan doc

### Low Stock Warning During Sale
- **Where:** `src/UI/Dialogs/ReceiptDialog.xaml.cs`
- **What:** After checkout, check each sold product's current stock vs `ReorderLevel`. If any are at or below, show a non-blocking notice in the receipt dialog.
- **Approach:** After `LineItemsControl.ItemsSource` is set, iterate `result.Lines`, look up each product in `PosStateStore`, compare stock to reorder level. Show a warning panel if any matches.
- **Docs:** [sales-module.md](../features/sales/sales-module.md)

### Unsaved Cart Protection
- **Where:** `src/UI/Pages/SalesPage.xaml.cs`
- **What:** If cart has items and user clicks away in navigation, prompt: "You have items in your cart. Leave this page?"
- **Approach:** Intercept navigation in `SalesPage_Unloaded` or via a hook in `NavigationService`. Check if any cart tab has items. If yes, `MessageBox.Show` with Yes/No. Cancel navigation on No.
- **Docs:** [sales-module.md](../features/sales/sales-module.md)

---

## Backlog

- Bulk product import (CSV/Excel)
- Product image support
- Scheduled report email
- Sales trend forecasting
- Split payment (multiple methods per sale)
- **QR payment auto-detection** — Billplz/DuitNow gateway, polling, live QR in PaymentDialog — see [future-features.md](../planning/future-features.md)
- **Card payment auto-detection** — PAX EDC terminal SDK, auto-confirm on approval code — see [future-features.md](../planning/future-features.md)

---

## Recently Completed

- [x] Cost Price field on Product (optional/nullable) — DB migration via `TryAddColumn` (May 2026)
- [x] Live profit calculator in ProductDialog — profit/unit, margin %, markup %, colour-coded status (May 2026)
- [x] Show/Hide Cost Price column toggle on ProductsPage grid (May 2026)
- [x] Estimated gross profit + margin KPI band on ReportsPage (hidden when no cost data) (May 2026)
- [x] Excel export: profit rows in Summary sheet, Cost/Profit/Margin columns in Stock Snapshot sheet (May 2026)
- [x] Auto-print receipt after checkout — `AutoPrint` in config, `ReceiptDialog_Loaded` (May 2026)
- [x] Opening balance StockMovement on product registration — `OPENING_BALANCE` reason (May 2026)
- [x] Barcode scan auto-fills BarcodeBox in ProductDialog Add mode (May 2026)
- [x] Settings About section shows dynamic version from assembly (May 2026)
- [x] config.ini seeded by installer to fix auto-update on fresh installs (May 2026)
- [x] TrialExpiredWindow — Catalysm Inc branding (May 2026)
- [x] TrialLicenseService — 30-day date-based trial (May 2026)
- [x] Auto-update system end-to-end (May 2026)
