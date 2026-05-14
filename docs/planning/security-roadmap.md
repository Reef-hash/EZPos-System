# EZPos Security Roadmap

> Status: **Planned** — not yet implemented.
> Last reviewed: May 2026

This document captures the security audit findings and planned improvements for EZPos.
Items are ordered by priority (critical first).

---

## Current Security Posture

### Already Good ✅
- All SQL uses **parameterized queries** — no SQL injection risk anywhere in the codebase.
- Trial/license files stored in `%ProgramData%` — requires admin rights to tamper.
- DB restore validates SQLite integrity before replacing the live database.
- Backup creates a safety copy before any restore operation.

---

## Planned Security Work

### 🔴 Critical

#### 1. Cashier PIN / Password Login
- **Risk:** Anyone who opens the app can process refunds, change prices, and delete data.
- **Plan:** Implement a login screen (PIN or password) before the main window loads.
  - Store hashed PINs in the database (`Users` table, bcrypt or PBKDF2).
  - Session tracks the logged-in user so actions can be attributed.
- **Files to create/modify:**
  - `src/Core/Auth/` — `IUserService`, `UserService`, `SessionContext`
  - `src/DataAccess/Repositories/UserRepository.cs`
  - `src/UI/Auth/LoginWindow.xaml` + `.cs`
  - `App.xaml.cs` — show `LoginWindow` before `MainWindow`

#### 2. Manager-Gated Settings Page
- **Risk:** Any cashier can change tax rate, printer config, receipt footer — enabling tax fraud.
- **Plan:** Prompt for a manager PIN before entering `SettingsPage`.
- **Files to modify:**
  - `MainWindow.xaml.cs` — intercept Settings nav and show PIN prompt dialog.
  - `src/UI/Auth/ManagerPinDialog.xaml` + `.cs` (new).

#### 3. HMAC-Protected `trial.dat`
- **Risk:** User can open `trial.dat`, change the install date, and reset the 30-day trial infinitely.
- **Plan:** Write a salted HMAC signature alongside the date. On load, verify signature before trusting the date.
- **Files to modify:**
  - `src/Core/Licensing/TrialLicenseService.cs` — add HMAC write/verify helpers.
- **Key:** Derive HMAC key from a machine-specific value (e.g., machine GUID from registry) so the file is not portable.

#### 4. Signed/Encrypted `license.dat`
- **Risk:** License key is stored as plain text; anyone can read or manually write a key.
- **Plan:** Encrypt the stored key with `DPAPI` (`ProtectedData.Protect`) — Windows ties the encryption to the machine and user profile automatically.
- **Files to modify:**
  - `src/Core/Licensing/FileLicenseStorage.cs` — wrap `SaveKey`/`LoadKey` with `ProtectedData`.

---

### 🟡 Medium Priority

#### 5. Audit Log Table
- **Risk:** No record of who voided a sale, changed a price, or adjusted stock.
- **Plan:** Add an `AuditLog` table in SQLite. Log all sensitive actions (sale void, price edit, stock adjustment, settings save, login/logout) with `UserId`, `Action`, `Details`, `DateTime`.
- **Files to create/modify:**
  - `src/DataAccess/Repositories/Database.cs` — add `AuditLog` table in `Initialize()`.
  - `src/DataAccess/Repositories/AuditLogRepository.cs` — `Insert(entry)`, `GetRecent(n)`.
  - Call `AuditLogRepository.Insert(...)` from `SaleService`, `SettingsPage`, future `UserService`.

#### 6. Role-Based Access (Admin / Cashier)
- **Risk:** No separation of privilege — a cashier has the same access as an owner.
- **Plan:** Add a `Role` column to the `Users` table (`Admin`, `Cashier`). Gate pages and actions by role.
- **Architecture stub already exists** in `ARCHITECTURE.md` under `src/Security/Authorization/`.
- **Files to create:**
  - `src/Core/Auth/SessionContext.cs` — holds the currently logged-in user + role.
  - `src/Core/Auth/Permissions.cs` — constants for permission names.
  - Inject `SessionContext` into pages/services that need to gate actions.

