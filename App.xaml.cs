using System;
using System.IO;
using System.Text;
using System.Windows;
using EZPos.Business.Services;
using EZPos.Core.Licensing;
using EZPos.DataAccess.Repositories;
using EZPos.UI;
using EZPos.UI.Licensing;
using EZPos.UI.State;

namespace EZPos
{
    public partial class App : Application
    {
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
                //    LicenseService is currently MOCK: always returns Valid,
                //    so this block never blocks the app in development.
                //    TODO: when real API is ready, remove the mock comment below.
                var licenseStorage = new FileLicenseStorage();
                var licenseService = new LicenseService(licenseStorage);
                licenseService.LoadAndValidate();
                if (!licenseService.IsLicensed)
                {
                    var licenseWindow = new LicenseRequiredWindow(licenseService);
                    if (licenseWindow.ShowDialog() != true)
                    {
                        Shutdown(1);
                        return;
                    }
                }

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
                MainWindow = new MainWindow(stateStore, productService, saleService, stockService, reportService, categoryService);
                MainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}\n\n{ex.StackTrace}",
                    "EZPos - Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
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
