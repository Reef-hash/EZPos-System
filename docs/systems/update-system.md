# Auto-Update System — EZPos

> End-to-end documentation for the auto-update system: manifest format, startup check, download flow, and CI/CD pipeline.

---

## Overview

EZPos checks for updates silently on every startup. If a newer version is available, a dialog prompts the user. Downloading and installing happens in-app with no manual steps.

---

## Manifest

**URL:** `https://reef-hash.github.io/EZPos-Update-System/latest.json`
**Config key:** `App:UpdateManifestUrl` in `%ProgramData%\EZPos\config.ini`
**Hosted on:** GitHub Pages (`Reef-hash/EZPos-Update-System` repo)

### latest.json Format
```json
{
  "version": "1.1.6",
  "name": "EZPos v1.1.6",
  "publishedDate": "2026-05-07T11:15:04Z",
  "releaseNotes": "Description of changes",
  "downloadUrl": "https://github.com/Reef-hash/EZPos-System/releases/download/v1.1.6/EZPos-Setup-v1.1.6.exe",
  "checksum": {
    "algorithm": "sha256",
    "value": "f70bb14cffca911443358eafc8a7314ff025eb2bfc6c4d23bfce19b8a6a7efdc"
  },
  "mandatory": false,
  "minimumVersion": "1.0.0",
  "targetFramework": "net6.0-windows7.0",
  "updatedComponents": {
    "binaries": true,
    "schema": false
  }
}
```

---

## Startup Check Flow

```
MainWindow_Loaded
  ↓
CheckForUpdatesOnStartupAsync()  [background, async, non-blocking]
  ↓
ConfigHelper.Get("App:UpdateManifestUrl")
  ├── Empty → return (silent, no dialog)
  └── Has URL →
        ↓
        UpdaterService.CheckForUpdatesAsync()
          ├── Fetch latest.json (10s timeout — fails silently if offline)
          ├── Parse version from manifest
          ├── Compare: IsVersionNewer(manifest.Version, localVersion)
          │     ├── Same or older → return null (no dialog shown)
          │     └── Newer →
          │           Check minimumVersion: if local < minimum → mark mandatory
          │           Return manifest
          ↓
        UpdateAvailableDialog shown
          ├── User clicks Skip → nothing, app continues
          └── User clicks Update Now →
                ↓
                DownloadInstallerAsync()
                  ├── Download .exe to %TEMP%
                  ├── Verify SHA256 checksum
                  ├── Create pre-update DB backup (EZPos_PreUpdate_*.db)
                  ├── Run installer silently (/SILENT /NORESTART)
                  └── Application.Current.Shutdown()
```

---

## UpdaterService Key Details

**File:** `src/Business/Services/UpdaterService.cs`

- Version comparison: `IsVersionNewer()` uses `Version.Parse()` for semantic comparison
- HTTP timeout: 10 seconds
- SHA256 verification: computed on downloaded file, compared to manifest `checksum.value`
- Installer args: `/SILENT /NORESTART /CLOSEAPPLICATIONS`

---

## Version Reading

Version is read from the assembly at runtime — never hardcoded:

```csharp
var assembly = Assembly.GetEntryAssembly();
var ver = assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion
    .Split('+')[0];  // strips build metadata suffix
```

The `+` suffix (e.g. `1.1.6+abc123`) comes from .NET build metadata and is stripped.

---

## UpdateAvailableDialog

**File:** `src/UI/Dialogs/UpdateAvailableDialog.xaml`

Shows:
- New version name + current version
- Release notes from manifest
- "Update Now" button → triggers download
- "Skip" button → closes dialog, app continues normally

---

## Settings — Check for Updates Button

Users can manually trigger an update check from **Settings → About → Check for Updates**.

This calls the same `UpdaterService.CheckForUpdatesAsync()` path and shows a result message if already up to date.

---

## CI/CD Pipeline — Release Process

**Trigger:** Push to `main` with a new `<Version>` in `EZPos.csproj`

**Workflow:** `.github/workflows/auto-tag-from-csproj.yml`

```
1. Read <Version> from EZPos.csproj
2. Create git tag: v{Version}
3. dotnet publish -c Release -o publish
4. ISCC.exe InnoSetup-EZPos.iss → EZPos-Setup-v{Version}.exe
5. Compute SHA256 of installer
6. Generate latest.json with version, URL, checksum, release notes
7. Create GitHub Release → attach installer + latest.json
8. Push latest.json to Reef-hash/EZPos-Update-System repo
   (via UPDATE_MANIFEST_REPO_TOKEN secret)
   → GitHub Pages serves it at the manifest URL
```

**Result:** Within minutes of pushing, all installed EZPos clients will see the update on next launch.

---

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| Update dialog never appears | `App:UpdateManifestUrl` missing from `config.ini` |
| Update dialog never appears | Manifest version == installed version |
| Update dialog never appears | No internet / GitHub Pages not yet propagated (wait 2-5 min) |
| Download fails | Wrong `downloadUrl` in manifest or GitHub release not published yet |
| Checksum mismatch | Installer was rebuilt after manifest was generated; regenerate SHA256 |
