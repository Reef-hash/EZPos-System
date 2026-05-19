# Licensing System — EZPos

> End-to-end documentation for the licensing system: architecture, web backend (Fasa 1), desktop integration (Fasa 2), grace period, and UI flow.

---

## Architecture Overview

The licensing system uses an interface contract so the implementation can be swapped without touching any other code.

```
App.xaml.cs (startup)
  └── ILicenseService
        ├── LicenseService        ← ACTIVE (API-based validation)
        │     ├── ILicenseStorage
        │     │     └── FileLicenseStorage  ← stores key in license.dat
        │     └── LicenseApiClient          ← HTTP calls to web backend
        │           └── POST /api/licenses/validate
        └── TrialLicenseService   ← LEGACY (date-based 30-day trial, now replaced)
```

---

## Phase Overview

| Fasa | Scope | Status |
|------|-------|--------|
| Fasa 1 | Web backend — Stripe payment, key generation, `/api/licenses/validate` | ✅ Done |
| Fasa 2 | Desktop client — `LicenseApiClient`, `LicenseService`, grace-period cache | ✅ Done |
| Fasa 3 | Admin panel — view/deactivate keys, reset device binding | ✅ Done |

---

## Interface Contract

```csharp
// src/Core/Licensing/ILicenseService.cs
public interface ILicenseService
{
    LicenseInfo Current { get; }
    bool IsLicensed { get; }
    LicenseInfo LoadAndValidate();
    LicenseInfo Activate(string key);
}

// LicenseStatus enum
public enum LicenseStatus
{
    Valid,         // key verified online or within grace period
    Invalid,       // key exists but rejected by API
    Expired,       // grace period has lapsed (offline > 7 days)
    Missing,       // no key file found
    NotActivated   // reserved for future subscription flow
}
```

---

## Fasa 1 — Web Backend

### Web Project Location (active)
```
EZPos-System/EZPos-Web/src/EZPos.Web.Ui/
```
Both copies are kept in sync:
- `D:\#_Programming\#_Github_Repo\Production-Repository\EZPos-Web\src\EZPos.Web.Ui\`
- `D:\#_Programming\#_Github_Repo\Production-Repository\EZPos-System\EZPos-Web\src\EZPos.Web.Ui\`

### Dev Server
```
http://localhost:5122   (HTTP — use for Stripe CLI forwarding)
https://localhost:7000  (HTTPS)
```

### License Key Format
```
EZPOS-XXXX-XXXX-XXXX   (uppercase alphanumeric, cryptographically random)
```
Generated with `RandomNumberGenerator.GetBytes(12)` — no `System.Random`.

### Payment Flow
1. User visits `/Payment/Buy` → Stripe checkout (RM 499 MYR, one-time).
2. Stripe redirects to `/Payment/Success?session_id=...`
3. `PaymentController.Success()` verifies the session with the Stripe API (`PaymentStatus == "paid"`) before generating a key.
4. Key is written to the `Licenses` table and displayed on screen.
5. Webhook (`POST /Payment/Webhook`) handles `checkout.session.completed` as a backup — covers users who close the browser before the success page loads.

### API Endpoint: Validate License

```
POST /api/licenses/validate
Content-Type: application/json

{ "licenseKey": "EZPOS-XXXX-XXXX-XXXX" }
```

**Response (200 OK):**
```json
{ "isValid": true,  "message": "License is valid." }
{ "isValid": false, "message": "License key not found." }
```

**Controller:** `src/EZPos.Web.Ui/Controllers/LicensesController.cs`

### Database: License Table
```
Id              INTEGER  PRIMARY KEY
KeyString       TEXT     UNIQUE
IsActive        BOOLEAN
CustomerEmail   TEXT
StripeSessionId TEXT
CreatedAt       DATETIME
```

### Stripe Configuration (appsettings.json)
```json
"Stripe": {
  "SecretKey":      "sk_test_...",
  "PublishableKey": "pk_test_...",
  "WebhookSecret":  "whsec_..."
}
```
For production: replace test keys with live keys from Stripe Dashboard.

### Stripe CLI — Local Webhook Testing
```bash
# In E:\stripe_1.40.9_windows_x86_64\
stripe listen --forward-to http://localhost:5122/Payment/Webhook
```

---

## Fasa 2 — Desktop Integration

### Files Changed

| File | Change |
|------|--------|
| `src/Infrastructure/Licensing/LicenseApiClient.cs` | Replaced placeholder with real HTTP client |
| `src/Infrastructure/Licensing/LicenseValidationCache.cs` | **New** — grace period cache |
| `src/Core/Licensing/LicenseService.cs` | Replaced mock with real API + cache logic |
| `App.xaml.cs` | Swapped `TrialLicenseService` → `LicenseService` |
| `Config/config.ini` | Added `App:LicenseApiUrl` |

