# UI Modernization Plan — MahApps.Metro + MaterialDesign + Cyber Violet

> **Status:** POC COMPLETE — Dashboard approved, full migration in progress  
> **Target:** Migrate all pages and dialogs from custom WPF styles to MahApps.Metro + MaterialDesignInXamlToolkit  
> **Theme:** Cyber Violet Dark (`#0D0D1A` bg, `#7C3AED` primary, `#A78BFA` accent)  
> **Approach:** Dashboard serves as POC and style reference — all other pages must follow the same pattern

---

## Current Status (13 May 2026)

| Component | Status | Notes |
|---|---|---|
| NuGet packages (MahApps + MD) | ✅ Done | MahApps 2.4.10, MaterialDesignThemes 5.1.0 |
| `App.xaml` ResourceDictionaries | ✅ Done | BundledTheme Dark+Cyan + MahApps Controls/Fonts/Themes |
| `DashboardTheme.xaml` | ✅ Done | Cyber Violet palette, DashboardCardStyle, DashboardDataGrid |
| `DashboardPage.xaml` | ✅ Done | Page entrance animation, KPI cards, hover lift, count-up numbers |
| `DashboardPage.xaml.cs` | ✅ Done | AnimateCurrency + AnimateInt (DispatcherTimer count-up) |
| `MainWindow.xaml` | ✅ Done | MetroWindow migration — Phase 3 complete |
| `MainWindow.xaml.cs` | ✅ Done | Base class MetroWindow, brush keys, removed title bar handlers |
| Remaining pages (5 pages) | ⬜ Not started | Phase 4 — follow Dashboard style reference |
| Dialogs (6 dialogs) | ⬜ Not started | Phase 5 (visual) + Phase 6 (DialogHost overlay) |
| Snackbar system | ⬜ Not started | Phase 7 |

---

---

## Background

EZPos originally used a fully custom WPF theme defined in `App.xaml` — dark navy sidebar (`#0F172A`), dark content area (`#1E293B`), cyan accent (`#00D9FF`), and all component styles written by hand.

After completing the Dashboard POC with MahApps.Metro + MaterialDesign, the **Cyber Violet** theme has been approved as the official style for the entire application. All pages being migrated must follow the patterns demonstrated in DashboardPage.

---

## Cyber Violet — Official Color Tokens

Defined in `DashboardTheme.xaml`. These tokens **must be used** in all migrated pages:

```xml
<!-- Backgrounds -->
DashboardBackgroundBrush   = #0D0D1A   (main page background)
DashboardSurfaceBrush      = #13132A   (cards, panels, tables)
DashboardSurface2Brush     = #1A1A35   (row hover, nested surface)

<!-- Text -->
DashboardTextPrimaryBrush  = #F1F5F9   (headings, primary values)
DashboardTextSecondaryBrush= #94A3B8   (labels, subtitles)
DashboardTextMutedBrush    = #6B7280   (hints, placeholders)

<!-- Accent -->
DashboardPrimaryBrush      = #7C3AED   (primary button, highlights)
DashboardAccentBrush       = #A78BFA   (icons, violet KPI values)
DashboardHoverBrush        = #9F67FF   (button hover state)
DashboardBorderBrush       = #2D2B55   (card borders, table borders)
```

**Per-card KPI accent colors** (icon, top accent strip, value text):
| Card | Color | Hex |
|---|---|---|
| Revenue / primary metric | Violet | `#A78BFA` / `#7C3AED` |
| Transactions / count | Sky Blue | `#38BDF8` / `#0EA5E9` |
| Warnings / low stock | Amber | `#FCD34D` / `#F59E0B` |
| Averages / performance | Emerald | `#34D399` / `#10B981` |

---

## Dashboard as Mandatory Style Reference

`DashboardPage.xaml` is the **style reference** for all other page migrations. Every migrated page must follow these patterns:

