# Dependency Rules — EZPos

> Who can talk to whom. Enforce these at every code review.

---

## Allowed Dependencies

```
UI (Pages/Dialogs)
  → Business/Services         ✅
  → UI/State/PosStateStore    ✅ (read)
  → UI/Navigation             ✅
  → UI/Input                  ✅
  → Models/Domain             ✅ (read/display only)
  → DataAccess/Repositories   ❌ FORBIDDEN
  → Core/Licensing            ❌ (only App.xaml.cs is allowed)

Business/Services
  → DataAccess/Repositories   ✅
  → Models/Domain             ✅
  → UI/State/PosStateStore    ✅ (write after DB ops)
  → UI (any WPF type)         ❌ FORBIDDEN
  → System.Windows.*          ❌ FORBIDDEN

DataAccess/Repositories
  → Models/Domain             ✅
  → Database.cs               ✅
  → Business/Services         ❌ FORBIDDEN
  → UI                        ❌ FORBIDDEN

Models/Domain
  → Nothing                   (pure data classes only)

Core/Licensing
  → Models/Domain             ✅
  → DataAccess (ConfigHelper) ✅
  → UI                        ❌ FORBIDDEN
  → Business/Services         ❌ FORBIDDEN

App.xaml.cs (entry point only)
  → Core/Licensing            ✅ (one-time startup check)
  → Business/Services         ✅ (startup initialization)
  → UI/Licensing/*Window      ✅ (show expiry window if needed)
```

---

## Common Violations (never commit these)

| Violation | Correct approach |
|---|---|
| `SalesPage.xaml.cs` calls `ProductRepository.GetAll()` | Call `productService.GetAll()` instead |
| `ProductService.cs` uses `MessageBox.Show(...)` | Throw an exception; let the UI handle display |
| `SaleRepository.cs` checks if stock is sufficient | Move that check to `SaleService.cs` |
| `DashboardPage` writes directly to `PosStateStore` | Call the appropriate Service; it updates the store |
| Hardcoded version string in Settings XAML | Read from assembly via `AssemblyInformationalVersionAttribute` |

---

## Infrastructure/Licensing

`LicenseApiClient.cs` in `src/Infrastructure/Licensing/` is a placeholder for future Stripe/API licensing. When implemented:
- It is called only by `LicenseService.cs` (in Core/Licensing)
- It must never be called from UI directly
- Network calls must be async and fail gracefully