### LicenseApiClient

**File:** `src/Infrastructure/Licensing/LicenseApiClient.cs`

Calls `POST {baseUrl}/api/licenses/validate` with an 8-second timeout.

```csharp
var apiClient = new LicenseApiClient("http://localhost:5122");
var result    = await apiClient.ValidateAsync("EZPOS-XXXX-XXXX-XXXX");
// result.IsValid   — key accepted
// result.IsOffline — network unreachable (not an invalid key)
// result.Message   — human-readable detail
```

Uses `System.Net.Http.Json` (built into .NET 6 — no extra NuGet required).  
One static `HttpClient` instance is reused across all calls.

### LicenseValidationCache (Grace Period)

**File:** `src/Infrastructure/Licensing/LicenseValidationCache.cs`  
**Cache file:** `%ProgramData%\EZPos\license-cache.dat`

```
LAST_VALIDATED=2026-05-17T14:48:00Z
KEY=EZPOS-XXXX-XXXX-XXXX
STATUS=Valid
```

| Method | Description |
|--------|-------------|
| `SaveValid(key)` | Writes cache with current UTC time. Call after every successful API validation. |
| `IsWithinGracePeriod(key)` | Returns `true` if STATUS=Valid, KEY matches, and LAST_VALIDATED ≤ 7 days ago. |

Grace period: **7 days** (`LicenseValidationCache.GracePeriodDays`).  
All I/O errors are swallowed — cache is a best-effort, non-critical path.

### LicenseService

**File:** `src/Core/Licensing/LicenseService.cs`  
**Constructor:** `LicenseService(ILicenseStorage storage, LicenseApiClient apiClient)`

#### LoadAndValidate() — Decision Tree

```
Load key from license.dat
  │
  ├── No key → Missing
  │
  └── Call POST /api/licenses/validate (8s timeout)
        │
        ├── API online + isValid=true  → SaveCache → Valid
        ├── API online + isValid=false → Invalid
        │
        └── API offline (IsOffline=true)
              ├── Cache ≤ 7 days + key matches → Valid (grace period)
              └── Cache expired / missing      → Expired
```

Uses `Task.Run(() => _apiClient.ValidateAsync(key)).GetAwaiter().GetResult()` to avoid WPF UI-thread deadlock while keeping `ILicenseService` synchronous.

#### Activate(key) — Called by LicenseRequiredWindow

```
Trim + uppercase key
  └── Call POST /api/licenses/validate
        ├── IsValid=true → SaveKey to disk → SaveCache → Valid
        └── IsValid=false or offline → Invalid (key NOT saved)
```

### Config: LicenseApiUrl

**File:** `Config/config.ini`
```ini
App:LicenseApiUrl=http://localhost:5122
```
Read in `App.xaml.cs`:
```csharp
var apiUrl = ConfigHelper.Get("App:LicenseApiUrl", "http://localhost:5122");
```

**Before production deployment:** Update this value to the live domain (e.g., `https://ezpos-web.azurewebsites.net`).  
The fallback default `http://localhost:5122` is only used if the key is absent from `%ProgramData%\EZPos\config.ini`.

### File Locations (Runtime)

```
%ProgramData%\EZPos\
  ├── config.ini          — app settings (LicenseApiUrl, StoreName, etc.)
  ├── license.dat         — stored license key (written by FileLicenseStorage)
  ├── license-cache.dat   — grace period cache (written by LicenseValidationCache)
  └── trial.dat           — LEGACY: old trial install date (no longer used)
```

---

## Startup Flow (App.xaml.cs)

```
OnStartup()
  ↓
MigrateToNewDataLocation()
Database.Initialize()
  ↓
var apiUrl    = ConfigHelper.Get("App:LicenseApiUrl", "http://localhost:5122")
var apiClient = new LicenseApiClient(apiUrl)
ILicenseService licenseService = new LicenseService(new FileLicenseStorage(), apiClient)
licenseService.LoadAndValidate()
  ↓
switch (licenseService.Current.Status)
  ├── Valid         → continue to MainWindow
  ├── Missing       → show LicenseRequiredWindow
  ├── Invalid       → show LicenseRequiredWindow (key rejected)
  ├── NotActivated  → show LicenseRequiredWindow
  └── Expired       → show TrialExpiredWindow → Shutdown(1)
```

---

## UI Windows

### LicenseRequiredWindow

**File:** `src/UI/Licensing/LicenseRequiredWindow.xaml`

Shown for `Missing`, `Invalid`, `NotActivated` statuses.  
User enters their license key. On submit:
- Calls `licenseService.Activate(key)`
- If Valid → `DialogResult = true` → app continues
- If Invalid → shows error message, user can retry
- Close/cancel → `DialogResult = false` → `App.xaml.cs` calls `Shutdown(0)`