### 1. Page Entrance Animation
```xml
<!-- Root Grid must have Opacity="0" + TranslateTransform -->
<Grid x:Name="PageRoot" Opacity="0">
    <Grid.RenderTransform>
        <TranslateTransform x:Name="PageTranslate" Y="0"/>
    </Grid.RenderTransform>
</Grid>

<!-- UserControl.Triggers — fade in + slide up from Y=16 -->
<UserControl.Triggers>
    <EventTrigger RoutedEvent="Loaded">
        <BeginStoryboard><Storyboard>
            <DoubleAnimation Storyboard.TargetName="PageRoot"
                             Storyboard.TargetProperty="Opacity"
                             From="0" To="1" Duration="0:0:0.3"/>
            <DoubleAnimation Storyboard.TargetName="PageTranslate"
                             Storyboard.TargetProperty="Y"
                             From="16" To="0" Duration="0:0:0.35">
                <DoubleAnimation.EasingFunction><CubicEase EasingMode="EaseOut"/></DoubleAnimation.EasingFunction>
            </DoubleAnimation>
        </Storyboard></BeginStoryboard>
    </EventTrigger>
</UserControl.Triggers>
```

### 2. Standard Page Header
```xml
<Border Style="{StaticResource DashboardCardStyle}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>
        <!-- Left violet accent bar + title -->
        <StackPanel Orientation="Horizontal">
            <Border Width="4" Height="26" CornerRadius="2" Margin="0,0,14,0">
                <Border.Background>
                    <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                        <GradientStop Color="#7C3AED" Offset="0"/>
                        <GradientStop Color="#A78BFA" Offset="1"/>
                    </LinearGradientBrush>
                </Border.Background>
            </Border>
            <TextBlock Text="[Page Title]"
                       Foreground="{StaticResource DashboardTextPrimaryBrush}"
                       FontSize="20" FontWeight="Bold"/>
        </StackPanel>
        <!-- Action button on the right -->
        <Button Grid.Column="1" Style="{StaticResource DashboardRaisedButton}" .../>
    </Grid>
</Border>
```

### 3. Content Card
```xml
<Border CornerRadius="12" Background="{StaticResource DashboardSurfaceBrush}"
        BorderThickness="1">
    <Border.BorderBrush><SolidColorBrush Color="#2D2B55"/></Border.BorderBrush>
    <!-- Content inside with Padding="20,16" -->
</Border>
```

### 4. Standard DataGrid
```xml
<DataGrid Style="{StaticResource DashboardDataGrid}"
          AutoGenerateColumns="False" CanUserAddRows="False"
          IsReadOnly="True" ...>
```

### 5. Required Namespaces in Every Page
```xml
xmlns:fa="clr-namespace:FontAwesome.Sharp;assembly=FontAwesome.Sharp"
xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
```

> **IMPORTANT:** Do not add `ResourceDictionary.MergedDictionaries` inside page files. All MD and DashboardTheme resources are loaded globally via `App.xaml`.

---

## Theme File Strategy per Page

Each page may have its own theme file like `DashboardTheme.xaml`, **or** use the tokens from `DashboardTheme.xaml` directly (simpler approach). Recommendation:

| Page | Theme File | Strategy |
|---|---|---|
| DashboardPage | `DashboardTheme.xaml` | ✅ Exists — use directly |
| ProductsPage | Use `DashboardTheme.xaml` | No separate file needed |
| SalesPage | Use `DashboardTheme.xaml` | No separate file needed |
| StockPage | Use `DashboardTheme.xaml` | No separate file needed |
| ReportsPage | Use `DashboardTheme.xaml` | No separate file needed |
| SettingsPage | Use `DashboardTheme.xaml` | No separate file needed |

To use `DashboardTheme.xaml` tokens in other pages, ensure it is merged in `App.xaml`:
```xml
<ResourceDictionary Source="src/UI/Themes/DashboardTheme.xaml"/>
```

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

> **How to use this section:**  
> Each phase has a status badge, prerequisites, and per-file task checkboxes.  
> Update checkbox `[ ]` → `[x]` as tasks are completed.  
> Never start a phase until its prerequisites are met.  
> The **Completion Gate** at the end of each phase must pass before moving to the next.

