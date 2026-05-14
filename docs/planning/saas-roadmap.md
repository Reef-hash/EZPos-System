# SaaS & Long-Term Roadmap — EZPos

> Long-term vision for EZPos beyond the current desktop single-store product.

---

## Current State (v1.1.x)

EZPos is a **standalone Windows desktop POS** — single store, single machine, local SQLite database.

The architecture was intentionally designed to be layered and service-oriented so that SAAS multi-user deployment is achievable without a full rewrite.

---

## Phase: Real HWID / Online Licensing

**Trigger:** When the business is ready to sell licenses commercially.

**What changes:**
- Replace `TrialLicenseService` with `LicenseService(new FileLicenseStorage(), new LicenseApiClient())`
- Implement `LicenseApiClient.cs` in `src/Infrastructure/Licensing/` to call a licensing API (Stripe, custom, etc.)
- `LicenseRequiredWindow` becomes active for `LicenseStatus.NotActivated`

**What stays the same:** `ILicenseService` contract, startup routing in `App.xaml.cs`, all UI except the implementation swap.

---

## Phase: Role-Based Access

**Stubs already in place:**
- `src/Security/Authentication/` — empty, reserved for login service
- `src/Security/Authorization/` — empty, reserved for permission checks

**What to build:**
- User table in SQLite (username, hashed password, role)
- Login screen on startup (after license check, before MainWindow)
- Role enum: Admin, Cashier, Manager
- Authorization helper: `AuthorizationService.RequireRole(role)` — gates feature access

**UI changes:** Hide or disable nav items based on current user role.

---

## Phase: Multi-Store / Cloud Sync

**Prerequisites:** Role-based access must be complete first.

**What changes:**
- Replace SQLite with a remote DB (SQL Server / PostgreSQL / cloud SQLite)
- `Database.cs` connection factory becomes configurable (local vs cloud)
- `PosStateStore` sync becomes event-driven (WebSocket or polling)
- `ConfigHelper` reads from cloud config endpoint, not local `config.ini`

**Key design principle:** Because Services never reference SQLite directly (only Repositories do), swapping the DB layer only requires changes to `Database.cs` and the Repository classes.

---

## Phase: Hardware Layer Abstraction

**Current state:** `EscPosDocument` + `RawPrinterHelper` are used directly.

**Future:**
- `PrinterService` wraps `EscPosDocument` + `RawPrinterHelper` with retry logic and error reporting
- `BarcodeService` wraps `SalesKeyboardInputService` with configurable scanner profiles
- Both services injectable — makes unit testing possible

---

## Phase: Dashboard & Analytics (Web/Cloud)

- Web dashboard for store owner: daily/monthly revenue, top products, low stock alerts
- Multi-store comparison view
- Sales trends and forecasting
- Accessible from mobile without the desktop app

---

## Phase: Digital Payment Integration

- QR code generation for e-wallet payment (DuitNow, GrabPay, Touch 'n Go)
- Real-time payment confirmation webhook
- Receipt auto-generated on payment confirmation

---

## Architecture Readiness

| Future feature | Current readiness |
|---|---|
| Real licensing | ✅ Contract ready, one-line swap |
| Role-based access | ✅ Stubs in place |
| Cloud DB | ✅ Repository pattern isolates DB layer |
| Hardware abstraction | ✅ Utilities isolated, easy to wrap |
| Multi-store | ⚠️ Needs PosStateStore refactor for multi-tenant |
| Web dashboard | ⚠️ Needs API layer on top of Services |