### TrialExpiredWindow

**File:** `src/UI/Licensing/TrialExpiredWindow.xaml`

Shown when `LicenseStatus.Expired` (grace period lapsed).  
Full-screen modal — cannot be bypassed.  
Shows contact info: **Catalysm Inc / Zarif El-Mansour / 019-5778954 (WhatsApp only)**.

---

## Testing

### Successful Purchase Test
1. Start the web app: `dotnet run` in `EZPos-Web/src/EZPos.Web.Ui/`
2. Start Stripe CLI: `stripe listen --forward-to http://localhost:5122/Payment/Webhook`
3. Visit `http://localhost:5122/Payment/Buy`
4. Use Stripe test card: `4242 4242 4242 4242` (any future date, any CVC)
5. Copy the key from the success page

### Desktop Validation Test
1. Ensure EZPos web is running at `http://localhost:5122`
2. Delete (or rename) `%ProgramData%\EZPos\license.dat` to force `Missing` status
3. Delete `%ProgramData%\EZPos\license-cache.dat` to clear grace period cache
4. Launch EZPos — `LicenseRequiredWindow` should appear
5. Enter the test key — app should open if key is in the database
6. Check web server logs — should show `Key EZPOS-XXXX bound to device XXXXXXXXXXXXXXXX`

### Device Binding Test
1. Activate key on Machine A → works ✅
2. Copy `license.dat` to Machine B (or change registry/NIC to simulate different device)
3. Launch EZPos on Machine B → should show `LicenseRequiredWindow` with error:
   `"This license key is already in use on another machine."`

### Grace Period Test
1. Save a valid key to `license.dat`
2. Stop the web server
3. Launch EZPos — should start (grace period cache active)
4. Manually set `LAST_VALIDATED` in `license-cache.dat` to 8+ days ago
5. Launch EZPos again — should show `TrialExpiredWindow`

---

## Production Deployment Checklist

### Step 1 — Prepare Web Server

- [ ] Choose hosting platform (Railway free tier, Azure App Service, Fly.io, etc.)
- [ ] Deploy `EZPos-Web/src/EZPos.Web.Ui/` to the platform
- [ ] Note the public URL (e.g., `https://ezpos-licensing.up.railway.app`)

### Step 2 — Configure Stripe (Live Mode)

- [ ] Login Stripe Dashboard → toggle **Test → Live** (top-left corner)
- [ ] Copy live keys:
  - `sk_live_...` → `appsettings.json` `Stripe:SecretKey`
  - `pk_live_...` → `appsettings.json` `Stripe:PublishableKey`
- [ ] Stripe Dashboard → Developers → Webhooks → **Add endpoint**
  - URL: `https://your-public-url/Payment/Webhook`
  - Event: `checkout.session.completed`
  - Copy the new `whsec_...` → `appsettings.json` `Stripe:WebhookSecret`
- [ ] Update price in `PaymentController.cs` if needed (currently RM 499 = `49900` in cents)

### Step 3 — Remove Development Flags

- [ ] **`Program.cs` (both copies)** — remove `EnsureDeleted()` line:
  ```csharp
  // REMOVE THIS LINE before production:
  dbContext.Database.EnsureDeleted();
  ```
  Keep `EnsureCreated()` — it is safe (skips if schema already exists).

### Step 4 — Update EZPos Desktop Config

- [ ] Edit `Config/config.ini` — change `LicenseApiUrl` to production URL:
  ```ini
  App:LicenseApiUrl=https://your-public-url
  ```
- [ ] Also update `%ProgramData%\EZPos\config.ini` on any machine that already has EZPos installed

### Step 5 — Build & Distribute

