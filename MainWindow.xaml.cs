using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EZPos.Business.Services;
using EZPos.UI.Navigation;
using EZPos.UI.State;

namespace EZPos.UI
{
    public partial class MainWindow : Window
    {
        private const string DefaultRoute = "Dashboard";
        private readonly PosStateStore stateStore;
        private readonly NavigationService navigationService;
        private readonly ProductService productService;
        private readonly SaleService saleService;
        private readonly StockService stockService;
        private readonly ReportService reportService;
        private string currentPage = DefaultRoute;

        public MainWindow(PosStateStore stateStore, ProductService productService, SaleService saleService, StockService stockService, ReportService reportService)
        {
            InitializeComponent();

            this.stateStore     = stateStore;
            this.productService = productService;
            this.saleService    = saleService;
            this.stockService   = stockService;
            this.reportService  = reportService;

            navigationService = new NavigationService();
            RegisterRoutes();

            NavigateToPage(DefaultRoute);
        }

        private void RegisterRoutes()
        {
            navigationService.Register("Dashboard", () => new UI.Pages.DashboardPage(reportService));
            navigationService.Register("Sales",     () => new UI.Pages.SalesPage(stateStore, saleService));
            navigationService.Register("Products",  () => new UI.Pages.ProductsPage(stateStore, productService));
            navigationService.Register("Stock",     () => new UI.Pages.StockPage(stateStore, stockService));
            navigationService.Register("Reports",   () => new UI.Pages.ReportsPage());
            navigationService.Register("Settings",  () => new UI.Pages.SettingsPage());
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            var route = button.Tag as string;
            if (string.IsNullOrWhiteSpace(route))
            {
                return;
            }

            NavigateToPage(route);
        }

        private void NavigateToPage(string pageName)
        {
            try
            {
                if (!navigationService.TryCreatePage(pageName, out var page) || page is null)
                {
                    return;
                }

                currentPage = pageName;
                MainContent.Content = page;
                HighlightNavButton(pageName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open {pageName} page.\n\n{ex}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);

                if (!string.Equals(pageName, DefaultRoute, StringComparison.OrdinalIgnoreCase))
                {
                    NavigateToPage(DefaultRoute);
                }
            }
        }

        private void HighlightNavButton(string pageName)
        {
            var activeBackground = FindResource("PrimaryBrush") as Brush;
            var inactiveBackground = Brushes.Transparent;
            var activeForeground = FindResource("SidebarBrush") as Brush;
            var inactiveForeground = FindResource("TextSecondaryBrush") as Brush;

            ApplyNavState(DashboardNavBtn, pageName == "Dashboard", activeBackground, inactiveBackground, activeForeground, inactiveForeground);
            ApplyNavState(SalesNavBtn,      pageName == "Sales",     activeBackground, inactiveBackground, activeForeground, inactiveForeground);
            ApplyNavState(ProductsNavBtn,   pageName == "Products",  activeBackground, inactiveBackground, activeForeground, inactiveForeground);
            ApplyNavState(StockNavBtn,      pageName == "Stock",     activeBackground, inactiveBackground, activeForeground, inactiveForeground);
            ApplyNavState(ReportsNavBtn,    pageName == "Reports",   activeBackground, inactiveBackground, activeForeground, inactiveForeground);
            ApplyNavState(SettingsNavBtn,   pageName == "Settings",  activeBackground, inactiveBackground, activeForeground, inactiveForeground);
        }

        private static void ApplyNavState(Button button, bool isActive, Brush? activeBackground, Brush inactiveBackground, Brush? activeForeground, Brush? inactiveForeground)
        {
            button.Background = isActive ? activeBackground : inactiveBackground;
            button.Foreground = isActive ? activeForeground : inactiveForeground;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Logout functionality coming soon!", "Logout", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("Settings");
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("EZPos System v1.0\nModern Point of Sale Application", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}