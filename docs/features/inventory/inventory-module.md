# Inventory Module — EZPos

> Covers Products, Stock, and Categories.

---

## Key Files

| File | Role |
|---|---|
| `src/UI/Pages/ProductsPage.xaml/.cs` | Product catalogue — add/edit/delete/filter |
| `src/UI/Pages/StockPage.xaml/.cs` | Inventory KPIs, stock adjust, movement history |
| `src/UI/Dialogs/ProductDialog.xaml/.cs` | Add/Edit product form (3 modes) |
| `src/UI/Dialogs/StockAdjustDialog.xaml/.cs` | Stock In / Out / Manual correction |
| `src/UI/Dialogs/CategoryManagementDialog.xaml/.cs` | Add/Rename/Delete categories |
| `src/Business/Services/ProductService.cs` | Product CRUD + PosStateStore sync |
| `src/Business/Services/StockService.cs` | Stock adjustments + movement audit |
| `src/Business/Services/CategoryService.cs` | Category management |

---

## Product Management

### Add Product Flow
1. Click "Add Product" → `ProductDialog` opens in **Add mode**
2. Fill in: Name, Barcode (or scan), Price, **Cost Price (optional)**, Starting Stock, Reorder Level, Category, Unit Type
3. If Cost Price is filled in, the **live profit panel** appears instantly: Profit/Unit, Margin %, Markup %, and a colour-coded status label (e.g. "Healthy profit margin", "⚠️ Selling below cost!")
4. Save → `ProductService.Add(product)`:
   - `ProductRepository.Add()` → INSERT into `Products` (includes `CostPrice` if provided)
   - `_store.AddProduct()` → adds to live `PosStateStore` immediately
   - If opening stock > 0 → `StockMovementRepository.Insert()` with `Reason = OPENING_BALANCE`
5. Product immediately available in Sales page barcode lookup

### Edit Product Flow
1. Select product → click Edit → `ProductDialog` opens in **Edit mode** (pre-filled including Cost Price if set)
2. Modify fields → Save → `ProductService.Update()` → DB + PosStateStore updated
3. Live profit panel activates as soon as both Price and Cost Price are valid numbers

### Delete Product Flow
1. Select product → click Delete → confirmation prompt
2. `ProductService.Delete()` → DB + PosStateStore

### Scan-Mode ProductDialog
When a barcode scan yields "not found" on SalesPage:
- `ProductDialog(productService, categoryService, scannedBarcode)` constructor used
- `BarcodeBox` pre-filled with scanned barcode, set to read-only
- Focus jumps to `NameBox` — cashier only needs to type the name

When opened from ProductsPage Add button in Add mode:
- `SalesKeyboardInputService` is wired to the dialog's `PreviewTextInput`/`PreviewKeyDown`
- Scanning while dialog is open auto-fills `BarcodeBox` and jumps focus to `NameBox`

---

## Cost Price & Profit Calculator

### Overview
`CostPrice` is an **optional** `decimal?` field on `Product`. If left blank it is stored as `NULL` and all profit calculations are skipped — existing workflows are unaffected.

### Live Profit Panel (ProductDialog)
Appears automatically when both Price and Cost Price fields contain valid positive numbers.

| Field | Formula |
|---|---|
| Profit/Unit | `Price − CostPrice` |
| Margin % | `(Price − CostPrice) / Price × 100` |
| Markup % | `(Price − CostPrice) / CostPrice × 100` |

Status labels and colours:

| Condition | Label | Colour |
|---|---|---|
| Profit < 0 | ⚠️ Selling below cost! | OrangeRed |
| Margin < 15% | Very tight profit margin | OrangeRed |
| 15–39% | Healthy profit margin | LightGreen |
| 40–69% | Strong profit margin | Cyan |
| ≥ 70% | Premium pricing detected | Orchid |

### Products Page Column Toggle
A **Show Cost / Hide Cost** button on ProductsPage shows/hides the `Cost Price` column in the grid. Hidden by default (sensitive data).

### DB Column
Added via migration:
```sql
ALTER TABLE Products ADD COLUMN CostPrice REAL
```
Migration is safe — uses `TryAddColumn` which silently ignores `duplicate column` SQLiteException.

---

## ProductDialog Constructors

| Constructor | Mode | Use case |
|---|---|---|
| `(productService, categoryService)` | Add | New product from Products page |
| `(productService, categoryService, string barcode)` | Scan | Unknown barcode from Sales page |
| `(productService, categoryService, Product existing)` | Edit | Edit existing product |

---

## Unit Types

| Type | Description |
|---|---|
| `Unit` | Standard item sold individually |
| `Pack` | Bundle that maps to a parent Unit product; has a conversion rate (e.g. 1 pack = 12 units) |

When a Pack product is sold, stock is deducted from the parent Unit product proportionally.

---

## Stock Management

### Stock Adjustment Flow
1. From StockPage → select product → click Adjust
2. `StockAdjustDialog` opens
3. Select direction: Stock In / Stock Out / Manual Set
4. Enter quantity and reason
5. `StockService.AdjustStock()`:
   - UPDATE product stock in `Products` table
   - INSERT `StockMovements` row (Reason: `ADJUSTMENT` or `CORRECTION`)
   - Update `PosStateStore`

### StockMovement Audit Trail
Every stock change writes a `StockMovements` row:

| Reason | Trigger |
|---|---|
| `OPENING_BALANCE` | Product registered with stock > 0 |
| `SALE` | Checkout in Sales page |
| `ADJUSTMENT` | Manual stock-in/out from StockPage |
| `CORRECTION` | Manual stock override |

Stock history is visible per-product on the StockPage history panel.

---

## Low Stock Alerts

`StockService.GetLowStockItems()` returns products where `Stock <= ReorderLevel`.

Shown on:
- DashboardPage — low stock alert cards
- StockPage — KPI section

---

## Category Management

- Categories are free-form strings stored directly on the Product record
- `CategoryManagementDialog` allows Add, Rename (updates all products with that category), Delete
- Deleting a category does not delete products — they remain with the old category name until reassigned

---

## Business Rules

- Barcode field is optional (can be blank for manually-added products)
- Reorder level defaults to 5 if left blank
- Stock cannot go below 0 via the UI (validation in `StockAdjustDialog`)
- Products with Unit type can be parent of Pack products
- A Pack product must specify: parent product + conversion rate
- Product deletion is permanent — no soft-delete
