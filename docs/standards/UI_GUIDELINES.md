# UI Guidelines & Design System

> All UI components in EZPos must follow these standards for visual consistency.
>
> **Stack:** WPF + MahApps.Metro 2.4.10 + MaterialDesignThemes 5.1.0 + FontAwesome.Sharp 6.3.0  
> **Theme:** Cyber Violet Dark — last updated 14 May 2026

---

## Color Palette

All brush tokens are defined in `src/UI/Themes/DashboardTheme.xaml` and loaded globally via `App.xaml`. **Never hardcode hex values in XAML pages — always use a `StaticResource` token.**

### Background Tokens
| Token | Hex | Usage |
|---|---|---|
| `DashboardBackgroundBrush` | `#0D0D1A` | Main page background |
| `DashboardSurfaceBrush` | `#13132A` | Cards, panels, dialog backgrounds |
| `DashboardSurface2Brush` | `#1A1A35` | Row hover, nested surfaces |
| `ContentBrush` | `#13132A` | Alias — popup window backgrounds |

### Accent Tokens
| Token | Hex | Usage |
|---|---|---|
| `DashboardPrimaryBrush` | `#7C3AED` | Primary buttons, active states |
| `DashboardAccentBrush` | `#A78BFA` | Icons, highlights, KPI values |
| `DashboardHoverBrush` | `#9F67FF` | Button hover state |
| `DashboardBorderBrush` | `#2D2B55` | Card borders, dividers |
| `PrimaryBrush` | `#7C3AED` | Short alias for DashboardPrimaryBrush |
| `AccentBrush` | `#A78BFA` | Short alias for DashboardAccentBrush |

### Text Tokens
| Token | Hex | Usage |
|---|---|---|
| `DashboardTextPrimaryBrush` | `#F1F5F9` | Headings, primary values |
| `DashboardTextSecondaryBrush` | `#94A3B8` | Labels, subtitles, field labels |
| `DashboardTextMutedBrush` | `#6B7280` | Hints, placeholders |
| `TextPrimaryBrush` | `#F1F5F9` | Short alias |
| `TextSecondaryBrush` | `#94A3B8` | Short alias |
| `TextMutedBrush` | `#6B7280` | Short alias |

### Semantic Tokens
| Token | Hex | Usage |
|---|---|---|
| `SuccessBrush` | `#10B981` | Positive states, confirmations |
| `WarningBrush` | `#F59E0B` | Alerts, low stock warnings |
| `ErrorBrush` | `#EF4444` | Errors, validation, destructive actions |
| `InfoBrush` | `#7C3AED` | Informational highlights |

### KPI Card Accent Colors (Dashboard)
| Card Type | Color | Hex |
|---|---|---|
| Revenue / primary | Violet | `#A78BFA` / `#7C3AED` |
| Transactions / count | Sky Blue | `#38BDF8` / `#0EA5E9` |
| Warnings / low stock | Amber | `#FCD34D` / `#F59E0B` |
| Averages / performance | Emerald | `#34D399` / `#10B981` |

---

## Typography

| Role | Font | Notes |
|---|---|---|
| App Font | Segoe UI (default WPF) | All UI text |
| Mono Font | Consolas / Courier New | Barcode fields, code display |

---

## Icon Library

**FontAwesome.Sharp v6.3.0** — used for all icons throughout the application.

Usage in XAML:
```xml
<fa:IconBlock Icon="BoxOpen" FontSize="18" Foreground="{StaticResource PrimaryBrush}" />
```

Common icons used:
| Context | Icon Name |
|---|---|
| Products | `BoxOpen` |
| Sales | `ShoppingCart` |
| Stock | `Warehouse` |
| Reports | `ChartBar` |
| Settings | `Gear` |
| Save | `Save` |
| Print | `Print` |
| Search | `Search` |
| Add | `Plus` |
| Edit | `Pen` |
| Delete | `Trash` |
| Warning/Expired | `TriangleExclamation` |
| Trial End | `HourglassEnd` |
| Update | `Download` |
| WhatsApp | `Whatsapp` |

---

## Component Standards

### Buttons
| Style Key | Usage |
|---|---|
| `DashboardRaisedButton` | Primary action — violet filled, rounded |
| `PrimaryButtonStyle` | Alias for `DashboardRaisedButton` |
| `SecondaryButtonStyle` | Secondary / outlined |
| `NavButtonStyle` | Sidebar navigation — flat, left-aligned |
| `WinCloseButtonStyle` | Close button on popup windows — red on hover |
| `MaterialDesignOutlinedButton` | Neutral outlined (cancel, back) |

- Standard height: `36`–`40`
- Always set `materialDesign:ButtonAssist.CornerRadius` via the style — do not override inline

### Cards / Panels
- Use `materialDesign:Card` for standard content cards (shadow, rounded)
- Use `DashboardCardStyle` (Border) only for KPI cards that need hover animations
- Consistent margin: `Margin="0,0,0,16"` between cards

### Input Fields
- Standard height: `36`
- `VerticalContentAlignment="Center"`
- Use `materialDesign:HintAssist.Hint` for placeholder text on MD-styled inputs

### Form Dialogs
| Style Key | TargetType | Usage |
|---|---|---|
| `FieldLabel` | TextBlock | Caption above each input field |
| `ErrorLabel` | TextBlock | Validation error below a field (hidden by default) |
| `FieldGroup` | StackPanel | Vertical spacing wrapper per field group |
| `MonoFont` | FontFamily | Barcode / code input fields |

### Popup Windows
- `WindowStartupLocation="CenterOwner"` — always centered on parent
- `ResizeMode="NoResize"` for fixed dialogs
- `SizeToContent="Height"` for form dialogs
- `Background="{StaticResource ContentBrush}"`
- `FontFamily="{StaticResource AppFont}"`

---

## Spacing

| Context | Value |
|---|---|
| Section header to content | `Margin="0,0,0,16"` |
| Field group spacing | `Margin="0,0,0,14"` |
| Icon to text | `Margin="0,0,8,0"` or `Margin="0,0,10,0"` |
| Card internal padding | `Margin="28,24,28,20"` (dialogs) |

---

## Required Namespaces

Every page and dialog XAML **must** include:
```xml
xmlns:fa="clr-namespace:FontAwesome.Sharp;assembly=FontAwesome.Sharp"
xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
```

> Do **not** add `ResourceDictionary.MergedDictionaries` inside individual page files. All theme resources are loaded globally via `App.xaml`.

---

## Page Layout Pattern

All page `UserControl` elements follow this structure:

```xml
<!-- Root with entrance animation -->
<Grid x:Name="PageRoot" Opacity="0">
  <Grid.RenderTransform><TranslateTransform x:Name="PageTranslate" Y="0"/></Grid.RenderTransform>

  <ScrollViewer VerticalScrollBarVisibility="Auto">
    <StackPanel Margin="24,20">

      <!-- Page header card -->
      <materialDesign:Card Margin="0,0,0,16" Padding="20,16">
        <Grid>
          <!-- Left violet accent bar + title on left, action button on right -->
        </Grid>
      </materialDesign:Card>

      <!-- Content card -->
      <materialDesign:Card Margin="0,0,0,16" Padding="20,16">
        ...
      </materialDesign:Card>

    </StackPanel>
  </ScrollViewer>
</Grid>

<!-- Entrance animation (fade in + slide up) -->
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

---

## DataGrid Pattern

```xml
<DataGrid Style="{StaticResource DashboardDataGrid}"
          AutoGenerateColumns="False" CanUserAddRows="False"
          IsReadOnly="True">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*"/>
  </DataGrid.Columns>
</DataGrid>
```
