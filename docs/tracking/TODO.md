# TODO — EZPos

> Near-term implementation tasks. Move completed items to FEATURE_STATUS.md.

---

## In Progress

_Nothing in progress currently._

---

## Next Up

### Barcode Management Module — Phase 1 (MVP)
- **Plan doc:** [barcode-module.md](../features/barcodes/barcode-module.md)
- **Branch:** `feature/barcode-management` (create before starting)
- **Architecture:** MVVM — all new files use ViewModels. Do not add code-behind logic.

**Step-by-step order:**
1. `dotnet add package ZXing.Net` — add to `EZPos.csproj`
2. Create `src/Models/Domain/BarcodeFormat.cs` — enum
3. Create `src/Models/Domain/LabelTemplate.cs` + `LabelPrintJob.cs`
4. Add `BarcodeFormat` column migration in `Database.MigrateProductsTable()`
5. Create `src/Business/Services/BarcodeService.cs`
6. Create `src/DataAccess/Repositories/LabelTemplateRepository.cs` (JSON file)
7. Create `src/UI/ViewModels/RelayCommand.cs`
8. Create `src/Business/Services/LabelPrintService.cs`
9. Create `src/UI/ViewModels/BarcodesPageViewModel.cs`
10. Create `src/UI/Pages/BarcodesPage.xaml` + `.cs`
11. Create `src/UI/ViewModels/QuickPrintDialogViewModel.cs`
12. Create `src/UI/Dialogs/QuickPrintDialog.xaml` + `.cs`
13. Add "Print Label..." button to `ProductsPage.xaml` toolbar
14. Add "Generate Barcode" wand button to `ProductDialog.xaml` (BarcodeBox row)
15. Register `"Barcodes"` in `MainWindow.RegisterRoutes()` + add sidebar nav button

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
- **Stripe FPX** — enable Malaysian online banking (Maybank2u, CIMB Clicks, etc.) in existing Stripe integration:
  - Add `"fpx"` to `PaymentMethodTypes` in `PaymentController.CreateCheckoutSession()`
  - Enable FPX in Stripe Dashboard → Settings → Payment Methods
  - Requires live Stripe account for MYR/Malaysia
- **Alternative payment gateway** — Toyyibpay or Billplz for local bank transfer (requires new controller + API integration)
- **Admin panel — sales history view** — show all purchases with Stripe session ID, refund status
- **Promo: 7-day free trial** — bila nak buat promo, ubah `TrialDurationDays = 7` dalam `TrialLicenseService.cs` + tambah fallback dalam `LicenseService.LoadAndValidate()`: kalau tiada `license.dat`, semak `trial.dat` (3-baris change). Trial expired → `TrialExpiredWindow`. Trial valid → teruskan.

---

## Recently Completed

- [x] Cost Price field on Product (optional/nullable) — DB migration via `TryAddColumn` (May 2026)
- [x] Live profit calculator in ProductDialog — profit/unit, margin %, markup %, colour-coded status (May 2026)
- [x] Show/Hide Cost Price column toggle on ProductsPage grid (May 2026)
- [x] Estimated gross profit + margin KPI band on ReportsPage (hidden when no cost data) (May 2026)
- [x] Excel export: profit rows in Summary sheet, Cost/Profit/Margin columns in Stock Snapshot sheet (May 2026)
- [x] Auto-print receipt after checkout — `AutoPrint` in config, `ReceiptDialog_Loaded` (May 2026)
- [x] Opening balance StockMovement on product registration — `OPENING_BALANCE` reason (May 2026)
- [x] **Real licensing — Fasa 1**: Web backend — Stripe payment, key generation, `/api/licenses/validate` (May 2026)
- [x] **Real licensing — Fasa 2**: Desktop — LicenseApiClient, LicenseValidationCache (7-day grace), LicenseService, DeviceFingerprint, device binding (May 2026)
- [x] **Real licensing — Fasa 3**: Admin panel — login, dashboard, deactivate/activate, reset device, dynamic pricing (May 2026)
- [x] Barcode scan auto-fills BarcodeBox in ProductDialog Add mode (May 2026)
- [x] Settings About section shows dynamic version from assembly (May 2026)
- [x] config.ini seeded by installer to fix auto-update on fresh installs (May 2026)
- [x] TrialExpiredWindow — Catalysm Inc branding (May 2026)
- [x] TrialLicenseService — 30-day date-based trial (May 2026)
- [x] Auto-update system end-to-end (May 2026)
