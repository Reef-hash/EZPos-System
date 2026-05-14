using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EZPos.Business.Services;

namespace EZPos.UI.Pages
{
    public partial class DashboardPage : UserControl
    {
        private readonly ReportService _reportService;

        public DashboardPage(ReportService reportService)
        {
            _reportService = reportService;
            InitializeComponent();
            Loaded += DashboardPage_Loaded;
        }

        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            DateLabel.Text = $"Today — {DateTime.Now:dddd, dd MMMM yyyy}";
            LoadData();
        }

        private void LoadData()
        {
            var summary = _reportService.GetTodaySummary();
            var alerts  = _reportService.GetLowStockAlerts();

            // Count-up animations — numbers "type themselves" on load
            AnimateCurrency(TodayRevenueText, (double)summary.TotalRevenue, "RM ");
            AnimateInt(TodayOrdersText,  summary.TotalOrders);
            AnimateInt(LowStockCountText, alerts.Count);
            AnimateCurrency(AvgOrderText, (double)summary.AverageOrder, "RM ");

            // Low stock table
            if (alerts.Count == 0)
            {
                LowStockSubLabel.Text      = "— all items healthy";
                LowStockGrid.Visibility    = Visibility.Collapsed;
                EmptyAlertPanel.Visibility = Visibility.Visible;
            }
            else
            {
                LowStockSubLabel.Text      = $"({alerts.Count} item{(alerts.Count == 1 ? "" : "s")} need attention)";
                LowStockGrid.ItemsSource   = null;
                LowStockGrid.ItemsSource   = alerts;
                LowStockGrid.Visibility    = Visibility.Visible;
                EmptyAlertPanel.Visibility = Visibility.Collapsed;
            }
        }

        // Animate decimal values (e.g. RM 0.00 → RM 1,250.50)
        private static void AnimateCurrency(TextBlock target, double targetValue, string prefix)
        {
            const int    steps     = 35;
            const int    intervalMs = 16;  // ~60fps
            double       increment  = targetValue / steps;
            int          step       = 0;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(intervalMs) };
            timer.Tick += (s, e) =>
            {
                step++;
                double current = Math.Min(targetValue, increment * step);
                target.Text = $"{prefix}{current:N2}";
                if (step >= steps)
                {
                    target.Text = $"{prefix}{targetValue:N2}";
                    timer.Stop();
                }
            };
            timer.Start();
        }

        // Animate integer values (e.g. 0 → 24)
        private static void AnimateInt(TextBlock target, int targetValue)
        {
            if (targetValue == 0) { target.Text = "0"; return; }

            const int steps      = 25;
            const int intervalMs = 20;
            double    increment  = (double)targetValue / steps;
            int       step       = 0;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(intervalMs) };
            timer.Tick += (s, e) =>
            {
                step++;
                int current = (int)Math.Min(targetValue, Math.Round(increment * step));
                target.Text = current.ToString();
                if (step >= steps)
                {
                    target.Text = targetValue.ToString();
                    timer.Stop();
                }
            };
            timer.Start();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            DateLabel.Text = $"Today — {DateTime.Now:dddd, dd MMMM yyyy}";
            LoadData();
        }
    }
}
