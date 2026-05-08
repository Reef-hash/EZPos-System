# UI Modernization Plan — MahApps.Metro + MaterialDesign

> **Status:** PLANNED — not started  
> **Target:** Replace custom hand-rolled WPF styles with MahApps.Metro + MaterialDesignInXamlToolkit  
> **Goal:** Professional, modern UI with richer components (dialogs, snackbars, styled grids) while preserving the existing dark cyan brand identity

---

## Background

EZPos currently uses a fully custom WPF theme defined in `App.xaml` — dark navy sidebar (`#0F172A`), dark content area (`#1E293B`), cyan accent (`#00D9FF`), and all component styles (buttons, inputs, cards, DataGrids) written by hand.

This works, but it means every new UI component must be hand-styled from scratch. Adopting MahApps + MaterialDesign provides:

- Production-quality component library (dialogs, snackbars, chips, badges, progress, date pickers)
- `DialogHost` — in-place overlay dialogs instead of separate `Window` instances
- `Snackbar` — non-blocking toast notifications (ideal for cart events, low stock warnings)
- Styled `DataGrid` with column sorting, row hover, selection states out of the box
- `MetroWindow` — polished custom chrome with built-in minimize/maximize/close

---

## Package Versions (Target)

| Package | NuGet ID | Min Version | Notes |
|---|---|---|---|
| MahApps.Metro | `MahApps.Metro` | 2.4.10 | Supports .NET 6, WPF, Windows 7+ |
| MaterialDesign Themes | `MaterialDesignThemes` | 5.1.0 | Supports .NET 6 |
| MaterialDesign Colors | `MaterialDesignColors` | 3.1.0 | Included transitively by MaterialDesignThemes |
| FontAwesome.Sharp | `FontAwesome.Sharp` | 6.3.0 | **Keep as-is** — compatible with both libraries |

> ⚠️ Do NOT use MaterialDesign 4.x — it targets .NET Framework / older .NET 5. Use **5.x** for .NET 6 compatibility.

---

## Architecture Impact

```
App.xaml
  └── MahApps BaseTheme       (Dark, sets window/control chrome)
  └── MaterialDesign BaseTheme (Dark)
  └── MaterialDesign ColorTheme (Primary: Cyan, Accent: Cyan)
  └── EZPos Custom Overrides   (keep brand colors, override where needed)

MainWindow.xaml
  └── mah:MetroWindow           (replaces WindowStyle=None + manual WindowChrome)
      └── Sidebar + ContentArea (unchanged layout)

Dialogs (8 total)
  └── Migrate from Window → MaterialDesign DialogHost overlay
      OR keep as Window but apply MD styles inside

SalesPage, ProductsPage, etc.
  └── Replace custom DataGrid styles → MaterialDesign DataGrid style
  └── Replace custom TextBox/ComboBox/Button → MD equivalents
  └── Add Snackbar at page level for toast notifications
```

---

## Migration Phases

### Phase 1 — Package Install + ResourceDictionary Merge *(Foundation)*

**Estimated effort:** 2–4 hours  
**Risk:** Low — additive only, no existing code removed  
**Goal:** Both libraries load without breaking existing UI

**Steps:**
1. Install NuGet packages:
   ```
   MahApps.Metro 2.4.10
   MaterialDesignThemes 5.1.0
   ```
2. Merge ResourceDictionaries in `App.xaml`:
   ```xml
   <ResourceDictionary.MergedDictionaries>
       <!-- MaterialDesign -->
       <materialDesign:BundledTheme BaseTheme="Dark"
           PrimaryColor="Cyan" SecondaryColor="Cyan" />
       <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml" />

       <!-- MahApps -->
       <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml" />
       <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml" />
       <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Themes/Dark.Cyan.xaml" />

       <!-- EZPos overrides (brand colors, custom keys) -->
       <!-- Keep existing color/brush keys — override MD defaults where needed -->
   </ResourceDictionary.MergedDictionaries>
   ```
3. Verify app still launches and all pages load without XAML errors.
4. Fix any `StaticResource` key conflicts between MD/MahApps and existing `App.xaml` keys.

**Known conflicts to resolve:**
- MD defines its own `PrimaryBrush`, `SecondaryBrush` — may conflict with EZPos `PrimaryBrush`
- MahApps redefines `BorderBrush` globally — check sidebar/card borders still render correctly