#### 7. Code Signing Certificate
- **Risk:** Windows SmartScreen warns users on first run; fake/tampered installers can circulate.
- **Plan:** Purchase an Authenticode certificate (Sectigo EV ~USD 200/yr or OV cheaper).
  - Sign `EZPos.exe` and `Setup-EZPos.exe` in the CI pipeline after build.
- **InnoSetup:** Update `InnoSetup-EZPos.iss` — add `SignTool` directive once cert is available.

---

### 🟢 Low Priority

#### 8. IL Obfuscation (ConfuserEx)
- **Risk:** Source logic (pricing, licensing, trial validation) visible via `dnSpy` / `ILSpy`.
- **Plan:** Add **ConfuserEx** as a post-build step on Release configuration.
  - Download: https://github.com/mkck9/ConfuserEx (maintained fork)
  - Protections to enable: `rename`, `anti debug`, `anti dump`, `constants`.
  - Exclude `Resources/` and public API surface if needed.
- **Note:** Test thoroughly after enabling — obfuscation can break WPF XAML bindings if `rename` is too aggressive. Use `[assembly: InternalsVisibleTo]` workarounds as needed.

#### 9. Anti-Debugging (C# P/Invoke)
- **Risk:** Attacker can attach a debugger, pause execution, and patch license/trial checks.
- **Plan:** Add a check in `App.xaml.cs` `OnStartup` (Release builds only):

```csharp
#if !DEBUG
[DllImport("kernel32.dll")]
static extern bool IsDebuggerPresent();

[DllImport("ntdll.dll", SetLastError = true)]
static extern int NtQueryInformationProcess(
    IntPtr hProcess, int infoClass,
    ref int info, int size, out int returnLen);

private static bool IsBeingDebugged()
{
    if (IsDebuggerPresent()) return true;
    if (System.Diagnostics.Debugger.IsAttached) return true;
    // NtQueryInformationProcess check (ProcessDebugPort = 7)
    int debugPort = 0;
    NtQueryInformationProcess(Process.GetCurrentProcess().Handle, 7,
        ref debugPort, sizeof(int), out _);
    return debugPort != 0;
}
#endif
```

- Optionally: write a **C++/CLI mixed-mode DLL** (`AntiDebug.dll`) for native-level checks that IL patching tools cannot bypass.

#### 10. SQLite Encryption
- **Risk:** `EZPos.db` in `%ProgramData%` is readable by any admin-level user or forensic tool.
- **Plan:** Evaluate **SQLCipher** (open source) or the official **SQLite Encryption Extension (SEE)**.
  - SQLCipher: replace `System.Data.SQLite` with `SQLitePCLRaw` + `SQLCipher` NuGet.
  - Key management: derive the key from DPAPI-protected secret stored at first run.
- **Note:** This is the highest-effort item and only matters if the DB file itself is a threat in your deployment model.

---

## Implementation Notes

- When building the `Users` table, **never store passwords in plain text**. Use `PBKDF2` via `Rfc2898DeriveBytes` (built into .NET) or `BCrypt.Net-Next` NuGet.
- DPAPI (`System.Security.Cryptography.ProtectedData`) is the correct tool for encrypting local files like `license.dat` and `trial.dat` — it is free, built into Windows, and requires no key management.
- All audit log writes should be **fire-and-forget** (catch and swallow exceptions) so a log failure never breaks a sale.

---

## Suggested Implementation Order

```
Phase 1 (before public release):
  1. Cashier PIN login
  2. Manager-gated settings
  3. HMAC trial.dat
  4. DPAPI license.dat

Phase 2 (post-launch, v1.2+):
  5. Audit log
  6. Role-based access (Admin / Cashier)
  7. Code signing cert

Phase 3 (future / SaaS):
  8. ConfuserEx obfuscation
  9. Anti-debugging
  10. SQLite encryption
```
