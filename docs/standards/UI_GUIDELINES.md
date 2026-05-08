# UI Guidelines & Design System

> All UI components in EZPos must follow these standards for visual consistency.

---

## Color Palette

| Role | Hex | Usage |
|---|---|---|
| Primary | `#00D9FF` | Buttons, active states, highlights, icons |
| Primary Dark | `#00B8D4` | Button hover, pressed states |
| Sidebar | `#0F172A` | Navigation sidebar background |
| Content | `#1E293B` | Main content area background |
| Card | `#334155` | Card and panel backgrounds |
| Text Primary | `#F1F5F9` | Main text |
| Text Secondary | `#94A3B8` | Labels, descriptions |
| Text Muted | `#64748B` | Hints, disabled text |
| Success | `#10B981` | Positive states, confirmations |
| Warning | `#F59E0B` | Alerts, low stock |
| Error | `#EF4444` | Errors, destructive actions, trial expired |
| WhatsApp Green | `#FF25D366` | WhatsApp contact in TrialExpiredWindow |
| Cyan Contact | `#00D9FF` | Company name in TrialExpiredWindow |

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
- Primary action: `PrimaryButtonStyle` — cyan background, dark text
- Secondary action: `SecondaryButtonStyle` — outlined or muted
- Destructive action: `ErrorBrush` background (e.g. "Close Application" in TrialExpiredWindow)
- Standard height: `36`–`40`

### Cards / Panels
- Use `PosCardStyle` for all content sections
- Consistent margin: `Margin="0,0,0,16"` between cards

### Input Fields
- Standard height: `36`
- `VerticalContentAlignment="Center"`

### Dialogs / Windows
- `WindowStartupLocation="CenterOwner"` — always centered on parent
- `ResizeMode="NoResize"` for fixed dialogs
- `SizeToContent="Height"` for form dialogs

---

## Spacing

| Context | Value |
|---|---|
| Section header to content | `Margin="0,0,0,16"` |
| Field group spacing | `Margin="0,0,0,14"` |
| Icon to text | `Margin="0,0,8,0"` or `Margin="0,0,10,0"` |
| Card internal padding | `Margin="28,24,28,20"` (dialogs) |

---

## Page Layout Pattern

All page `UserControl` elements follow this structure:

```xml
<ScrollViewer>
  <StackPanel Margin="0">
    <!-- Page header card -->
    <Border Style="{StaticResource PosCardStyle}" Margin="0,0,0,16">
      <Grid>
        <!-- Title + primary action button -->
      </Grid>
    </Border>

    <!-- Content cards -->
    <Border Style="{StaticResource PosCardStyle}" Margin="0,0,0,16">
      ...
    </Border>
  </StackPanel>
</ScrollViewer>
```
