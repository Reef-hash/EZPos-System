# Data Storage & Runtime Paths — EZPos

---

## Runtime Directory Layout

```
C:\Program Files\EZPos\              ← Read-only. Installer target for binaries.
  ├── EZPos.exe
  ├── EZPos.deps.json
  └── *.dll

C:\ProgramData\EZPos\                ← Read-write. Survives all updates and uninstalls.
  ├── EZPos.db                       ← Live SQLite database (seeded on first install only)
  ├── config.ini                     ← Store settings (seeded on first install only)
  ├── trial.dat                      ← Trial install date (written once, never overwritten)
  ├── license.dat                    ← License key file (future real licensing)
  ├── Backups\
  │   ├── EZPos_Backup_*.db          ← Manual backups created from Settings page
  │   └── EZPos_PreUpdate_*.db       ← Auto-backup created before each update install
  └── Logs\                          ← Reserved for future logging
```

**Key principle:** All user data lives in `%ProgramData%\EZPos\`. Updating the app (overwriting `Program Files`) never touches user data.

---

## config.ini

**Location:** `C:\ProgramData\EZPos\config.ini`
**Seeded by:** Inno Setup installer on first install (`onlyifdoesntexist` flag — never overwritten on update)
**Read/written by:** `ConfigHelper.cs` in `src/DataAccess/Repositories/`

**Default contents:**
```ini
StoreName=EZPos Store
PrinterName=
TaxRate=6
Currency=RM
ReceiptFooter=Thank you, come again!
TaxMode=Fake
PaymentHotkeyCash=F1
PaymentHotkeyQr=F2
PaymentHotkeyCard=F3
PaymentHotkeyCheque=F4
App:UpdateManifestUrl=https://reef-hash.github.io/EZPos-Update-System/latest.json
```

**Config key format:**
- Standard keys: `KeyName=Value`
- App-level keys: `App:KeyName=Value`

---

## SQLite Database

**Location:** `C:\ProgramData\EZPos\EZPos.db`
**Library:** `System.Data.SQLite 1.0.117`
**Managed by:** `Database.cs` — `Initialize()` creates tables if missing, runs migrations

### Tables

| Table | Purpose |
|---|---|
| `Products` | Product catalogue — barcode, name, price, stock, reorder level, category, unit type |
| `Sales` | Sale transactions — date, total, payment method, tendered, change |
| `SaleItems` | Line items per sale — product ID, quantity, unit price |
| `StockMovements` | Audit trail — product ID, change qty (±), reason, datetime |

### StockMovement Reasons

| Reason | Trigger |
|---|---|
| `SALE` | A sale checkout reduces stock |
| `ADJUSTMENT` | Manual stock-in from StockAdjustDialog |
| `CORRECTION` | Manual stock correction |
| `OPENING_BALANCE` | Initial stock set when a product is first registered |

---

## trial.dat

**Location:** `C:\ProgramData\EZPos\trial.dat`
**Written by:** Inno Setup `CurStepChanged(ssPostInstall)` → `InitializeTrialIfNeeded()`
**Guard:** `FileExists()` check — installer never overwrites an existing `trial.dat`
**Read by:** `TrialLicenseService.ReadInstallDate()`

**Format:**
```
INSTALL_DATE=2026-05-07 14:30:00
```

Date is local time, format `yyyy/mm/dd hh:nn:ss` (Inno Setup `GetDateTimeString`).
`TrialLicenseService` handles both local-time format and ISO 8601 with Z suffix.

---

## Database Initialization & Migration

`Database.Initialize()` is called in `App.xaml.cs` before any service is created.

It:
1. Creates all tables with `CREATE TABLE IF NOT EXISTS`
2. Runs schema migrations (ALTER TABLE for new columns added in later versions)
3. Creates `%ProgramData%\EZPos\` directory if missing

This means the app can run on a machine without the installer (dev machines) and the DB will be created automatically.
