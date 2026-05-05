using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosedXML.Excel;
using EZPos.Business.Services;
using Microsoft.Win32;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace EZPos.UI.Pages
{
    public partial class ReportsPage : UserControl
    {
        private readonly ReportService _reportService = new();
        private ObservableCollection<ChartData> salesTrendData = new();
        private ObservableCollection<TopProductResult> topProducts = new();
        private ObservableCollection<HourlySales> peakHours = new();
        private bool isInitialized;

        // Local display model for the bar chart (adds BarHeight + BarColor for rendering)
        public class ChartData
        {
            public string  Label     { get; set; } = string.Empty;
            public decimal Value     { get; set; }
            public double  BarHeight { get; set; }
            public Brush   BarColor  { get; set; } = Brushes.Gray;
        }

        public ReportsPage()
        {
            InitializeComponent();
            Loaded += ReportsPage_Loaded;
        }

        private void ReportsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInitialized)
            {
                return;
            }

            // Default: load today's data on first load
            LoadData(DateTime.Today, DateTime.Today);
            isInitialized = true;
        }

        // ── Core data load ────────────────────────────────────────────────────

        private void LoadData(DateTime from, DateTime to)
        {
            // KPI summary cards
            var summary = _reportService.GetSummary(from, to);
            TotalSalesValue.Text    = $"RM {summary.TotalRevenue:N2}";
            TotalOrdersValue.Text   = summary.TotalOrders.ToString();
            AvgOrderValue.Text      = $"RM {summary.AverageOrder:N2}";
            TransactionsValue.Text  = summary.TotalOrders.ToString();

            // Bar chart — daily breakdown
            var daily = _reportService.GetDailyBreakdown(from, to);
            salesTrendData.Clear();
            double maxRevenue = 1; // avoid divide-by-zero
            foreach (var d in daily)
                if ((double)d.Revenue > maxRevenue) maxRevenue = (double)d.Revenue;

            foreach (var d in daily)
            {
                salesTrendData.Add(new ChartData
                {
                    Label     = d.Label,
                    Value     = d.Revenue,
                    BarHeight = Math.Max(4, (double)d.Revenue / maxRevenue * 220),
                    BarColor  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00D9FF"))
                });
            }

            if (salesTrendData.Count == 0)
                salesTrendData.Add(new ChartData { Label = "No data", Value = 0, BarHeight = 4,
                    BarColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569")) });

            SalesTrendChart.ItemsSource = null;
            SalesTrendChart.ItemsSource = salesTrendData;

            // Top products
            var top = _reportService.GetTopProducts(from, to);
            topProducts.Clear();
            foreach (var p in top) topProducts.Add(p);
            TopProductsGrid.ItemsSource = null;
            TopProductsGrid.ItemsSource = topProducts;

            // Hourly breakdown — only meaningful for a single day
            var hourly = _reportService.GetHourlyBreakdown(from);
            peakHours.Clear();
            foreach (var h in hourly) peakHours.Add(h);
            PeakHoursGrid.ItemsSource = null;
            PeakHoursGrid.ItemsSource = peakHours;
        }

        private void InitializeData()
        {
            // Kept for compatibility — no longer used (LoadData replaces this)
        }

        private void BindData()
        {
            // Kept for compatibility — no longer used
        }

        private (DateTime from, DateTime to) GetSelectedDateRange()
        {
            if (StartDatePicker?.SelectedDate != null && EndDatePicker?.SelectedDate != null)
                return (StartDatePicker.SelectedDate.Value.Date, EndDatePicker.SelectedDate.Value.Date);
            return (DateTime.Today, DateTime.Today);
        }

        private void PeriodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || PeriodCombo is null || DateRangeLabel is null || StartDatePicker is null || EndDatePicker is null)
            {
                return;
            }

            string selectedPeriod = (PeriodCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Today";
            DateRangeLabel.Text = selectedPeriod;

            DateTime today = DateTime.Now;
            switch (selectedPeriod)
            {
                case "Today":
                    StartDatePicker.SelectedDate = today;
                    EndDatePicker.SelectedDate   = today;
                    break;
                case "Yesterday":
                    StartDatePicker.SelectedDate = today.AddDays(-1);
                    EndDatePicker.SelectedDate   = today.AddDays(-1);
                    break;
                case "This Week":
                    StartDatePicker.SelectedDate = today.AddDays(-(int)today.DayOfWeek);
                    EndDatePicker.SelectedDate   = today;
                    break;
                case "Last Week":
                    StartDatePicker.SelectedDate = today.AddDays(-(int)today.DayOfWeek - 7);
                    EndDatePicker.SelectedDate   = today.AddDays(-(int)today.DayOfWeek - 1);
                    break;
                case "This Month":
                    StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
                    EndDatePicker.SelectedDate   = today;
                    break;
                case "Last Month":
                    StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    EndDatePicker.SelectedDate   = new DateTime(today.Year, today.Month, 1).AddDays(-1);
                    break;
                case "Last 3 Months":
                    StartDatePicker.SelectedDate = today.AddMonths(-3);
                    EndDatePicker.SelectedDate   = today;
                    break;
            }

            // Auto-refresh on preset selection (not on "Custom" — wait for Filter click)
            if (selectedPeriod != "Custom")
            {
                var (from, to) = GetSelectedDateRange();
                LoadData(from, to);
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker is null || EndDatePicker is null) return;

            var from = StartDatePicker.SelectedDate ?? DateTime.Today;
            var to   = EndDatePicker.SelectedDate   ?? DateTime.Today;

            if (from > to)
            {
                MessageBox.Show("Start date cannot be after end date.", "Invalid Date Range",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadData(from.Date, to.Date);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var (from, to) = GetSelectedDateRange();

            var dialog = new SaveFileDialog
            {
                Title      = "Export Report",
                Filter     = "Excel Workbook (*.xlsx)|*.xlsx|PDF Document (*.pdf)|*.pdf",
                FilterIndex = 1,
                FileName   = $"EZPos_Report_{from:yyyyMMdd}_{to:yyyyMMdd}",
                DefaultExt = ".xlsx",
                AddExtension = true
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                if (string.Equals(Path.GetExtension(dialog.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                    ExportToPdf(dialog.FileName, from, to);
                else
                    ExportToExcel(dialog.FileName, from, to);

                var result = MessageBox.Show(
                    $"Report exported successfully.\n\nOpen the file now?",
                    "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(string filePath, DateTime from, DateTime to)
        {
            using var workbook = new XLWorkbook();

            // ── Palette (readable light theme) ────────────────────────────
            var hdrFill    = XLColor.FromHtml("#1E293B");   // dark navy   — header bg
            var hdrFont    = XLColor.FromHtml("#FFFFFF");   // white       — header text
            var hdrAccent  = XLColor.FromHtml("#00D9FF");   // cyan        — accent header text (title)
            var altFill    = XLColor.FromHtml("#F1F5F9");   // light gray  — alternate data row
            var whiteFill  = XLColor.White;
            var totalFill  = XLColor.FromHtml("#E2E8F0");   // soft gray   — total row
            var bodyFont   = XLColor.FromHtml("#1E293B");   // dark        — body text
            var borderClr  = XLColor.FromHtml("#CBD5E1");   // subtle      — border
            var okColor    = XLColor.FromHtml("#166534");   // green text  — OK status
            var lowColor   = XLColor.FromHtml("#92400E");   // amber text  — Low Stock
            var outColor   = XLColor.FromHtml("#991B1B");   // red text    — Out of Stock
            var okFill     = XLColor.FromHtml("#DCFCE7");
            var lowFill    = XLColor.FromHtml("#FEF3C7");
            var outFill    = XLColor.FromHtml("#FEE2E2");

            var storeName = EZPos.DataAccess.Repositories.ConfigHelper.Get("StoreName", "EZPos");

            // ─────────────────────────────────────────────────────────────
            // Sheet 1 — Summary
            // ─────────────────────────────────────────────────────────────
            var ws1 = SetupSheet(workbook, "Summary");

            // Title banner (merged A1:E3)
            ws1.Range("A1:E1").Merge().Value = $"{storeName} — Sales Report";
            ws1.Cell("A1").Style.Font.Bold = true;
            ws1.Cell("A1").Style.Font.FontSize = 16;
            ws1.Cell("A1").Style.Font.FontColor = hdrAccent;
            ws1.Cell("A1").Style.Fill.BackgroundColor = hdrFill;
            ws1.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws1.Row(1).Height = 26;

            ws1.Range("A2:E2").Merge().Value = $"Period: {from:dd MMM yyyy} – {to:dd MMM yyyy}";
            ws1.Cell("A2").Style.Font.Italic = true;
            ws1.Cell("A2").Style.Font.FontColor = hdrFont;
            ws1.Cell("A2").Style.Fill.BackgroundColor = hdrFill;

            ws1.Range("A3:E3").Merge().Value = $"Generated: {DateTime.Now:dd MMM yyyy  hh:mm tt}";
            ws1.Cell("A3").Style.Font.Italic = true;
            ws1.Cell("A3").Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
            ws1.Cell("A3").Style.Fill.BackgroundColor = hdrFill;

            // KPI section label
            int r = 5;
            ws1.Cell(r, 1).Value = "KEY PERFORMANCE INDICATORS";
            ws1.Cell(r, 1).Style.Font.Bold = true;
            ws1.Cell(r, 1).Style.Font.FontColor = hdrFill;
            ws1.Cell(r, 1).Style.Font.FontSize = 10;
            r++;

            WriteHeaderRow(ws1, r, hdrFill, hdrFont, borderClr, "Metric", "Value");
            r++;

            var summary = _reportService.GetSummary(from, to);
            var kpis = new[]
            {
                ("Total Revenue",    $"RM {summary.TotalRevenue:N2}"),
                ("Total Orders",     summary.TotalOrders.ToString("N0")),
                ("Average Order",    $"RM {summary.AverageOrder:N2}"),
                ("Total Items Sold", summary.TotalItemsSold.ToString("N0")),
            };

            foreach (var (i, (metric, value)) in kpis.Select((k, i) => (i, k)))
            {
                var fill = i % 2 == 0 ? whiteFill : altFill;
                ws1.Cell(r, 1).Value = metric;
                ws1.Cell(r, 1).Style.Font.FontColor = bodyFont;
                ws1.Cell(r, 2).Value = value;
                ws1.Cell(r, 2).Style.Font.Bold = true;
                ws1.Cell(r, 2).Style.Font.FontColor = bodyFont;
                ApplyDataRowStyle(ws1.Range(r, 1, r, 2), fill, borderClr);
                r++;
            }

            // Payment breakdown sub-table
            r++;
            ws1.Cell(r, 1).Value = "PAYMENT METHOD BREAKDOWN";
            ws1.Cell(r, 1).Style.Font.Bold = true;
            ws1.Cell(r, 1).Style.Font.FontColor = hdrFill;
            ws1.Cell(r, 1).Style.Font.FontSize = 10;
            r++;

            WriteHeaderRow(ws1, r, hdrFill, hdrFont, borderClr, "Payment Method", "Orders", "Revenue (RM)", "% of Revenue");
            r++;

            var payments = _reportService.GetPaymentBreakdown(from, to);
            foreach (var (i, p) in payments.Select((p, i) => (i, p)))
            {
                var fill = i % 2 == 0 ? whiteFill : altFill;
                decimal pct = summary.TotalRevenue > 0 ? p.Revenue / summary.TotalRevenue * 100 : 0;
                ws1.Cell(r, 1).Value = p.Method;
                ws1.Cell(r, 2).Value = p.Orders;
                ws1.Cell(r, 3).Value = (double)p.Revenue;
                ws1.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                ws1.Cell(r, 4).Value = $"{pct:F1}%";
                SetRowFontColor(ws1.Range(r, 1, r, 4), bodyFont);
                ApplyDataRowStyle(ws1.Range(r, 1, r, 4), fill, borderClr);
                r++;
            }

            // Total row for payment
            if (payments.Count > 0)
            {
                ws1.Cell(r, 1).Value = "TOTAL";
                ws1.Cell(r, 1).Style.Font.Bold = true;
                ws1.Cell(r, 2).Value = payments.Sum(p => p.Orders);
                ws1.Cell(r, 2).Style.Font.Bold = true;
                ws1.Cell(r, 3).Value = (double)payments.Sum(p => p.Revenue);
                ws1.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                ws1.Cell(r, 3).Style.Font.Bold = true;
                ws1.Cell(r, 4).Value = "100.0%";
                ws1.Cell(r, 4).Style.Font.Bold = true;
                ws1.Range(r, 1, r, 4).Style.Fill.BackgroundColor = totalFill;
                SetRowFontColor(ws1.Range(r, 1, r, 4), bodyFont);
                ApplyBorder(ws1.Range(r, 1, r, 4), borderClr);
            }

            ws1.Column(1).Width = 26;
            ws1.Column(2).Width = 14;
            ws1.Column(3).Width = 18;
            ws1.Column(4).Width = 16;
            ws1.SheetView.FreezeRows(1);

            // ─────────────────────────────────────────────────────────────
            // Sheet 2 — Transactions
            // ─────────────────────────────────────────────────────────────
            var ws2 = SetupSheet(workbook, "Transactions");
            r = 1;
            WriteHeaderRow(ws2, r, hdrFill, hdrFont, borderClr, "Sale #", "Date", "Time", "Payment", "Items", "Total (RM)", "Items Detail");
            r++;

            var transactions = _reportService.GetTransactions(from, to);
            foreach (var (i, t) in transactions.Select((t, i) => (i, t)))
            {
                var fill = i % 2 == 0 ? whiteFill : altFill;
                ws2.Cell(r, 1).Value = t.SaleId;
                ws2.Cell(r, 2).Value = t.DateTime.ToString("dd MMM yyyy");
                ws2.Cell(r, 3).Value = t.DateTime.ToString("hh:mm tt");
                ws2.Cell(r, 4).Value = t.PaymentMethod;
                ws2.Cell(r, 5).Value = t.ItemCount;
                ws2.Cell(r, 6).Value = (double)t.TotalAmount;
                ws2.Cell(r, 6).Style.NumberFormat.Format = "#,##0.00";
                ws2.Cell(r, 7).Value = t.ItemsSummary;
                SetRowFontColor(ws2.Range(r, 1, r, 7), bodyFont);
                ApplyDataRowStyle(ws2.Range(r, 1, r, 7), fill, borderClr);
                r++;
            }

            // Grand total row
            if (transactions.Count > 0)
            {
                ws2.Cell(r, 1).Value = "GRAND TOTAL";
                ws2.Cell(r, 1).Style.Font.Bold = true;
                ws2.Cell(r, 5).Value = transactions.Sum(t => t.ItemCount);
                ws2.Cell(r, 5).Style.Font.Bold = true;
                ws2.Cell(r, 6).Value = (double)transactions.Sum(t => t.TotalAmount);
                ws2.Cell(r, 6).Style.NumberFormat.Format = "#,##0.00";
                ws2.Cell(r, 6).Style.Font.Bold = true;
                ws2.Range(r, 1, r, 7).Style.Fill.BackgroundColor = totalFill;
                SetRowFontColor(ws2.Range(r, 1, r, 7), bodyFont);
                ApplyBorder(ws2.Range(r, 1, r, 7), borderClr);
            }

            ws2.Column(1).Width = 10;
            ws2.Column(2).Width = 16;
            ws2.Column(3).Width = 12;
            ws2.Column(4).Width = 12;
            ws2.Column(5).Width = 8;
            ws2.Column(6).Width = 14;
            ws2.Column(7).Width = 50;
            ws2.SheetView.FreezeRows(1);

            // ─────────────────────────────────────────────────────────────
            // Sheet 3 — Daily Breakdown
            // ─────────────────────────────────────────────────────────────
            var ws3 = SetupSheet(workbook, "Daily Breakdown");
            r = 1;
            WriteHeaderRow(ws3, r, hdrFill, hdrFont, borderClr, "Date", "Revenue (RM)");
            r++;

            var daily = _reportService.GetDailyBreakdown(from, to);
            foreach (var (i, d) in daily.Select((d, i) => (i, d)))
            {
                var fill = i % 2 == 0 ? whiteFill : altFill;
                ws3.Cell(r, 1).Value = d.Label;
                ws3.Cell(r, 1).Style.Font.FontColor = bodyFont;
                ws3.Cell(r, 2).Value = (double)d.Revenue;
                ws3.Cell(r, 2).Style.NumberFormat.Format = "#,##0.00";
                ws3.Cell(r, 2).Style.Font.FontColor = bodyFont;
                ApplyDataRowStyle(ws3.Range(r, 1, r, 2), fill, borderClr);
                r++;
            }

            if (daily.Count > 0)
            {
                ws3.Cell(r, 1).Value = "TOTAL";
                ws3.Cell(r, 1).Style.Font.Bold = true;
                ws3.Cell(r, 1).Style.Font.FontColor = bodyFont;
                ws3.Cell(r, 2).FormulaA1 = $"=SUM(B2:B{r - 1})";
                ws3.Cell(r, 2).Style.Font.Bold = true;
                ws3.Cell(r, 2).Style.Font.FontColor = bodyFont;
                ws3.Cell(r, 2).Style.NumberFormat.Format = "#,##0.00";
                ws3.Range(r, 1, r, 2).Style.Fill.BackgroundColor = totalFill;
                ApplyBorder(ws3.Range(r, 1, r, 2), borderClr);
            }

            ws3.Column(1).Width = 20;
            ws3.Column(2).Width = 18;
            ws3.SheetView.FreezeRows(1);

            // ─────────────────────────────────────────────────────────────
            // Sheet 4 — Top Products
            // ─────────────────────────────────────────────────────────────
            var ws4 = SetupSheet(workbook, "Top Products");
            r = 1;
            WriteHeaderRow(ws4, r, hdrFill, hdrFont, borderClr, "Rank", "Product Name", "Qty Sold", "Revenue (RM)");
            r++;

            var top = _reportService.GetTopProducts(from, to, 20);
            foreach (var (i, p) in top.Select((p, i) => (i, p)))
            {
                var fill = i % 2 == 0 ? whiteFill : altFill;
                ws4.Cell(r, 1).Value = p.Rank;
                ws4.Cell(r, 2).Value = p.Name;
                ws4.Cell(r, 3).Value = p.Quantity;
                ws4.Cell(r, 4).Value = (double)p.Revenue;
                ws4.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                SetRowFontColor(ws4.Range(r, 1, r, 4), bodyFont);
                ApplyDataRowStyle(ws4.Range(r, 1, r, 4), fill, borderClr);
                r++;
            }

            ws4.Column(1).Width = 8;
            ws4.Column(2).Width = 32;
            ws4.Column(3).Width = 12;
            ws4.Column(4).Width = 18;
            ws4.SheetView.FreezeRows(1);

            // ─────────────────────────────────────────────────────────────
            // Sheet 5 — Hourly Breakdown
            // ─────────────────────────────────────────────────────────────
            var ws5 = SetupSheet(workbook, "Hourly Breakdown");
            r = 1;
            WriteHeaderRow(ws5, r, hdrFill, hdrFont, borderClr, "Time Slot", "Orders", "Sales (RM)");
            r++;

            var hourly = _reportService.GetHourlyBreakdown(from);
            foreach (var (i, h) in hourly.Select((h, i) => (i, h)))
            {
                var fill = i % 2 == 0 ? whiteFill : altFill;
                ws5.Cell(r, 1).Value = h.TimeSlot;
                ws5.Cell(r, 2).Value = h.Orders;
                ws5.Cell(r, 3).Value = (double)h.Sales;
                ws5.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
                SetRowFontColor(ws5.Range(r, 1, r, 3), bodyFont);
                ApplyDataRowStyle(ws5.Range(r, 1, r, 3), fill, borderClr);
                r++;
            }

            ws5.Column(1).Width = 18;
            ws5.Column(2).Width = 12;
            ws5.Column(3).Width = 16;
            ws5.SheetView.FreezeRows(1);

            // ─────────────────────────────────────────────────────────────
            // Sheet 6 — Stock Snapshot
            // ─────────────────────────────────────────────────────────────
            var ws6 = SetupSheet(workbook, "Stock Snapshot");
            r = 1;
            WriteHeaderRow(ws6, r, hdrFill, hdrFont, borderClr, "Barcode", "Product Name", "Category", "Price (RM)", "Current Stock", "Reorder Level", "Status");
            r++;

            var stock = _reportService.GetStockSnapshot();
            foreach (var (i, s) in stock.Select((s, i) => (i, s)))
            {
                var rowFill = s.Status == "Out of Stock" ? outFill
                            : s.Status == "Low Stock"    ? lowFill
                            : i % 2 == 0                 ? whiteFill : altFill;
                var statusColor = s.Status == "Out of Stock" ? outColor
                                : s.Status == "Low Stock"    ? lowColor
                                : okColor;

                ws6.Cell(r, 1).Value = s.Barcode;
                ws6.Cell(r, 2).Value = s.Name;
                ws6.Cell(r, 3).Value = s.Category;
                ws6.Cell(r, 4).Value = (double)s.Price;
                ws6.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                ws6.Cell(r, 5).Value = s.Stock;
                ws6.Cell(r, 6).Value = s.ReorderLevel;
                ws6.Cell(r, 7).Value = s.Status;
                ws6.Cell(r, 7).Style.Font.Bold = true;
                ws6.Cell(r, 7).Style.Font.FontColor = statusColor;

                SetRowFontColor(ws6.Range(r, 1, r, 6), bodyFont);
                ApplyDataRowStyle(ws6.Range(r, 1, r, 7), rowFill, borderClr);
                r++;
            }

            ws6.Column(1).Width = 18;
            ws6.Column(2).Width = 30;
            ws6.Column(3).Width = 16;
            ws6.Column(4).Width = 14;
            ws6.Column(5).Width = 16;
            ws6.Column(6).Width = 16;
            ws6.Column(7).Width = 14;
            ws6.SheetView.FreezeRows(1);

            workbook.SaveAs(filePath);
        }

        private void ExportToPdf(string filePath, DateTime from, DateTime to)
        {
            var summary = _reportService.GetSummary(from, to);
            var payments = _reportService.GetPaymentBreakdown(from, to);
            var top = _reportService.GetTopProducts(from, to, 10);
            var daily = _reportService.GetDailyBreakdown(from, to);
            var stock = _reportService.GetStockSnapshot().Take(10).ToList();
            var storeName = EZPos.DataAccess.Repositories.ConfigHelper.Get("StoreName", "EZPos");

            using var document = new PdfDocument();
            document.Info.Title = $"EZPos Report {from:yyyy-MM-dd} to {to:yyyy-MM-dd}";

            var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
            var headingFont = new XFont("Arial", 11, XFontStyle.Bold);
            var bodyFont = new XFont("Arial", 10, XFontStyle.Regular);
            var monoFont = new XFont("Courier New", 9, XFontStyle.Regular);
            var accentBrush = new XSolidBrush(XColor.FromArgb(0, 217, 255));
            var textBrush = new XSolidBrush(XColor.FromArgb(30, 41, 59));
            var mutedBrush = new XSolidBrush(XColor.FromArgb(100, 116, 139));
            var linePen = new XPen(XColor.FromArgb(203, 213, 225), 0.8);

            PdfPage page = document.AddPage();
            page.Size = PageSize.A4;
            XGraphics gfx = XGraphics.FromPdfPage(page);
            double margin = 40;
            double y = margin;
            double contentWidth = page.Width - (margin * 2);

            void EnsureSpace(double needed)
            {
                if (y + needed <= page.Height - margin)
                    return;

                page = document.AddPage();
                page.Size = PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                y = margin;
            }

            void DrawLine(string text, XFont font, XBrush brush, double indent = 0)
            {
                EnsureSpace(18);
                var rect = new XRect(margin + indent, y, contentWidth - indent, 16);
                gfx.DrawString(text, font, brush, rect, XStringFormats.TopLeft);
                y += 16;
            }

            void DrawSection(string title)
            {
                EnsureSpace(30);
                if (y > margin)
                    y += 6;

                gfx.DrawLine(linePen, margin, y, page.Width - margin, y);
                y += 8;
                DrawLine(title, headingFont, textBrush);
            }

            string Currency(decimal value) => $"RM {value:N2}";

            DrawLine(storeName, titleFont, accentBrush);
            DrawLine("Analytics & Reports", headingFont, textBrush);
            DrawLine($"Period: {from:dd MMM yyyy} - {to:dd MMM yyyy}", bodyFont, mutedBrush);
            DrawLine($"Generated: {DateTime.Now:dd MMM yyyy hh:mm tt}", bodyFont, mutedBrush);

            DrawSection("Summary");
            DrawLine($"Total Revenue      {Currency(summary.TotalRevenue)}", monoFont, textBrush);
            DrawLine($"Total Orders       {summary.TotalOrders:N0}", monoFont, textBrush);
            DrawLine($"Average Order      {Currency(summary.AverageOrder)}", monoFont, textBrush);
            DrawLine($"Total Items Sold   {summary.TotalItemsSold:N0}", monoFont, textBrush);

            DrawSection("Payment Methods");
            if (payments.Count == 0)
            {
                DrawLine("No payment data for the selected period.", bodyFont, mutedBrush);
            }
            else
            {
                foreach (var payment in payments)
                {
                    var percentage = summary.TotalRevenue > 0
                        ? payment.Revenue / summary.TotalRevenue * 100
                        : 0;
                    DrawLine($"{payment.Method,-12} {payment.Orders,4} orders   {Currency(payment.Revenue),12}   {percentage,5:F1}%", monoFont, textBrush);
                }
            }

            DrawSection("Top Products");
            if (top.Count == 0)
            {
                DrawLine("No product sales in the selected period.", bodyFont, mutedBrush);
            }
            else
            {
                foreach (var product in top)
                {
                    DrawLine($"#{product.Rank,-2} {product.Name}", bodyFont, textBrush);
                    DrawLine($"Qty {product.Quantity:N0}   Revenue {Currency(product.Revenue)}", monoFont, mutedBrush, 14);
                }
            }

            DrawSection("Daily Breakdown");
            if (daily.Count == 0)
            {
                DrawLine("No daily breakdown available.", bodyFont, mutedBrush);
            }
            else
            {
                foreach (var day in daily)
                    DrawLine($"{day.Label,-12} {Currency(day.Revenue)}", monoFont, textBrush);
            }

            DrawSection("Stock Snapshot");
            if (stock.Count == 0)
            {
                DrawLine("No stock data available.", bodyFont, mutedBrush);
            }
            else
            {
                foreach (var item in stock)
                {
                    DrawLine($"{item.Name} [{item.Category}]", bodyFont, textBrush);
                    DrawLine($"Barcode {item.Barcode}   Stock {item.Stock}   Reorder {item.ReorderLevel}   Status {item.Status}", monoFont, mutedBrush, 14);
                }
            }

            document.Save(filePath);
        }

        private static IXLWorksheet SetupSheet(XLWorkbook wb, string name)
        {
            var ws = wb.Worksheets.Add(name);
            ws.Style.Font.FontName = "Calibri";
            ws.Style.Font.FontSize = 11;
            return ws;
        }

        private static void WriteHeaderRow(IXLWorksheet ws, int row, XLColor fill, XLColor font, XLColor border, params string[] headers)
        {
            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = ws.Cell(row, col);
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold            = true;
                cell.Style.Font.FontColor       = font;
                cell.Style.Fill.BackgroundColor = fill;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = border;
            }
            ws.Row(row).Height = 20;
        }

        private static void ApplyDataRowStyle(IXLRange range, XLColor fill, XLColor border)
        {
            range.Style.Fill.BackgroundColor = fill;
            ApplyBorder(range, border);
        }

        private static void SetRowFontColor(IXLRange range, XLColor color)
        {
            range.Style.Font.FontColor = color;
        }

        private static void ApplyBorder(IXLRange range, XLColor border)
        {
            range.Style.Border.OutsideBorder     = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorderColor = border;
            range.Style.Border.InsideBorder      = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorderColor  = border;
        }

        private void RefreshData()
        {
            var (from, to) = GetSelectedDateRange();
            LoadData(from, to);
        }
    }
}
