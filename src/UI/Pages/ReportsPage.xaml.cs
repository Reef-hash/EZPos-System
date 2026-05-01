using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EZPos.UI.Pages
{
    public partial class ReportsPage : UserControl
    {
        private ObservableCollection<ChartData> salesTrendData;
        private ObservableCollection<TopProduct> topProducts;
        private ObservableCollection<PeakHour> peakHours;

        public class ChartData
        {
            public string Label { get; set; }
            public decimal Value { get; set; }
            public double BarHeight { get; set; }
            public Brush BarColor { get; set; }
        }

        public class TopProduct
        {
            public int Rank { get; set; }
            public string Name { get; set; }
            public int Quantity { get; set; }
            public decimal Revenue { get; set; }
        }

        public class PeakHour
        {
            public string TimeSlot { get; set; }
            public int Orders { get; set; }
            public decimal Sales { get; set; }
        }

        public ReportsPage()
        {
            InitializeComponent();
            InitializeData();
            BindData();
        }

        private void InitializeData()
        {
            // Sales Trend Data (Last 7 days)
            salesTrendData = new ObservableCollection<ChartData>
            {
                new ChartData { Label = "Mon", Value = 1250, BarHeight = 150, BarColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00D9FF")) },
                new ChartData { Label = "Tue", Value = 1680, BarHeight = 200, BarColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00D9FF")) },
                new ChartData { Label = "Wed", Value = 1420, BarHeight = 170, BarColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00D9FF")) },
                new ChartData { Label = "Thu", Value = 2100, BarHeight = 250, BarColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00D9FF")) },
                new ChartData { Label = "Fri", Value = 2450, BarHeight = 290, BarColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00D9FF")) },
                new ChartData { Label = "Sat", Value = 3200, BarHeight = 380, BarColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF10B981")) },
                new ChartData { Label = "Sun", Value = 2800, BarHeight = 330, BarColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF10B981")) },
            };

            // Top Products
            topProducts = new ObservableCollection<TopProduct>
            {
                new TopProduct { Rank = 1, Name = "Cappuccino", Quantity = 245, Revenue = 1592.50m },
                new TopProduct { Rank = 2, Name = "Espresso", Quantity = 189, Revenue = 1039.50m },
                new TopProduct { Rank = 3, Name = "Cheese Sandwich", Quantity = 156, Revenue = 1326.00m },
                new TopProduct { Rank = 4, Name = "Cookie Mix", Quantity = 342, Revenue = 1197.00m },
                new TopProduct { Rank = 5, Name = "Donut Box", Quantity = 198, Revenue = 891.00m },
            };

            // Peak Hours
            peakHours = new ObservableCollection<PeakHour>
            {
                new PeakHour { TimeSlot = "08:00-09:00", Orders = 28, Sales = 2145.50m },
                new PeakHour { TimeSlot = "09:00-10:00", Orders = 35, Sales = 2890.75m },
                new PeakHour { TimeSlot = "10:00-11:00", Orders = 22, Sales = 1567.25m },
                new PeakHour { TimeSlot = "11:00-12:00", Orders = 31, Sales = 2456.00m },
                new PeakHour { TimeSlot = "12:00-13:00", Orders = 42, Sales = 3200.50m },
                new PeakHour { TimeSlot = "13:00-14:00", Orders = 18, Sales = 1234.75m },
            };
        }

        private void BindData()
        {
            SalesTrendChart.ItemsSource = salesTrendData;
            TopProductsGrid.ItemsSource = topProducts;
            PeakHoursGrid.ItemsSource = peakHours;
        }

        private void PeriodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedPeriod = (PeriodCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Today";
            DateRangeLabel.Text = selectedPeriod;

            // Update start and end dates based on selection
            DateTime today = DateTime.Now;
            switch (selectedPeriod)
            {
                case "Today":
                    StartDatePicker.SelectedDate = today;
                    EndDatePicker.SelectedDate = today;
                    break;
                case "Yesterday":
                    StartDatePicker.SelectedDate = today.AddDays(-1);
                    EndDatePicker.SelectedDate = today.AddDays(-1);
                    break;
                case "This Week":
                    DateTime weekStart = today.AddDays(-(int)today.DayOfWeek);
                    StartDatePicker.SelectedDate = weekStart;
                    EndDatePicker.SelectedDate = today;
                    break;
                case "Last Week":
                    DateTime lastWeekStart = today.AddDays(-(int)today.DayOfWeek - 7);
                    DateTime lastWeekEnd = today.AddDays(-(int)today.DayOfWeek - 1);
                    StartDatePicker.SelectedDate = lastWeekStart;
                    EndDatePicker.SelectedDate = lastWeekEnd;
                    break;
                case "This Month":
                    StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
                    EndDatePicker.SelectedDate = today;
                    break;
                case "Last Month":
                    DateTime lastMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    DateTime lastMonthEnd = new DateTime(today.Year, today.Month, 1).AddDays(-1);
                    StartDatePicker.SelectedDate = lastMonthStart;
                    EndDatePicker.SelectedDate = lastMonthEnd;
                    break;
                case "Last 3 Months":
                    StartDatePicker.SelectedDate = today.AddMonths(-3);
                    EndDatePicker.SelectedDate = today;
                    break;
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime startDate = StartDatePicker.SelectedDate ?? DateTime.Now;
            DateTime endDate = EndDatePicker.SelectedDate ?? DateTime.Now;

            if (startDate > endDate)
            {
                MessageBox.Show("Start date cannot be after end date.", "Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Filtering reports from {startDate:dd/MM/yyyy} to {endDate:dd/MM/yyyy}", 
                "Filter Applied", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // In real application, would refresh data based on date range
            RefreshData();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export functionality would save report to PDF or Excel.", 
                "Export Report", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshData()
        {
            // Update KPI values (in real app, would fetch from database)
            // For demo, just show update
            MessageBox.Show("Data refreshed successfully!", "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
