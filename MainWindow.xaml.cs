using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EZPos.Business.Services;
using EZPos.DataAccess.Repositories;
using EZPos.UI.Dialogs;
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
        private readonly CategoryService categoryService;
        private string currentPage = DefaultRoute;

        public MainWindow(PosStateStore stateStore, ProductService productService, SaleService saleService, StockService stockService, ReportService reportService, CategoryService categoryService)
        {
            InitializeComponent();

            this.stateStore      = stateStore;
            this.productService  = productService;
            this.saleService     = saleService;
            this.stockService    = stockService;
            this.reportService   = reportService;
            this.categoryService = categoryService;

            navigationService = new NavigationService();
            RegisterRoutes();

            NavigateToPage(DefaultRoute);
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;
            await CheckForUpdatesOnStartupAsync();
        }

        private async Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                var manifestUrl = ConfigHelper.Get("App:UpdateManifestUrl", "");
                if (string.IsNullOrWhiteSpace(manifestUrl))
                {
                    return;
                }

                var currentVersion = GetCurrentAppVersion();
                var updater = new UpdaterService(currentVersion, manifestUrl);
                var manifest = await updater.CheckForUpdatesAsync();

                if (manifest == null)
                {
                    return;
                }

                var dialog = new UpdateAvailableDialog(manifest, currentVersion)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() != true || !dialog.UserClickedUpdate)
                {
                    return;
                }

                var installerPath = Path.Combine(Path.GetTempPath(), $"EZPos-Setup-v{manifest.Version}.exe");
                var downloadSuccess = await updater.DownloadInstallerAsync(
                    manifest.DownloadUrl ?? string.Empty,
                    manifest.Checksum?.Algorithm,
                    manifest.Checksum?.Value,
                    installerPath);

                if (!downloadSuccess)
                {
                    return;
                }

                try
                {
                    var dbDir = Path.GetDirectoryName(Database.DbFile) ?? AppDomain.CurrentDomain.BaseDirectory;
                    var backupPath = Path.Combine(dbDir, $"EZPos_PreUpdate_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                    File.Copy(Database.DbFile, backupPath, overwrite: false);
                }
                catch
                {
                    // Keep startup update flow non-intrusive.
                }

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = "/SILENT /NORESTART",
                        UseShellExecute = false
                    });

                    await Task.Delay(1000);
                    Application.Current.Shutdown();
                }
                catch
                {
                    // Keep startup update flow non-intrusive.
                }
            }
            catch
            {
                // Startup auto-check must be silent and never crash the app.
            }
        }

        private static string GetCurrentAppVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informational))
            {
                return informational.Split('+')[0];
            }

            var version = assembly.GetName().Version;
            if (version == null)
            {
                return "1.0.0";
            }

            var build = version.Build < 0 ? 0 : version.Build;
            return $"{version.Major}.{version.Minor}.{build}";
        }

        private void RegisterRoutes()
        {
            navigationService.Register("Dashboard", () => new UI.Pages.DashboardPage(reportService));
            navigationService.Register("Sales",     () => new UI.Pages.SalesPage(stateStore, saleService, categoryService));
            navigationService.Register("Products",  () => new UI.Pages.ProductsPage(stateStore, productService, categoryService));
            navigationService.Register("Stock",     () => new UI.Pages.StockPage(stateStore, stockService, categoryService));
            navigationService.Register("Reports",   () => new UI.Pages.ReportsPage());
            navigationService.Register("Settings",  () => new UI.Pages.SettingsPage(stateStore));
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

        // ── Custom title bar ──────────────────────────────────────────────
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ToggleMaximize();
            else
                DragMove();
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeIcon.Icon = FontAwesome.Sharp.IconChar.Expand;
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeIcon.Icon = FontAwesome.Sharp.IconChar.Compress;
            }
        }
    }
}