---

### Phase Overview

| Phase | Description | Status | Prerequisite |
|---|---|---|---|
| **P1** | Foundation — packages + App.xaml | ✅ COMPLETE | — |
| **P2** | Dashboard POC — Cyber Violet style reference | ✅ COMPLETE | P1 |
| **P3** | MainWindow → MetroWindow chrome | ⬜ NOT STARTED | P1 |
| **P4** | Pages migration (5 remaining pages) | ⬜ NOT STARTED | P2, P3 |
| **P5** | Dialogs — Cyber Violet styling | ⬜ NOT STARTED | P2 |
| **P6** | Dialogs → DialogHost overlay system | ⬜ NOT STARTED | P5 |
| **P7** | Snackbar notification system | ⬜ NOT STARTED | P3, P6 |

---

### Phase 1 — Foundation *(Packages + App.xaml)*

> **Status:** ✅ COMPLETE  
> **Completed:** 13 May 2026  
> **Effort:** ~2 hours  
> **Risk:** Low

**Tasks:**
- [x] Install `MahApps.Metro 2.4.10` via NuGet
- [x] Install `MaterialDesignThemes 5.1.0` via NuGet
- [x] Merge `BundledTheme` (Dark + Cyan) in `App.xaml`
- [x] Merge MahApps Controls, Fonts, Themes in `App.xaml`
- [x] Verify existing EZPos brush keys still resolve (`PrimaryBrush`, `WarningBrush`, etc.)
- [x] Build passes with 0 errors

**Completion Gate:** `dotnet build` — 0 errors, app launches, all pages load.

---

### Phase 2 — Dashboard POC *(Cyber Violet Style Reference)*

> **Status:** ✅ COMPLETE  
> **Completed:** 13 May 2026  
> **Effort:** ~4 hours  
> **Risk:** Low  
> **Prerequisite:** Phase 1

**Goal:** Establish the Cyber Violet visual language using Dashboard as the proof-of-concept. This phase defines the style tokens, animation patterns, and component patterns that all future phases must follow.

**Tasks:**
- [x] Create `src/UI/Themes/DashboardTheme.xaml` with Cyber Violet color tokens
- [x] Define `DashboardCardStyle` (CornerRadius=12, surface bg, border)
- [x] Define `DashboardRaisedButton` (violet, hover animation)
- [x] Define `DashboardDataGrid` (violet headers, row hover)
- [x] Rewrite `DashboardPage.xaml` — page entrance animation (fade + slide-up)
- [x] KPI card 1: Revenue (Violet `#A78BFA`)
- [x] KPI card 2: Transactions (Sky `#38BDF8`)
- [x] KPI card 3: Low Stock (Amber `#FCD34D`)
- [x] KPI card 4: Avg Order (Emerald `#34D399`)
- [x] Per-card hover lift animation (Y: 0 → -6) + border glow on `MouseEnter/Leave`
- [x] Low stock DataGrid with `DashboardDataGrid` style + Status badge column
- [x] Empty state panel (shown when no low stock alerts)
- [x] Rewrite `DashboardPage.xaml.cs` — `AnimateCurrency()` count-up (35 steps, 16ms)
- [x] Rewrite `DashboardPage.xaml.cs` — `AnimateInt()` count-up (25 steps, 20ms)
- [x] Build passes with 0 errors, Dashboard renders correctly

**Completion Gate:** App runs, Dashboard shows Cyber Violet dark theme, KPI cards animate on load, numbers count up, hover lift works.

---

### Phase 3 — MainWindow Chrome *(MetroWindow)*

> **Status:** ✅ COMPLETE  
> **Estimated effort:** 3–5 hours  
> **Risk:** Medium — touches main application shell  
> **Prerequisite:** Phase 1

**Goal:** Replace the hand-rolled `WindowStyle=None` + `WindowChrome` title bar with `mah:MetroWindow`. This gives a polished native window chrome with glow border and built-in minimize/maximize/close.

