# EZPos — Project Structure & Organization Rules

> **Scope of this document:** Structure rules, layer responsibilities, naming conventions, and maintenance standards only.
> For roadmap → see `docs/planning/`. For progress → see `docs/tracking/`. For architecture detail → see `docs/architecture/`.

---

## Directory Layout

```
EZPos-System/
├── src/
│   ├── Models/Domain/           — Domain entity classes only. No logic.
│   ├── DataAccess/Repositories/ — Raw SQL + row mapping. No business logic.
│   ├── Business/Services/       — Business rules and orchestration. No WPF types.
│   ├── Core/Licensing/          — License contracts and implementations.
│   ├── Infrastructure/Licensing/— External API clients (future).
│   ├── UI/
│   │   ├── State/               — Single shared in-memory state (PosStateStore).
│   │   ├── Navigation/          — Route → UserControl mapping.
│   │   ├── Input/               — Hardware input services (barcode scanner).
│   │   ├── Licensing/           — License-related windows.
│   │   ├── Dialogs/             — Modal dialog windows.
│   │   └── Pages/               — Page-level UserControls.
│   ├── Security/
│   │   ├── Authentication/      — Stub (future multi-user login).
│   │   └── Authorization/       — Stub (future role-based access).
│   └── Utilities/
│       ├── Extensions/          — Extension method classes.
│       └── Helpers/             — Utility classes (ESC/POS, raw print).
│
├── Config/
│   └── config.ini               — Default config seeded to %ProgramData% on first install.
│
├── Resources/
│   ├── Icons/                   — app.ico and icon assets.
│   └── Images/                  — Image assets.
│
├── docs/                        — All project documentation (see docs/README.md).
│
├── MainWindow.xaml/.cs          — App shell and nav wiring.
├── App.xaml/.cs                 — Startup sequence: license → DB → services → MainWindow.
├── Program.cs                   — Entry point.
├── EZPos.csproj                 — Project file. Version number lives here.
├── InnoSetup-EZPos.iss          — Installer script.
└── .github/workflows/           — CI/CD pipelines.
```

---

## Layer Rules

### What each layer is allowed to do

| Layer | Can call | Cannot call |
|---|---|---|
| `UI/Pages`, `UI/Dialogs` | Services, PosStateStore, NavigationService | Repositories directly, DB |
| `Business/Services` | Repositories, Domain models | UI types, WPF namespaces |
| `DataAccess/Repositories` | Database.cs, Domain models | Services, UI |
| `Models/Domain` | Nothing | Everything |
| `UI/State/PosStateStore` | Domain models | Services, Repositories |

### Violations to never commit
- A Page/Dialog executing raw SQL
- A Service importing any `System.Windows` namespace
- A Repository containing if/else business rules
- `PosStateStore` being modified directly from a page without going through a Service

---

## Naming Conventions

### Files
| Type | Convention | Example |
|---|---|---|
| Domain model | `PascalCase.cs` | `Product.cs`, `SaleItem.cs` |
| Repository | `{Entity}Repository.cs` | `ProductRepository.cs` |
| Service | `{Domain}Service.cs` | `ProductService.cs` |
| Page | `{Name}Page.xaml` | `SalesPage.xaml` |
| Dialog | `{Name}Dialog.xaml` | `ProductDialog.xaml` |
| Window | `{Name}Window.xaml` | `TrialExpiredWindow.xaml` |

### Config keys
- Feature config: `FeatureName` (e.g. `StoreName`, `TaxRate`)
- App-level config: `App:KeyName` (e.g. `App:UpdateManifestUrl`)
- Hotkeys: `{Action}Hotkey{Variant}` (e.g. `PaymentHotkeyCash`)

---

## Adding a New Feature — Checklist

1. Domain model → `src/Models/Domain/`
2. Repository (if needs DB) → `src/DataAccess/Repositories/`
3. Service → `src/Business/Services/`
4. Page or Dialog → `src/UI/Pages/` or `src/UI/Dialogs/`
5. Update `PosStateStore` if the feature needs shared state
6. Document behavior in `docs/features/{module}/`
7. Update `docs/tracking/FEATURE_STATUS.md`

---

## NuGet Dependencies

| Package | Version | Purpose |
|---|---|---|
| `System.Data.SQLite` | 1.0.117 | SQLite database |
| `FontAwesome.Sharp` | 6.3.0 | WPF icon library |
| `ClosedXML` | 0.102.2 | Excel (.xlsx) export |
| `PdfSharpCore` | 1.3.67 | PDF export |
| `Microsoft.VisualBasic` | 10.3.0 | FileSystem helpers |

---

## Build Commands

```bash
# Local build
dotnet build --configuration Release

# Publish for installer
dotnet publish -c Release -o publish

# Build installer (requires Inno Setup installed)
ISCC.exe /DAppVersion=1.x.x InnoSetup-EZPos.iss
```

## Release Process

1. Bump `<Version>` in `EZPos.csproj`
2. Commit and push to `main`
3. CI automatically: creates git tag → builds installer → publishes GitHub Release → syncs `latest.json` to update manifest repo