---

### Phase 2 — MainWindow Migration to MetroWindow *(Window Chrome)*

**Estimated effort:** 3–5 hours  
**Risk:** Medium — touches MainWindow layout and code-behind  
**Goal:** Replace hand-rolled `WindowStyle=None` chrome with `mah:MetroWindow`

**Steps:**
1. Change `MainWindow.xaml` root element from `Window` to `mah:MetroWindow`:
   ```xml
   <mah:MetroWindow x:Class="EZPos.UI.MainWindow"
       xmlns:mah="clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro"
       ...
       GlowBrush="{DynamicResource AccentColorBrush}"
       BorderThickness="1">
   ```
2. Remove `WindowChrome.WindowChrome` block (MahApps handles this)
3. Remove custom title bar `Grid` (Row 0) — replace with MahApps `TitleTemplate` or `LeftWindowCommands`/`RightWindowCommands`
4. Remove `MinimizeBtn_Click`, `MaximizeBtn_Click`, `CloseBtn_Click` handlers (MahApps provides these)
5. Preserve existing sidebar layout (Column 0) and content area (Column 1) — these are unaffected

**Code-behind changes (`MainWindow.xaml.cs`):**
- Remove `TitleBar_MouseLeftButtonDown`, `MinimizeBtn_Click`, `MaximizeBtn_Click`, `CloseBtn_Click`
- Change base class from `Window` to `MetroWindow`

**Branding placement:**
- App logo + "EZPos" text → move to MahApps `TitleTemplate`:
  ```xml
  <mah:MetroWindow.TitleTemplate>
      <DataTemplate>
          <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
              <Image Source="/Resources/Icons/app.ico" Width="20" Height="20" Margin="0,0,8,0" />
              <TextBlock Text="EZPos" FontWeight="Bold" FontSize="13" />
          </StackPanel>
      </DataTemplate>
  </mah:MetroWindow.TitleTemplate>
  ```

---

### Phase 3 — Button, Input, and Form Controls *(Component Styling)*

**Estimated effort:** 4–8 hours  
**Risk:** Medium — many files touched, but purely visual  
**Goal:** Replace custom `PrimaryButtonStyle`, `SecondaryButtonStyle`, TextBox, ComboBox styles with MaterialDesign equivalents

**Button mapping:**