**Tasks:**

`MainWindow.xaml`:
- [x] Change root element from `Window` to `mah:MetroWindow`
- [x] Add `xmlns:mah` namespace declaration
- [x] Add `GlowBrush="{StaticResource DashboardPrimaryBrush}"` and `BorderThickness="1"`
- [x] Remove `WindowChrome.WindowChrome` attached property block
- [x] Remove custom title bar `Grid` (Row 0 — logo, title text, minimize/maximize/close buttons)
- [x] Move app logo + "EZPos" text into `mah:MetroWindow.TitleTemplate`
- [x] Apply Cyber Violet background to window: `Background="{StaticResource DashboardBackgroundBrush}"`
- [x] Verify sidebar (Column 0) and content area (Column 1) layout is unchanged

`MainWindow.xaml.cs`:
- [x] Change class declaration: `public partial class MainWindow : MetroWindow`
- [x] Add `using MahApps.Metro.Controls;`
- [x] Remove `TitleBar_MouseLeftButtonDown` handler
- [x] Remove `MinimizeBtn_Click` handler
- [x] Remove `MaximizeBtn_Click` handler
- [x] Remove `CloseBtn_Click` handler
- [x] Verify constructor and page navigation logic is unaffected

**Completion Gate:** App launches, window has native chrome with violet glow border, all nav buttons load pages correctly, minimize/maximize/close work.

---

### Phase 4 — Pages Migration *(5 Remaining Pages)*

> **Status:** ⬜ NOT STARTED  
> **Estimated effort:** 8–14 hours (across all pages)  
> **Risk:** Medium — many files, but purely visual  
> **Prerequisite:** Phase 2 (Dashboard reference), Phase 3 (MainWindow chrome)

**Goal:** Apply Cyber Violet styling to all remaining pages using `DashboardPage.xaml` as the exact reference. Each page gets: entrance animation, Cyber Violet header, card-wrapped sections, and Cyber Violet DataGrid.

**Style reference for all tasks:** `DashboardPage.xaml` + `DashboardTheme.xaml`

---

#### 4A — ProductsPage

`src/UI/Pages/ProductsPage.xaml`:
- [ ] Add `xmlns:fa` and `xmlns:materialDesign` namespaces
- [ ] Set root `Grid x:Name="PageRoot" Opacity="0"` + `TranslateTransform x:Name="PageTranslate"`
- [ ] Add `UserControl.Triggers` — page entrance fade + slide-up (copy from DashboardPage)
- [ ] Replace header section with standard Cyber Violet header (violet accent bar + title + action button)
- [ ] Wrap product list section in `DashboardCardStyle` Border
- [ ] Apply `DashboardDataGrid` style to product DataGrid
- [ ] Replace all `PrimaryButtonStyle` → `DashboardRaisedButton`
- [ ] Replace all `SecondaryButtonStyle` → `MaterialDesignOutlinedButton` with Cyber Violet border
- [ ] Replace search `TextBox` → `MaterialDesignOutlinedTextBox`
- [ ] Replace filter `ComboBox` → `MaterialDesignOutlinedComboBox`
- [ ] Set page background: `Background="{StaticResource DashboardBackgroundBrush}"`

`src/UI/Pages/ProductsPage.xaml.cs`:
- [ ] No code-behind changes required (styling only)

---

#### 4B — SalesPage

