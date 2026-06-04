# Layered Architecture — EZPos

---

## Overview

EZPos uses a strict 4-layer architecture. Each layer has one responsibility and defined communication rules.

```
┌─────────────────────────────────────────────────┐
│  PRESENTATION LAYER                             │
│  src/UI/ — Pages, Dialogs, Windows              │
│  XAML + minimal code-behind                     │
├─────────────────────────────────────────────────┤
│  BUSINESS LOGIC LAYER                           │
│  src/Business/Services/                         │
│  Rules, validation, orchestration               │
├─────────────────────────────────────────────────┤
│  DATA ACCESS LAYER                              │
│  src/DataAccess/Repositories/                   │
│  Raw SQL, row → object mapping                  │
├─────────────────────────────────────────────────┤
│  DATABASE LAYER                                 │
│  SQLite — C:\ProgramData\EZPos\EZPos.db         │
└─────────────────────────────────────────────────┘

Supporting layers (cross-cutting):
┌─────────────────────┬──────────────────────────┐
│  POSSTATE STORE      │  CORE/LICENSING           │
│  UI/State/           │  Core/Licensing/          │
│  In-memory app state │  ILicenseService contract │
└─────────────────────┴──────────────────────────┘
```

---

## Layer Responsibilities

### Presentation Layer — `src/UI/`

**What it does:**
- Renders data from `PosStateStore` to the screen
- Captures user input and delegates to Services
- Handles navigation via `NavigationService`
- Owns all WPF/XAML types

**Rules:**
- ✅ Call Services to perform actions
- ✅ Read from `PosStateStore` for display
- ✅ Register keyboard/scanner event handlers on window
- ❌ Never call Repositories directly
- ❌ Never contain business rules or calculations
- ❌ Never write SQL

---

### Business Logic Layer — `src/Business/Services/`

| Service | Responsibility |
|---|---|
| `ProductService` | Add, Edit, Delete, GetAll, GetByBarcode — syncs DB + PosStateStore |
| `CategoryService` | Add, Rename, Delete categories |
| `SaleService` | ProcessSale → writes Sale + SaleItems + StockMovements in one transaction |
| `StockService` | AdjustStock → writes StockMovement + updates product stock |
| `ReportService` | Query aggregations for dashboard and reports pages |
| `UpdaterService` | Fetch manifest, compare versions, download installer, verify SHA256 |

**Rules:**
- ✅ Contains all business rules and validations
- ✅ Orchestrates Repositories and state updates
- ✅ Returns domain objects to callers
- ❌ Never import `System.Windows` or WPF types
- ❌ Never talk to DB directly (use Repositories)

---

### Data Access Layer — `src/DataAccess/Repositories/`

| Repository | Operations |
|---|---|
| `ProductRepository` | `GetAll()`, `GetByBarcode()`, `Add()`, `Update()`, `Delete()` |
| `CategoryRepository` | `GetAll()`, `Add()`, `Rename()`, `Delete()` |
| `SaleRepository` | `AddSale()` — writes Sale + SaleItems + StockMovements in one DB transaction |
| `StockMovementRepository` | `Insert()`, `InsertWithConnection()`, `GetByProduct()` |
| `Database` | Schema init, migration, connection factory |
| `ConfigHelper` | Flat key-value read/write from `config.ini` |

**Rules:**
- ✅ All SQL lives here
- ✅ Maps DB rows to domain objects
- ❌ Never contains business logic (no if/else rules)
- ❌ Never references UI or Service layer

---

### PosStateStore — `src/UI/State/PosStateStore.cs`

The single in-memory state for the running app. All UI binds to or reads from this store.

**Populated on startup** by `ProductService.LoadAll()`, `CategoryService.GetAll()`, etc.

**Updated after every write operation** by the Service that performed the operation:
- `AddProduct()`, `UpdateProduct()`, `RemoveProduct()`
- `AddSale()`, `UpdateStockAfterSale()`
- `ReloadTaxConfig()`

