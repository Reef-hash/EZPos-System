using System;
using System.Windows;
using EZPos.Business.Services;
using EZPos.DataAccess.Repositories;
using EZPos.UI;
using EZPos.UI.State;

namespace EZPos
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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
                // 1. Initialize DB schema (creates tables / migrates columns if needed)
                Database.Initialize();

                // 2. Create shared state store and load products from DB
                var stateStore = new PosStateStore();
                var productService = new ProductService(stateStore);
                productService.LoadAll();

                // 3. Create shared services
                var saleService   = new SaleService(stateStore);
                var stockService  = new StockService(stateStore);
                var reportService = new ReportService();

                // 4. Launch main window with all services injected
                MainWindow = new MainWindow(stateStore, productService, saleService, stockService, reportService);
                MainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}\n\n{ex.StackTrace}",
                    "EZPos - Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}
