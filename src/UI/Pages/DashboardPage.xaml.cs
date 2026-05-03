using System;
using System.Windows;
using System.Windows.Controls;
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
            // KPI cards — today's summary
            var summary = _reportService.GetTodaySummary();
            TodayRevenueText.Text = $"RM {summary.TotalRevenue:N2}";
            TodayOrdersText.Text  = summary.TotalOrders.ToString();
            AvgOrderText.Text     = $"RM {summary.AverageOrder:N2}";

            // Low stock alerts
            var alerts = _reportService.GetLowStockAlerts();
            LowStockCountText.Text = alerts.Count.ToString();

            if (alerts.Count == 0)
            {
                LowStockSubLabel.Text     = "— all items healthy";
                LowStockGrid.Visibility   = Visibility.Collapsed;
                EmptyAlertPanel.Visibility = Visibility.Visible;
            }
            else
            {
                LowStockSubLabel.Text     = $"({alerts.Count} item{(alerts.Count == 1 ? "" : "s")} need attention)";
                LowStockGrid.ItemsSource  = null;
                LowStockGrid.ItemsSource  = alerts;
                LowStockGrid.Visibility   = Visibility.Visible;
                EmptyAlertPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            DateLabel.Text = $"Today — {DateTime.Now:dddd, dd MMMM yyyy}";
            LoadData();
        }
    }
}