- [ ] `dotnet publish -c Release` for EZPos desktop
- [ ] Build Inno Setup installer (`InnoSetup-EZPos.iss`)
- [ ] Test installer on a clean machine (no existing `%ProgramData%\EZPos\`)
- [ ] Verify: install → license window → enter key → main window opens

### Step 6 — Post-Launch

- [ ] Stripe Dashboard → Payments — verify test purchase shows up
- [ ] Monitor web server logs for any binding errors
- [ ] Keep admin panel (Fasa 3) in mind for handling "transfer device" support requests

---

## Inno Setup — config.ini Creation

```pascal
[Files]
Source: "Config\config.ini"; DestDir: "{commonappdata}\EZPos"; Flags: onlyifdoesntexist
```

`onlyifdoesntexist` means reinstalling never overwrites the user's existing config (e.g., their `PrinterName` settings). The default `App:LicenseApiUrl` in `Config/config.ini` is only written on first install.

---

## Fasa 3 — Admin Panel

### Overview

A browser-based admin panel for managing license keys. Access is protected by cookie-based authentication. No public registration — credentials are stored in `appsettings.json`.

### Access URL
```
http://localhost:5122/Admin          → redirects to /Admin/Login if not authenticated
http://localhost:5122/Admin/Login    → login page
http://localhost:5122/Admin/Dashboard → license management dashboard
```

### Default Credentials
| Field    | Value              |
|----------|--------------------|
| Username | `admin`            |
| Password | `ezpos@admin2026`  |

**⚠️ Change these before production deployment** (see `appsettings.json` → `Admin:Username`, `Admin:Password`).

### Authentication

**Scheme:** ASP.NET Core Cookie Authentication (`"AdminCookie"`)

**Cookie settings:**
- Name: `EZPosAdmin`
- HttpOnly: `true` (not accessible via JavaScript)
- SameSite: `Strict`
- SecurePolicy: `SameAsRequest` (HTTPS in production, HTTP in dev)
- Session duration: **8 hours** with sliding expiration
- Login redirect: `/Admin/Login`

**Configuration in `Program.cs`:**
```csharp
builder.Services.AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", options =>
    {
        options.LoginPath           = "/Admin/Login";
        options.Cookie.Name         = "EZPosAdmin";
        options.Cookie.HttpOnly     = true;
        options.Cookie.SameSite     = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan      = TimeSpan.FromHours(8);
        options.SlidingExpiration   = true;
    });

// Must be before UseAuthorization():
app.UseAuthentication();
app.UseAuthorization();
```

### Controller Routes

**File:** `src/EZPos.Web.Ui/Controllers/AdminController.cs`

| Method | Route                     | Auth | Description |
|--------|---------------------------|------|-------------|
| GET    | `/Admin/Login`            | No   | Show login form |
| POST   | `/Admin/Login`            | No   | Process login; redirect to Dashboard on success |
| POST   | `/Admin/Logout`           | No   | Sign out; redirect to Login |
| GET    | `/Admin/Dashboard`        | Yes  | License list with stats and search |
| POST   | `/Admin/Deactivate/{id}`  | Yes  | Set `IsActive = false` |
| POST   | `/Admin/Activate/{id}`    | Yes  | Set `IsActive = true` (reactivate) |
| POST   | `/Admin/ResetDevice/{id}` | Yes  | Set `DeviceId = null` |

All POST actions use `[ValidateAntiForgeryToken]` (CSRF protection).

### Dashboard Features

- **Stats bar:** Total Keys / Active / Deactivated counts
- **Search:** Filter by license key or email (GET param `?search=...`)
- **Table columns:** #, License Key, Email, Purchase Date, Device ID (truncated, full value in tooltip), Status badge, Actions
- **Per-row actions:**
  - **Deactivate** (shown when key is active) — disables key; device will be rejected on next validation
  - **Activate** (shown when key is deactivated) — re-enables key
  - **Reset Device** (shown only when `DeviceId` is not null) — clears device binding so key can be activated on a new machine

### Reset Device — Use Case

When a customer changes their PC (hard drive replaced, new computer, etc.), their license will fail validation because the `DeviceId` no longer matches.

**Resolution flow:**
1. Customer contacts support
2. Admin logs in to `/Admin/Dashboard`
3. Finds the customer's key by email or key string
4. Clicks **Reset Device** on that row
5. Customer activates the key on their new machine — it binds to the new device fingerprint

### Views

**`Views/Admin/Login.cshtml`**
- Standalone page (`Layout = null`) — no shared layout dependency
- Dark theme matching EZPos branding (amber `#FFC107` accent)
- Username + password fields with CSRF token
- Displays error message on invalid credentials

**`Views/Admin/Dashboard.cshtml`**
- Standalone dark-themed page
- All action forms include `@Html.AntiForgeryToken()`
- Deactivate action shows a browser `confirm()` dialog before submitting
- Flash messages shown via `TempData["Message"]` after successful actions

### Security Notes

- Credentials are stored in `appsettings.json` (server-side only — never exposed to clients)
- All admin actions require an authenticated session cookie
- Login failures are logged via `ILogger` (with username, without password)
- All destructive actions use HTTP POST with CSRF tokens — not GET links
- Cookie is `HttpOnly` + `SameSite=Strict` — mitigates XSS and CSRF

### Production Setup — Admin Panel

- [ ] Change `Admin:Username` and `Admin:Password` in `appsettings.json` before deployment
- [ ] Ensure the app runs over HTTPS so the cookie is transmitted securely
- [ ] Consider restricting `/Admin/*` routes to a VPN or IP allowlist via reverse proxy (optional, extra hardening)
- [ ] Do not expose `appsettings.json` via any static file route
