# Licensing System — EZPos

> End-to-end documentation for the licensing system: architecture, active implementation, UI flow, and migration path.

---

## Architecture Overview

The licensing system uses an interface contract so the implementation can be swapped without touching any other code.

```
App.xaml.cs (startup)
  └── ILicenseService
        ├── TrialLicenseService   ← ACTIVE (date-based 30-day trial)
        └── LicenseService        ← Future (HWID / API activation)
              └── ILicenseStorage
                    └── FileLicenseStorage  ← stores key in license.dat
```

---

## Interface Contract

```csharp
// src/Core/Licensing/ILicenseService.cs
public interface ILicenseService
{
    LicenseInfo Current { get; }
    bool IsLicensed { get; }
    void LoadAndValidate();
    void Activate(string key);
}

// LicenseStatus enum
public enum LicenseStatus
{
    Valid, Invalid, Expired, Missing, NotActivated
}
```

---

## Active Implementation: TrialLicenseService

**File:** `src/Core/Licensing/TrialLicenseService.cs`
**Trial duration:** 30 days from install date

### trial.dat Path
```
C:\ProgramData\EZPos\trial.dat
```

### trial.dat Format
```
INSTALL_DATE=2026-05-07 14:30:00
```
Written by Inno Setup on first install only. **Never overwritten on reinstall.**

### LoadAndValidate() Logic
1. Read `trial.dat` from `%ProgramData%\EZPos\`
2. If file missing → auto-create with `UtcNow` as install date (dev machine fallback)
3. Parse `INSTALL_DATE=` line
4. Compute `installDate + 30 days`
5. If `UtcNow < expiryDate` → `LicenseStatus.Valid`
6. If `UtcNow >= expiryDate` → `LicenseStatus.Expired`
7. All I/O failures → fail safe (treat as Expired)

### Date Parsing
Two formats are handled:
- ISO 8601 with Z suffix: `2026-05-07T14:30:00Z` (`DateTimeStyles.RoundtripKind`)
- Local time string: `2026-05-07 14:30:00` (`DateTimeStyles.AssumeLocal`) — used by Inno Setup

### Activate()
No-op stub. Trial is date-based, not key-based.

---

## Startup Flow

```
App.xaml.cs — OnStartup()
  ↓
ILicenseService licenseService = new TrialLicenseService();
licenseService.LoadAndValidate();
  ↓
switch (licenseService.Current.Status)
  ├── Valid     → continue to app normally
  ├── Expired   → show TrialExpiredWindow → Shutdown(1)
  └── (other)  → show TrialExpiredWindow → Shutdown(1)
```

---

## TrialExpiredWindow

**File:** `src/UI/Licensing/TrialExpiredWindow.xaml`

Shown when `LicenseStatus.Expired` or any non-Valid status.

**Properties:**
- Full-screen modal — cannot be bypassed
- Error-red border and title bar
- `TriangleExclamation` icon
- Shows expiry date from `licenseInfo.ExpiryDate`
- Contact card:
  - **Catalysm Inc** (cyan `#00D9FF`)
  - **Zarif El-Mansour**
  - WhatsApp icon (`#FF25D366`) + **019-5778954** + "WhatsApp only"
- Single "Close Application" button — calls `Application.Current.Shutdown()`

---

## Inno Setup — trial.dat Creation

```pascal
procedure InitializeTrialIfNeeded();
var
  TrialFile : String;
  DataDir   : String;
  Lines     : TArrayOfString;
begin
  DataDir   := ExpandConstant('{commonappdata}\EZPos');
  TrialFile := DataDir + '\trial.dat';
  if FileExists(TrialFile) then Exit;  // NEVER overwrite
  if not DirExists(DataDir) then CreateDir(DataDir);
  SetArrayLength(Lines, 1);
  Lines[0] := 'INSTALL_DATE=' + GetDateTimeString('yyyy/mm/dd hh:nn:ss', '-', ':');
  SaveStringsToFile(TrialFile, Lines, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then InitializeTrialIfNeeded();
end;
```

**Key behavior:** `FileExists()` guard means reinstalling never resets the trial clock.

---

## LicenseRequiredWindow (Future)

**File:** `src/UI/Licensing/LicenseRequiredWindow.xaml`

Key-entry activation window. Not active in current build. Will be shown when `LicenseStatus.NotActivated` for HWID/API licensing flow.

---

## Migration to Real Licensing

Change **one line** in `App.xaml.cs`:

```csharp
// Current (trial):
ILicenseService licenseService = new TrialLicenseService();

// Future (HWID/API):
ILicenseService licenseService = new LicenseService(new FileLicenseStorage(), new LicenseApiClient());
```

Everything else — the `switch` routing, `TrialExpiredWindow`, `LicenseRequiredWindow` — remains unchanged.

`LicenseApiClient.cs` in `src/Infrastructure/Licensing/` is the placeholder for the Stripe/API integration.