`src/UI/Pages/SalesPage.xaml`:
- [ ] Add `xmlns:fa` and `xmlns:materialDesign` namespaces
- [ ] Set root `Grid x:Name="PageRoot" Opacity="0"` + `TranslateTransform`
- [ ] Add `UserControl.Triggers` — page entrance animation
- [ ] Replace header with standard Cyber Violet header
- [ ] Cart panel — wrap in `DashboardCardStyle` Border, apply Sky Blue accent (`#38BDF8`) for totals
- [ ] Product search panel — wrap in `DashboardCardStyle` Border
- [ ] Replace search `TextBox` → `MaterialDesignOutlinedTextBox`
- [ ] Replace all action buttons → `DashboardRaisedButton` or `MaterialDesignOutlinedButton`
- [ ] Cart `ItemsControl`/`DataGrid` — apply `DashboardDataGrid` style (or match DashboardSurfaceBrush manually)
- [ ] Payment total area — Emerald accent (`#34D399`) for final total value
- [ ] Set page background: `Background="{StaticResource DashboardBackgroundBrush}"`

`src/UI/Pages/SalesPage.xaml.cs`:
- [ ] No code-behind changes required (styling only)

`src/UI/Pages/SalesModeControl.xaml`:
- [ ] Apply Cyber Violet surface colors to mode toggle buttons
- [ ] Match border/background tokens with DashboardTheme

---

#### 4C — StockPage

`src/UI/Pages/StockPage.xaml`:
- [ ] Add `xmlns:fa` and `xmlns:materialDesign` namespaces
- [ ] Set root `Grid x:Name="PageRoot" Opacity="0"` + `TranslateTransform`
- [ ] Add `UserControl.Triggers` — page entrance animation
- [ ] Replace header with standard Cyber Violet header
- [ ] Wrap stock movement list in `DashboardCardStyle` Border with Amber top accent (`#F59E0B`)
- [ ] Apply `DashboardDataGrid` style to stock movement DataGrid
- [ ] Replace all buttons → `DashboardRaisedButton` / `MaterialDesignOutlinedButton`
- [ ] Set page background: `Background="{StaticResource DashboardBackgroundBrush}"`

`src/UI/Pages/StockPage.xaml.cs`:
- [ ] No code-behind changes required (styling only)

---

#### 4D — ReportsPage

`src/UI/Pages/ReportsPage.xaml`:
- [ ] Add `xmlns:fa` and `xmlns:materialDesign` namespaces
- [ ] Set root `Grid x:Name="PageRoot" Opacity="0"` + `TranslateTransform`
- [ ] Add `UserControl.Triggers` — page entrance animation
- [ ] Replace header with standard Cyber Violet header
- [ ] Summary KPI row — apply same 4-card pattern as Dashboard (Violet/Sky/Amber/Emerald)
- [ ] Transaction list section — wrap in `DashboardCardStyle`, apply `DashboardDataGrid`
- [ ] Top products section — wrap in `DashboardCardStyle`, apply `DashboardDataGrid`
- [ ] Replace date filter inputs → `MaterialDesignOutlinedTextBox` / `DatePicker`
- [ ] Replace all buttons → `DashboardRaisedButton` / `MaterialDesignOutlinedButton`
- [ ] Set page background: `Background="{StaticResource DashboardBackgroundBrush}"`

`src/UI/Pages/ReportsPage.xaml.cs`:
- [ ] No code-behind changes required (styling only)

---

#### 4E — SettingsPage

`src/UI/Pages/SettingsPage.xaml`:
- [ ] Add `xmlns:fa` and `xmlns:materialDesign` namespaces
- [ ] Set root `Grid x:Name="PageRoot" Opacity="0"` + `TranslateTransform`
- [ ] Add `UserControl.Triggers` — page entrance animation
- [ ] Replace header with standard Cyber Violet header
- [ ] Group each settings section in `DashboardCardStyle` Border
- [ ] Replace all `TextBox` → `MaterialDesignOutlinedTextBox`
- [ ] Replace all `ComboBox` → `MaterialDesignOutlinedComboBox`
- [ ] Replace save/action buttons → `DashboardRaisedButton`
- [ ] Set page background: `Background="{StaticResource DashboardBackgroundBrush}"`

`src/UI/Pages/SettingsPage.xaml.cs`:
- [ ] No code-behind changes required (styling only)

---

**Phase 4 Completion Gate:** All 5 pages render with Cyber Violet dark background, entrance animations play on navigation, all buttons and inputs match the Dashboard style, no white/unstyled areas visible.

