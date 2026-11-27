# EZPos Electron Rewrite — Plan

> **Status:** PLANNING. Scope agreed 2026-11-28. Phase 0 not started.
> **Supersedes:** [ui-modernization.md](ui-modernization.md) (WPF Graphite + Amber restyle). Don't do any more UI restyling in the WPF app.
> **Reference app:** EZOffice (`C:\Users\GM\Documents\Production\EZOffice`). Its stack, shared components and `docs/DESIGN_SYSTEM.md` are the source of truth for the look of the new app.

---

## 1. Decision summary

| Topic | Decision |
|---|---|
| Approach | Rebuild EZPos as an **Electron + React + TypeScript + Tailwind** app. Don't restyle the WPF XAML. |
| Packaging | **Separate app and repo.** It is not a module inside EZOffice. It copies EZOffice's shared components and design tokens rather than depending on them. |
| Target OS | Windows 10/11 only. All clients are confirmed to meet this. |
| Scope | **Full parity** with WPF v1.7.x. **Nothing is removed**, including features that are rarely used now (for example Cheque payment and the tax modes), because future clients may need them. Barcodes is carried over fully and improved. |
| Data model | Fix sales data gaps **during** the rewrite. See §4. |
| WPF app during rewrite | Bugfix only. No new features. |
| Rollout | A final "bridge" WPF update installs the Electron app. The Electron app opens the same `%ProgramData%\EZPos\` data in place. |

---

## 2. Feature inventory (parity checklist)

Every row must work in Electron before any client is migrated.

### Sales
- [ ] Multiple transaction tabs: new, hold, switch, close
- [ ] Global barcode scan capture (keyboard-wedge scanner, anywhere on the Sales screen)
- [ ] Product search and category filter, product grid
- [ ] Cart: add, increase or decrease quantity, edit quantity inline, remove, clear
- [ ] Weighed products (weight input dialog)
- [ ] Sales are blocked unless a shift is open
- [ ] Tax calculation per configured tax mode and rate

### Payment
- [ ] Cash, Card, QR and Cheque payment methods
- [ ] Configurable hotkeys (default F1–F4)
- [ ] Quick-amount buttons, change calculation
- [ ] Full keyboard and arrow-key navigation

### Receipt
- [ ] ESC/POS raw print to the receipt printer
- [ ] Fallback to a normal driver print
- [ ] Hotkeys for new sale and print (default PageUp/PageDown)

### Products
- [ ] Create, edit, view and delete products. Double-click opens the product.
- [ ] Search, plus scan-to-register when a scanned barcode is unknown
- [ ] Filters and counters, toggle for the cost column
- [ ] Category management (add, rename, delete)
- [ ] Barcode generation, barcode format
- [ ] Unit types, packs and parent product with conversion rate
- [ ] Cost price and profit preview

### Stock
- [ ] Stock in, stock out, edit stock (adjust dialog with preview)
- [ ] Filters by stock status and by category, summary
- [ ] Offer to print labels after stock in
- [ ] Stock movement history

### Barcodes (carried over fully and improved)
- [ ] Product selection: by row, select all, by category
- [ ] Print job queue, preview
- [ ] Print labels (Xprinter XP-365B), export to PDF
- [ ] Label template editor: new, save, delete
- [ ] Print history log
- [ ] Quick print dialog (from Products and Stock)
- [ ] Scan to add to the print queue

### Cash Drawer
- [ ] Open shift with opening balance
- [ ] Close shift: expected vs actual cash, discrepancy preview, payment breakdown
- [ ] Shift history

### Reports
- [ ] Period summary, daily breakdown, hourly breakdown
- [ ] Top products, transactions list, payment breakdown
- [ ] Stock snapshot, low-stock alerts
- [ ] Export to Excel and PDF

### Dashboard
- [ ] KPIs: today's revenue, transactions today, low-stock count, average order value
- [ ] Low-stock table

### Settings
- [ ] Store name, currency, receipt footer
- [ ] Printer selection and detection
- [ ] Tax rate and tax mode
- [ ] Payment and receipt hotkeys
- [ ] Database backup and restore
- [ ] Check for updates

### Licensing
- [ ] Trial
- [ ] Activation via license API (`App:LicenseApiUrl`)
- [ ] Revalidate, offline validation cache
- [ ] Transfer via WhatsApp
- [ ] **Existing activated clients must stay licensed after migration.** See §5.

---

## 3. Target architecture

Mirror EZOffice's layout:

```
electron/
  main.ts            app lifecycle, window, updater
  db/                better-sqlite3 connection + versioned migrations
  ipc/               one handler file per module (products, sales, stock, ...)
  services/          business logic (port of src/Business/Services)
  hardware/          winspool raw printing (koffi), printer enumeration
  licensing/         port of Core/Licensing + Infrastructure/Licensing
