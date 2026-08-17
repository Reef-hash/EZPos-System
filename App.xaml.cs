using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using EZPos.Business.Services;
using EZPos.Core.Licensing;
using EZPos.DataAccess.Repositories;
using EZPos.Infrastructure.Licensing;
using EZPos.UI;
using EZPos.UI.Licensing;
using EZPos.UI.State;

namespace EZPos
{
    public partial class App : Application
    {
        private readonly CancellationTokenSource _licenseRevalidationCts = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Required for Code Page 437 (PC437/ESC-POS) on .NET 6+
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show(
                    $"Unhandled crash:\n\n{args.Exception.GetType().Name}: {args.Exception.Message}\n\n{args.Exception.StackTrace}",
                    "EZPos - Unhandled Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            try
            {
                // 0. Migrate data from old location (app folder) to new location (%ProgramData%)
                MigrateToNewDataLocation();

                // 1. Initialize DB schema (creates tables / migrates columns if needed)
                Database.Initialize();

                // 4. License check — runs before any UI is shown.
                //
                //    Mode: ONLINE (LicenseService → LicenseApiClient → /api/licenses/validate)
                //    API base URL read from config.ini: App:LicenseApiUrl
                //    Falls back to 7-day grace period cache when API is unreachable.
                var apiUrl    = ConfigHelper.Get("App:LicenseApiUrl", "https://ezpos-landing.onrender.com");
                var apiClient = new LicenseApiClient(apiUrl);
                ILicenseService licenseService = new LicenseService(new FileLicenseStorage(), apiClient);
                licenseService.LoadAndValidate();

                switch (licenseService.Current.Status)
                {
                    case LicenseStatus.Valid:
                        break; // continue startup normally

                    case LicenseStatus.Missing:
                    case LicenseStatus.Invalid:
                    case LicenseStatus.NotActivated:
                        var activationWindow = new LicenseRequiredWindow(licenseService, apiClient);
                        if (activationWindow.ShowDialog() != true)
                        {
                            Shutdown(0);
                            return;
                        }
                        break;

                    case LicenseStatus.Expired:
                    default:
                        // Grace period has lapsed (or license was rejected outright).
                        // TrialExpiredWindow offers a manual "Check Connection & Retry"
                        // button — DialogResult is only true if a fresh online validation
                        // inside that window came back Valid, so the app is safe to continue.
                        var expiredWindow = new TrialExpiredWindow(licenseService.Current, licenseService, apiClient);
                        if (expiredWindow.ShowDialog() != true)
                        {
                            Shutdown(1);
                            return;
                        }
                        break;
                }

                // While the app is open, keep quietly retrying the API in the background
                // if we're currently running on the offline grace-period cache — refreshes
                // the cache the moment the (often-sleeping, free-tier) backend responds,
                // without blocking startup or requiring the user to close and reopen.
                licenseService.StartBackgroundRevalidation(_licenseRevalidationCts.Token);

                // 3. Create shared state store and load products from DB
                var stateStore = new PosStateStore();
                var productService = new ProductService(stateStore);
                productService.LoadAll();

                // 5. Create shared services
                var saleService      = new SaleService(stateStore);
                var stockService     = new StockService(stateStore);
                var reportService    = new ReportService();
                var categoryService  = new CategoryService();

                // 6. Launch main window with all services injected
                MainWindow = new MainWindow(stateStore, productService, saleService, stockService, reportService, categoryService, licenseService);
                MainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.Message}", "EZPos", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _licenseRevalidationCts.Cancel();
            _licenseRevalidationCts.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// One-time migration: copy data from old location (app folder) to new location (%ProgramData%\EZPos\)
        /// This runs on first launch of new version; subsequent launches skip silently.
        /// </summary>
        private void MigrateToNewDataLocation()
        {
            try
            {
                // Old location: next to EZPos.exe in install folder
                var oldDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EZPos.db");
                var oldConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "config.ini");
                var oldLicensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.dat");

                // New location: %ProgramData%\EZPos\
                var programDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "EZPos");
                var newDbPath = Path.Combine(programDataDir, "EZPos.db");
                var newConfigPath = Path.Combine(programDataDir, "config.ini");
                var newLicensePath = Path.Combine(programDataDir, "license.dat");

                // Ensure new directory exists
                if (!Directory.Exists(programDataDir))
                    Directory.CreateDirectory(programDataDir);

                // Migrate database: copy only if old exists and new doesn't
                if (File.Exists(oldDbPath) && !File.Exists(newDbPath))
                {
                    File.Copy(oldDbPath, newDbPath, overwrite: false);
                    MessageBox.Show(
                        $"Database migrated to:\n{newDbPath}",
                        "EZPos Data Migration",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                // Migrate config: copy only if old exists and new doesn't
                if (File.Exists(oldConfigPath) && !File.Exists(newConfigPath))
                {
                    File.Copy(oldConfigPath, newConfigPath, overwrite: false);
                }

                // Migrate license: copy only if old exists and new doesn't
                if (File.Exists(oldLicensePath) && !File.Exists(newLicensePath))
                {
                    File.Copy(oldLicensePath, newLicensePath, overwrite: false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Warning: Data migration encountered an issue:\n{ex.Message}\n\nThe app will continue, but check your backup.",
                    "EZPos - Data Migration Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