---

### Phase 5 — Dialogs Styling *(Cyber Violet Visual)*

> **Status:** ⬜ NOT STARTED  
> **Estimated effort:** 4–6 hours  
> **Risk:** Low-Medium — visual only, no invocation pattern change  
> **Prerequisite:** Phase 2

**Goal:** Apply Cyber Violet colors and MD control styles inside all dialogs. Dialogs remain as `Window` for now — only the visual styling changes.

**Tasks per dialog:**

`RenameDialog.xaml` + `.cs`:
- [ ] Set `Background="{StaticResource DashboardSurfaceBrush}"`, `BorderBrush="{StaticResource DashboardBorderBrush}"`
- [ ] Replace `TextBox` → `MaterialDesignOutlinedTextBox`
- [ ] Replace OK button → `DashboardRaisedButton`
- [ ] Replace Cancel button → `MaterialDesignOutlinedButton`

`WeightInputDialog.xaml` + `.cs`:
- [ ] Apply `DashboardSurfaceBrush` background
- [ ] Replace numeric `TextBox` → `MaterialDesignOutlinedTextBox`
- [ ] Replace action buttons → `DashboardRaisedButton` / `MaterialDesignOutlinedButton`

`StockAdjustDialog.xaml` + `.cs`:
- [ ] Apply `DashboardSurfaceBrush` background
- [ ] Replace all `TextBox`/`ComboBox` → MD outlined variants
- [ ] Replace action buttons → `DashboardRaisedButton` / `MaterialDesignOutlinedButton`
- [ ] Apply Amber accent (`#FCD34D`) to stock quantity display

`CategoryManagementDialog.xaml` + `.cs`:
- [ ] Apply `DashboardSurfaceBrush` background
- [ ] Replace list display → `DashboardDataGrid` or custom surface list
- [ ] Replace all inputs → MD outlined variants
- [ ] Replace action buttons → `DashboardRaisedButton` / `MaterialDesignOutlinedButton`

`ProductDialog.xaml` + `.cs`:
- [ ] Apply `DashboardSurfaceBrush` background
- [ ] Replace all `TextBox`/`ComboBox` → MD outlined variants
- [ ] Replace save/cancel buttons → `DashboardRaisedButton` / `MaterialDesignOutlinedButton`
- [ ] Barcode field: keep existing scanner wiring, only change visual style
- [ ] Image preview area: wrap in `DashboardCardStyle` Border

`PaymentDialog.xaml` + `.cs`:
- [ ] Apply `DashboardSurfaceBrush` background, `DashboardBackgroundBrush` for numpad area
- [ ] Replace payment method tabs → MD `TabControl` with Cyber Violet accent
- [ ] Total display: Emerald accent (`#34D399`) for final total
- [ ] Replace all inputs → MD outlined variants
- [ ] Replace action buttons → `DashboardRaisedButton`
- [ ] Keep all numpad button logic unchanged

> **Do not touch:**  
> `ReceiptDialog` — keep as-is (print functionality depends on exact layout)  
> `UpdateAvailableDialog` — keep as-is (pre-MainWindow lifecycle)

**Phase 5 Completion Gate:** All 6 dialogs open with Cyber Violet dark background, MD-styled inputs and buttons, consistent with page styling. Build passes with 0 errors.

---

### Phase 6 — Dialogs → DialogHost *(Overlay System)*

> **Status:** ⬜ NOT STARTED  
> **Estimated effort:** 6–10 hours  
> **Risk:** Medium-High — changes invocation pattern across multiple call sites  
> **Prerequisite:** Phase 5

**Goal:** Replace `Window.ShowDialog()` pattern with `MaterialDesign.DialogHost.Show()` overlays for a seamless in-app experience.

**Setup tasks (do once):**

`MainWindow.xaml`:
- [ ] Wrap content area `Frame`/`ContentControl` inside `materialDesign:DialogHost Identifier="RootDialog"`
- [ ] Verify `DialogHost` does not interfere with sidebar layout

