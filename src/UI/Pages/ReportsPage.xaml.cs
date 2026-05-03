using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EZPos.Business.Services;

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
            MessageBox.Show("Export to PDF will be available in Phase 4.",
                "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshData()
        {
            var (from, to) = GetSelectedDateRange();
            LoadData(from, to);
        }
    }
}
