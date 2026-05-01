using System;
using System.Windows;
using EZPos.UI;

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
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}\n\n{ex.StackTrace}", 
                    "EZPos - Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown(1);
            }
        }
    }
}
