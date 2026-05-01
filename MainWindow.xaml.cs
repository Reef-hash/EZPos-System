using System;
using System.Windows;

namespace EZPos.UI
{
    public partial class MainWindow : Window
    {
        private string currentPage = "Sales";

        public MainWindow()
        {
            InitializeComponent();
            NavigateToPage("Sales");
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            string pageName = button.Tag.ToString();
            NavigateToPage(pageName);
        }

        private void NavigateToPage(string pageName)
        {
            currentPage = pageName;
            System.Windows.Controls.UserControl page = null;

            switch (pageName)
            {
                case "Sales":
                    page = new UI.Pages.SalesPage();
                    break;
                case "Products":
                    page = new UI.Pages.ProductsPage();
                    break;
                case "Stock":
                    page = new UI.Pages.StockPage();
                    break;
                case "Reports":
                    page = new UI.Pages.ReportsPage();
                    break;
            }

            if (page != null)
            {
                MainContent.Content = page;
                HighlightNavButton(pageName);
            }
        }

        private void HighlightNavButton(string pageName)
        {
            SalesNavBtn.Background = pageName == "Sales" ? System.Windows.Application.Current.Resources["{StaticResource PrimaryBrush}"] as System.Windows.Media.Brush : System.Windows.Application.Current.Resources["{StaticResource SidebarBrush}"] as System.Windows.Media.Brush;
            ProductsNavBtn.Background = pageName == "Products" ? System.Windows.Application.Current.Resources["{StaticResource PrimaryBrush}"] as System.Windows.Media.Brush : System.Windows.Application.Current.Resources["{StaticResource SidebarBrush}"] as System.Windows.Media.Brush;
            StockNavBtn.Background = pageName == "Stock" ? System.Windows.Application.Current.Resources["{StaticResource PrimaryBrush}"] as System.Windows.Media.Brush : System.Windows.Application.Current.Resources["{StaticResource SidebarBrush}"] as System.Windows.Media.Brush;
            ReportsNavBtn.Background = pageName == "Reports" ? System.Windows.Application.Current.Resources["{StaticResource PrimaryBrush}"] as System.Windows.Media.Brush : System.Windows.Application.Current.Resources["{StaticResource SidebarBrush}"] as System.Windows.Media.Brush;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Logout functionality coming soon!", "Logout", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings coming soon!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("EZPos System v1.0\nModern Point of Sale Application", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}