**Rules:**
- ✅ UI reads from PosStateStore for display
- ✅ Services update PosStateStore after DB writes
- ❌ Never modified directly from page code-behind

---

### Core/Licensing — `src/Core/Licensing/`

| File | Role |
|---|---|
| `ILicenseService.cs` | Contract: `LoadAndValidate()`, `Activate()`, `IsLicensed`, `Current` |
| `ILicenseStorage.cs` | Contract: `LoadKey()`, `SaveKey()`, `ClearKey()` |
| `LicenseInfo.cs` | Data: Key, Status, ExpiryDate, ActivatedAt, PlanName |
| `LicenseStatus.cs` | Enum: Valid, Invalid, Expired, Missing, NotActivated |
| `TrialLicenseService.cs` | **Active** — 30-day date-based trial |
| `LicenseService.cs` | Mock — always returns Valid; ready for API wiring |
| `FileLicenseStorage.cs` | Stores license key at `%ProgramData%\EZPos\license.dat` |

**See:** [Licensing System](../systems/licensing-system.md) for full flow.

---

## MVVM Pattern (New Modules)

The Barcode Management module is the **first MVVM module** in EZPos. All new modules going forward use this pattern. Existing pages are not converted — no regression risk.

### Layer Mapping

```
┌─────────────────────────────────────────────────┐
│  PRESENTATION LAYER — BarcodesPage.xaml         │
│  Binds to ViewModel properties and commands     │
│  Code-behind: DataContext only, zero logic      │
├─────────────────────────────────────────────────┤
│  VIEWMODEL LAYER — src/UI/ViewModels/           │
│  INotifyPropertyChanged + ICommand              │
│  Owns all UI state, filter logic, commands      │
│  Calls Services — never Repositories directly  │
├─────────────────────────────────────────────────┤
│  BUSINESS LOGIC LAYER — unchanged               │
│  BarcodeService, LabelPrintService              │
├─────────────────────────────────────────────────┤
│  DATA ACCESS LAYER — unchanged                  │
│  LabelTemplateRepository, BarcodeLabelRepository│
└─────────────────────────────────────────────────┘
```

### RelayCommand

Shared utility at `src/UI/ViewModels/RelayCommand.cs`. Do not add a third-party MVVM framework.

```csharp
public sealed class RelayCommand : ICommand
{
    private readonly Action      _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute    = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter)    => _execute();

    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

For commands that take a parameter, add a generic `RelayCommand<T>` alongside it.

### ViewModel Rules

- ✅ Implements `INotifyPropertyChanged` — use `[CallerMemberName]` pattern
- ✅ Exposes `ICommand` properties (never event handlers from code-behind)
- ✅ Calls Services to perform actions
- ✅ Reads from `PosStateStore` for display state
- ❌ Never imports `System.Windows.Controls` or WPF-specific types
- ❌ Never calls Repositories directly — always through a Service
- ❌ No `MessageBox.Show` in ViewModels — surface errors via observable properties bound to UI

### Code-Behind Rule (MVVM pages)

The `.xaml.cs` file must only:
1. Call `InitializeComponent()`
2. Assign `DataContext = viewModel`
3. Wire events that cannot be expressed in XAML (e.g. `PreviewKeyDown` for scanner passthrough)

No business logic, no service calls, no state in code-behind.

### ViewModels Location

```
src/UI/ViewModels/
    RelayCommand.cs
    BarcodesPageViewModel.cs
    QuickPrintDialogViewModel.cs
    LabelTemplateEditorViewModel.cs
```

---

## Startup Sequence

```
Program.cs
  └── App.xaml.cs — OnStartup()
        ├── 1. TrialLicenseService.LoadAndValidate()
        │      └── Expired? → TrialExpiredWindow → Shutdown(1)
        ├── 2. Database.Initialize() — schema creation / migration
        ├── 3. PosStateStore created
        ├── 4. Services created (ProductService, CategoryService, etc.)
        ├── 5. Services load data → populate PosStateStore
        └── 6. MainWindow created and shown
              └── MainWindow_Loaded → CheckForUpdatesOnStartupAsync()
```
