# Reporting Module — EZPos

---

## Key Files

| File | Role |
|---|---|
| `src/UI/Pages/ReportsPage.xaml/.cs` | Reports UI — date range, charts, tables, export |
| `src/UI/Pages/DashboardPage.xaml/.cs` | Live KPI cards and alerts |
| `src/Business/Services/ReportService.cs` | All reporting queries and aggregations |

---

## ReportService Methods

| Method | Returns |
|---|---|
| `GetSummary(from, to)` | Revenue, orders, avg order, items sold, **estimated profit**, **profit margin %** |
| `GetDailyBreakdown(from, to)` | Day-by-day revenue breakdown |
| `GetTopProducts(from, to, limit)` | Best-selling products by quantity and revenue |
| `GetTodaySummary()` | Today's revenue, sales count, and top product |
| `GetLowStockAlerts()` | Products at or below reorder level |
| `GetPaymentBreakdown(from, to)` | Revenue split by payment method |
| `GetTransactions(from, to)` | Full transaction list with line items |
| `GetStockSnapshot()` | Current stock levels for all products, **including CostPrice** |

---

## ReportsPage

### Date Range Selection
- From/To date pickers
- Presets: Today, This Week, This Month, Custom

### Report Sections
1. **Summary KPIs** — total revenue, sales count, average sale
2. **Estimated Profit KPI band** — Est. Gross Profit + Profit Margin % cards. Hidden automatically when no products have a cost price set. Shows a note: "Estimated based on products with cost price set."
3. **Daily Breakdown** — revenue per day in selected range
4. **Top Products** — best sellers table
5. **Payment Breakdown** — cash vs QR vs card vs cheque
6. **Transaction List** — detailed sale records

### Export

| Format | Library | Output |
|---|---|---|
| Excel (.xlsx) | ClosedXML 0.102.2 | 6 sheets: Summary, Daily, Top Products, Transactions, Payment, Stock Snapshot |
| PDF | PdfSharpCore 1.3.67 | Formatted report with store name and date range |

**Excel enhancements (CostPrice update):**
- **Sheet 1 (Summary):** Est. Gross Profit and Profit Margin rows added in KPI table (green text), shown only when cost data exists. Includes footnote.
- **Sheet 6 (Stock Snapshot):** 3 new columns — Cost Price (RM), Profit/Unit (RM), Margin %. N/A shown for products without a cost price.

Export uses `ConfigHelper.Get("StoreName")` for the store name header.

---

## DashboardPage

Live data loaded on page navigation, not on app startup.

**KPI Cards:**
- Today's Revenue
- Today's Sales Count
- Total Products
- Low Stock Count

**Low Stock Alerts panel:** shows product name + current stock for all products at/below reorder level.

---

## Business Rules

- All report queries are date-range inclusive (from start of `from` date to end of `to` date)
- Reports read directly from `SaleRepository` / `ProductRepository` — they do not use `PosStateStore`
- Export filenames include date range: `EZPos_Report_2026-05-01_to_2026-05-07.xlsx`