src/
  modules/           sales, products, stock, barcodes, cash-drawer, reports, dashboard, settings
  shared/            components copied from EZOffice (Button, Card, Table, Modal, Input, StatusBadge, Toast, PageHeader, AppShell)
```

- **Business logic lives in the main process.** The renderer only talks to it over typed IPC, with zod validation as in EZOffice.
- **Data location doesn't change.** It stays in `%ProgramData%\EZPos\`: `EZPos.db`, `config.ini`, `license.dat`, the trial file and the validation cache.
- **Updates** use `electron-updater`, replacing the GitHub Pages `latest.json` manifest.

---

## 4. Data model changes

These are bundled with the rewrite so that the schema only has to be migrated once.

| Change | Why |
|---|---|
| Add a `SchemaVersion` table and numbered migrations | The current approach adds columns ad hoc with `TryAddColumn`. |
| `Sales`: add `Subtotal`, `TaxAmount`, `DiscountAmount`, `AmountPaid`, `ChangeGiven`, `ShiftId` | So that receipts can be reprinted exactly and sales can be tied to a shift. |
| `SaleItems`: add `CostPrice`, `ProductName` (snapshot) | Profit reports stay correct after a product's cost or name changes. |
| `Products.Category` (text): add `CategoryId` foreign key, keep the text column during transition | Renaming a category currently orphans products. |

Rules for this migration:
- **Back up before migrating.** Copy `EZPos.db` to `EZPos.db.pre-electron-<timestamp>` before the first migration runs.
- **Additive changes only.** No column drops, so the WPF app can still open the database if a client rolls back.
- Old sales rows get `NULL` in the new columns. Reports must handle that.

---

## 5. Risks and how Phase 0 retires them

| Risk | Mitigation and proof required |
|---|---|
| **Raw ESC/POS printing.** The WPF app uses `winspool.drv` P/Invoke ([RawPrinterHelper.cs](../../src/Utilities/Helpers/RawPrinterHelper.cs)). | Call `winspool.drv` from Node via `koffi`. Prove it by printing a receipt and kicking the cash drawer on the real printer. |
| **Label printing on Xprinter.** | Render labels as HTML and print them silently with `webContents.print({ silent, deviceName })`, or send raw TSPL via `koffi`. Prove it with a correctly sized label on the XP-365B. |
| **License device ID mismatch.** [DeviceFingerprint.cs](../../src/Infrastructure/Licensing/DeviceFingerprint.cs) takes the MAC of the first UP, non-loopback, non-tunnel adapter, sorted by .NET `Name`, hashes it with SHA256 and keeps the first 16 hex characters. Node's `os.networkInterfaces()` doesn't expose operational status or adapter type, and its sort order may differ. | Port the logic, then run the C# and Node versions side by side on several machines (desktop, laptop with Wi-Fi plus Ethernet, a machine with VPN or virtual adapters). They must match on every machine. If they can't be made to match reliably, fall back to reading adapters through PowerShell `Get-NetAdapter`. |
| **`license.dat` and trial file format compatibility.** | Read files written by the real WPF app and check that they validate. |
| **Existing DB compatibility.** | Open a copy of a real client DB, run the migrations and check that reports match the WPF output. |
| **Sales screen speed** (scanner input, Electron startup and memory). | Measure time from scan to cart line and cold start time on the lowest-spec client PC. |

---

## 6. Phases

| Phase | Content | Exit criteria |
|---|---|---|
| **0. Spike** | Electron skeleton from the EZOffice template. Receipt print, cash drawer kick, label print, read a real `EZPos.db`, port the device fingerprint and validate an existing license. | Every row in §5 is proven. If any fails, we decide go or no-go before continuing. |
| **1. Foundation** | AppShell and sidebar, design tokens, shared components, DB layer and migrations (§4), config, licensing, updater, installer. | An empty app installs, is licensed and updates itself. |
| **2. Modules** | Products and Categories → Stock → **Sales, Payment, Receipt and Shift** → Cash Drawer → Reports → Barcodes and Labels → Settings → Dashboard. | Every checkbox in §2 is ticked. |
| **3. Verification** | Run against copies of real client DBs. Pilot with 1–2 clients. | No blocking issues for 2 weeks. |
| **4. Migration** | Bridge WPF release: download the Electron installer, run it, then hand over. Document the rollback (reinstall WPF; the DB is still compatible because §4 is additive). | All clients are on Electron. |

---

## 7. Improvement backlog (after parity)

Not part of parity. Prioritise after migration.
- Users, login and roles (shifts are currently recorded as `"Cashier"`)
- Discounts, per item and per receipt
- Refund and void
- Customers and members
- Suppliers and purchase orders
- Barcodes module improvements, to be defined while porting it

---

## 8. Open items

- Name and location of the new repo (proposed: `C:\Users\GM\Documents\Production\EZPos-Desktop`)
- Whether to keep `config.ini` or move settings into the DB. Keep reading `config.ini` for migration either way.