**Migration order (simplest first to reduce risk):**

`RenameDialog`:
- [ ] Convert XAML from `Window` to `UserControl`
- [ ] Remove `WindowChrome`, title bar, `DialogResult` assignments
- [ ] Return result via `DialogHost.CloseDialogCommand` with result object
- [ ] Update all call sites: `await DialogHost.Show(new RenameDialogView(...), "RootDialog")`

`WeightInputDialog`:
- [ ] Convert XAML from `Window` to `UserControl`
- [ ] Return numeric result via `DialogHost.CloseDialogCommand`
- [ ] Update all call sites

`StockAdjustDialog`:
- [ ] Convert XAML from `Window` to `UserControl`
- [ ] Return adjustment result via `DialogHost.CloseDialogCommand`
- [ ] Update all call sites in `StockPage.xaml.cs`

`CategoryManagementDialog`:
- [ ] Convert XAML from `Window` to `UserControl`
- [ ] Update all call sites in `ProductsPage.xaml.cs`

`ProductDialog`:
- [ ] Convert XAML from `Window` to `UserControl`
- [ ] Verify barcode scanner `KeyDown` capture still works inside `DialogHost`
- [ ] Update all call sites in `ProductsPage.xaml.cs`

`PaymentDialog`:
- [ ] Convert XAML from `Window` to `UserControl`
- [ ] Verify numpad keyboard input still works inside `DialogHost`
- [ ] Update all call sites in `SalesPage.xaml.cs`

> **Do not migrate:**  
> `ReceiptDialog` — stays as `Window` (uses `PrintVisual`)  
> `UpdateAvailableDialog` — stays as `Window` (shown before `MainWindow` initializes)

**Phase 6 Completion Gate:** All 6 dialogs open as overlays on the main content area, result data passes back correctly to call sites, barcode scanner and numpad still function, build passes with 0 errors.

---

### Phase 7 — Snackbar Notification System

> **Status:** ⬜ NOT STARTED  
> **Estimated effort:** 2–3 hours  
> **Risk:** Low  
> **Prerequisite:** Phase 3 (MetroWindow), Phase 6 (DialogHost in place)

**Goal:** Replace non-destructive `MessageBox.Show` calls with a non-blocking Snackbar toast.

**Setup tasks:**

`MainWindow.xaml`:
- [ ] Add `materialDesign:Snackbar x:Name="MainSnackbar"` anchored to bottom of content area
- [ ] Set `MessageQueue="{materialDesign:MessageQueue}"`

`MainWindow.xaml.cs`:
- [ ] Add `public static SnackbarMessageQueue SnackQueue { get; private set; }`
- [ ] Assign `SnackQueue = MainSnackbar.MessageQueue!` in constructor

**Replace `MessageBox` with Snackbar at these call sites:**

`ProductsPage.xaml.cs`:
- [ ] "Product saved successfully" → `MainWindow.SnackQueue.Enqueue(...)`
- [ ] "Product deleted" → `MainWindow.SnackQueue.Enqueue(...)`
- [ ] "Category renamed" → `MainWindow.SnackQueue.Enqueue(...)`

`StockPage.xaml.cs`:
- [ ] "Stock adjusted" → `MainWindow.SnackQueue.Enqueue(...)`

`SettingsPage.xaml.cs`:
- [ ] "Settings saved" → `MainWindow.SnackQueue.Enqueue(...)`

**Keep as `MessageBox` (requires user confirmation):**
- [ ] "Are you sure you want to delete this product?" — destructive, keep `MessageBox`
- [ ] "Clear cart? This cannot be undone." — destructive, keep `MessageBox`

**Phase 7 Completion Gate:** Non-destructive confirmations appear as bottom toast notifications, destructive operations still use `MessageBox`, no regressions in save/delete flows.

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