| Current Style | MaterialDesign Equivalent |
|---|---|
| `PrimaryButtonStyle` | `Style="{StaticResource MaterialDesignRaisedButton}"` |
| `SecondaryButtonStyle` | `Style="{StaticResource MaterialDesignOutlinedButton}"` |
| `DangerButtonStyle` | `Style="{StaticResource MaterialDesignRaisedButton}"` + `Background=ErrorBrush` |
| `GhostButtonStyle` | `Style="{StaticResource MaterialDesignFlatButton}"` |
| `WinCtrlButtonStyle` | Keep custom or use MahApps `WindowCommandsStyle` |
| `NavButtonStyle` (sidebar) | Keep custom (MD doesn't have a sidebar nav concept) |

**Input mapping:**

| Current | MaterialDesign Equivalent |
|---|---|
| Plain `TextBox` | `Style="{StaticResource MaterialDesignOutlinedTextBox}"` |
| Plain `ComboBox` | `Style="{StaticResource MaterialDesignOutlinedComboBox}"` |
| `PasswordBox` | `Style="{StaticResource MaterialDesignOutlinedPasswordBox}"` |
| `CheckBox` | `Style="{StaticResource MaterialDesignCheckBox}"` |

**Files to update:**
- `src/UI/Pages/` — all 6 pages
- `src/UI/Dialogs/` — all 8 dialogs
- Remove overridden styles from `App.xaml` once MD defaults apply globally

---

### Phase 4 — Dialogs Migration to DialogHost *(Dialog System)*

**Estimated effort:** 6–10 hours  
**Risk:** Medium-High — changes dialog invocation pattern throughout the codebase  
**Goal:** Replace `Window`-based modal dialogs with MaterialDesign `DialogHost` overlays

**Current pattern:**
```csharp
var dlg = new ProductDialog(product);
if (dlg.ShowDialog() == true) { ... }
```

**New pattern:**
```csharp
var view = new ProductDialogView(product);
var result = await DialogHost.Show(view, "RootDialog");
if (result is ProductDialogResult r) { ... }
```

**DialogHost placement in MainWindow:**
```xml
<materialDesign:DialogHost Identifier="RootDialog"
    Grid.Row="1" Grid.Column="1">
    <!-- existing content area Frame/ContentControl -->
</materialDesign:DialogHost>
```

**Dialog priority order (migrate easiest first):**
1. `RenameDialog` — simple single-field form, low risk
2. `WeightInputDialog` — simple numeric input
3. `StockAdjustDialog` — medium complexity
4. `CategoryManagementDialog` — list + add/rename/delete
5. `ProductDialog` — complex, has barcode scanner wiring
6. `PaymentDialog` — most complex, has tab switching + numpad
7. `ReceiptDialog` — keep as Window (needs print functionality)
8. `UpdateAvailableDialog` — keep as Window (runs before MainWindow in some cases)

> **Note:** `ReceiptDialog` and `UpdateAvailableDialog` should remain as `Window` — they have special lifecycle requirements.

---

### Phase 5 — DataGrid Styling *(Tables)*

**Estimated effort:** 2–4 hours  
**Risk:** Low  
**Goal:** Apply MaterialDesign DataGrid style to all product/sales/stock/report tables

**Change:**
```xml
<!-- Add to each DataGrid -->
Style="{StaticResource MaterialDesignDataGrid}"
```

**Also add to each DataGrid:**
```xml
materialDesign:DataGridAssist.ColumnHeaderPadding="4 8"
materialDesign:DataGridAssist.CellPadding="4 8"
```

**Pages with DataGrids:**
- `ProductsPage.xaml` — product list
- `StockPage.xaml` — stock movement list
- `ReportsPage.xaml` — transaction list, top products
- `SalesPage.xaml` — cart items (ItemsControl, not DataGrid — may not apply)

---

### Phase 6 — Snackbar Notification System *(Toast Notifications)*

**Estimated effort:** 3–5 hours  
**Risk:** Low  
**Goal:** Replace `MessageBox.Show` confirmation toasts and add new non-blocking notices

**Snackbar placement in MainWindow:**
```xml
<materialDesign:Snackbar x:Name="MainSnackbar"
    MessageQueue="{materialDesign:MessageQueue}"
    Grid.Row="1" Grid.Column="1"
    VerticalAlignment="Bottom" />
```

**Expose globally via MainWindow:**
```csharp
public static MaterialDesignThemes.Wpf.SnackbarMessageQueue SnackQueue { get; private set; }
// Initialize in constructor
SnackQueue = MainSnackbar.MessageQueue!;
```

**Usage from any page:**
```csharp
MainWindow.SnackQueue.Enqueue("Product saved successfully.");
MainWindow.SnackQueue.Enqueue("⚠ Low stock: Milo 3in1 (2 remaining)");
```

**Replace these `MessageBox` calls with Snackbar:**
- "Product saved" after ProductDialog confirms
- "Stock adjusted" after StockAdjustDialog confirms
- "Category renamed" after RenameDialog confirms

**Keep `MessageBox` for:**
- Destructive confirmations (delete product, clear cart) — these require user response

---

## Color Strategy

EZPos uses a dark cyan theme. MaterialDesign supports this natively.

### MaterialDesign Color Mapping

| EZPos Key | MD Equivalent | Action |
|---|---|---|
| `PrimaryColor` `#00D9FF` | `PrimaryHueMidBrush` (Cyan) | Set `PrimaryColor="Cyan"` in BundledTheme |
| `SidebarColor` `#0F172A` | No MD equivalent | Keep as custom key |
| `ContentColor` `#1E293B` | `MaterialDesignPaper` | Override MD background |
| `CardBackground` `#334155` | `MaterialDesignCardBackground` | Override |
| `SuccessColor` `#10B981` | `SuccessBrush` (MD 5.x) | Use MD or keep custom |
| `ErrorColor` `#EF4444` | `MaterialDesignValidationErrorBrush` | Use MD or keep custom |

### Override Strategy

After merging MD/MahApps ResourceDictionaries, add EZPos brand overrides in `App.xaml` **after** the merged dictionaries:

```xml
<!-- Override MD paper/background to match EZPos dark theme -->
<SolidColorBrush x:Key="MaterialDesignPaper" Color="#FF1E293B" />
<SolidColorBrush x:Key="MaterialDesignBackground" Color="#FF0F172A" />
<SolidColorBrush x:Key="MaterialDesignCardBackground" Color="#FF334155" />
<SolidColorBrush x:Key="MaterialDesignBody" Color="#FFF1F5F9" />
<SolidColorBrush x:Key="MaterialDesignBodyLight" Color="#FF94A3B8" />
```

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| MD/MahApps ResourceDictionary key conflicts with existing EZPos keys | High | Medium | Audit key names before Phase 1; rename EZPos keys if needed |
| Barcode scanner wiring in ProductDialog breaks when migrated to DialogHost | Medium | High | Migrate ProductDialog last; test scanner thoroughly after |
| `DropShadowEffect` inside DataGrid cells causes white box artifacts (known WPF bug) | High | Low | Already documented in App.xaml; do not add shadow inside DataGrid cells |
| PaymentDialog complexity makes DialogHost migration error-prone | Medium | Medium | Keep as Window until Phase 4, or migrate without DialogHost |
| Windows 7 compatibility — MahApps 2.x requires .NET Framework 4.5+ | Low | High | MahApps 2.x supports .NET 6 on Windows 7; verify in test environment |
| Trial/license dialogs appear before MainWindow — can't use DialogHost | Low | Medium | `TrialExpiredWindow` and `LicenseRequiredWindow` stay as `Window` always |

---

## File Change Summary

| File | Phase | Change Type |
|---|---|---|
| `EZPos.csproj` | 1 | Add 2 NuGet package references |
| `App.xaml` | 1 | Merge MD + MahApps ResourceDictionaries; add brand overrides |
| `MainWindow.xaml` | 2 | Switch to `mah:MetroWindow`; remove custom title bar |
| `MainWindow.xaml.cs` | 2 | Remove window control handlers; change base class |
| `src/UI/Pages/*.xaml` (×6) | 3, 5 | Replace button/input/DataGrid styles |
| `src/UI/Dialogs/*.xaml` (×6) | 3, 4 | Replace styles; migrate to DialogHost (except Receipt, Update) |
| `src/UI/Dialogs/*.xaml.cs` (×6) | 4 | Change invocation pattern for DialogHost dialogs |
| `MainWindow.xaml` | 6 | Add Snackbar + DialogHost overlays |
| `MainWindow.xaml.cs` | 6 | Expose static SnackQueue |
| Call sites in pages/code-behind | 6 | Replace MessageBox with SnackQueue for non-destructive notices |

---

## Prerequisites Before Starting

- [ ] Create a dedicated Git branch: `feature/ui-modernization`
- [ ] Confirm MahApps 2.4.x runs on target Windows 7 machine (test environment)
- [ ] Read MaterialDesignThemes 5.x migration guide if upgrading from earlier version
- [ ] Take screenshots of all 6 pages and 8 dialogs for before/after comparison
- [ ] Ensure build is clean (`0 errors, 0 warnings`) before starting

---

## Do Not Change

These files/components should **not** be touched during this migration:

| Item | Reason |
|---|---|
| `SalesKeyboardInputService` | Complex HID barcode logic — no UI component involvement |
| `NavButtonStyle` (sidebar nav buttons) | MD has no sidebar nav concept; keep fully custom |
| `TrialExpiredWindow`, `LicenseRequiredWindow` | Pre-MainWindow lifecycle; keep as plain `Window` |
| `UpdateAvailableDialog` | May be shown before MainWindow; keep as `Window` |
| `ReceiptDialog` | Needs `PrintVisual`; keep as `Window` |
| All `*Repository.cs`, `*Service.cs` | Business logic layer — zero UI dependency |
| `ConfigHelper`, `DatabaseHelper` | Infrastructure — zero UI dependency |

---

## References

- [MahApps.Metro Docs](https://mahapps.com/docs/guides/quick-start)
- [MaterialDesignInXamlToolkit Docs](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki)
- [MaterialDesign + MahApps Integration Guide](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki/MahApps.Metro-integration)
- [MaterialDesignThemes 5.x Changelog](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/releases)
- [MahApps DialogHost Guide](https://mahapps.com/docs/controls/dialogs)