| File | Phase | Status | Change |
|---|---|---|---|
| `EZPos.csproj` | P1 | ✅ Done | Added MahApps.Metro + MaterialDesignThemes package refs |
| `App.xaml` | P1 | ✅ Done | BundledTheme + MahApps MergedDictionaries; Cyber Violet overrides |
| `src/UI/Themes/DashboardTheme.xaml` | P2 | ✅ Done | Created — Cyber Violet tokens, card/button/datagrid styles |
| `src/UI/Pages/DashboardPage.xaml` | P2 | ✅ Done | Full rewrite — entrance anim, KPI cards, hover lift, low stock table |
| `src/UI/Pages/DashboardPage.xaml.cs` | P2 | ✅ Done | AnimateCurrency + AnimateInt count-up methods |
| `MainWindow.xaml` | P3 | ⬜ Pending | Switch to `mah:MetroWindow`; remove custom title bar; add DialogHost + Snackbar |
| `MainWindow.xaml.cs` | P3, P7 | ⬜ Pending | Remove window chrome handlers; expose static SnackQueue |
| `src/UI/Pages/ProductsPage.xaml` | P4A | ⬜ Pending | Entrance anim, Cyber Violet header, MD inputs/buttons, DashboardDataGrid |
| `src/UI/Pages/SalesPage.xaml` | P4B | ⬜ Pending | Entrance anim, Cyber Violet header, MD inputs/buttons |
| `src/UI/Pages/SalesModeControl.xaml` | P4B | ⬜ Pending | Cyber Violet surface colors on mode toggle |
| `src/UI/Pages/StockPage.xaml` | P4C | ⬜ Pending | Entrance anim, Cyber Violet header, DashboardDataGrid |
| `src/UI/Pages/ReportsPage.xaml` | P4D | ⬜ Pending | Entrance anim, KPI summary cards, DashboardDataGrid |
| `src/UI/Pages/SettingsPage.xaml` | P4E | ⬜ Pending | Entrance anim, grouped DashboardCardStyle sections, MD inputs |
| `src/UI/Dialogs/RenameDialog.xaml` | P5, P6 | ⬜ Pending | Cyber Violet styling → UserControl for DialogHost |
| `src/UI/Dialogs/WeightInputDialog.xaml` | P5, P6 | ⬜ Pending | Cyber Violet styling → UserControl for DialogHost |
| `src/UI/Dialogs/StockAdjustDialog.xaml` | P5, P6 | ⬜ Pending | Cyber Violet styling → UserControl for DialogHost |
| `src/UI/Dialogs/CategoryManagementDialog.xaml` | P5, P6 | ⬜ Pending | Cyber Violet styling → UserControl for DialogHost |
| `src/UI/Dialogs/ProductDialog.xaml` | P5, P6 | ⬜ Pending | Cyber Violet styling → UserControl for DialogHost |
| `src/UI/Dialogs/PaymentDialog.xaml` | P5, P6 | ⬜ Pending | Cyber Violet styling → UserControl for DialogHost |
| `src/UI/Dialogs/*.xaml.cs` (×6) | P6 | ⬜ Pending | Replace `ShowDialog()` with `await DialogHost.Show()` pattern |
| Call sites in pages (×6 files) | P6 | ⬜ Pending | Update all dialog invocations to DialogHost pattern |
| Call sites for MessageBox | P7 | ⬜ Pending | Replace non-destructive `MessageBox.Show` → `SnackQueue.Enqueue` |

---

## Prerequisites Before Starting

- [x] Create a dedicated Git branch: `feature/ui-modernization`
- [x] Install MahApps.Metro 2.4.10 + MaterialDesignThemes 5.1.0
- [x] Merge ResourceDictionaries in `App.xaml` — app builds and launches
- [x] Confirm Cyber Violet theme approved (Dashboard POC sign-off: 13 May 2026)
- [ ] Confirm MahApps 2.4.x runs on target Windows 7 machine (test environment)
- [ ] Take screenshots of all 5 remaining pages and 6 dialogs before migration starts
- [ ] Ensure build is clean (`0 errors`) before starting each new phase

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
