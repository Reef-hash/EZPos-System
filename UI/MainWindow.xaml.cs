using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EZPos.UI
{
    public partial class MainWindow : Window
    {
        private Button _activeNavButton;

        public MainWindow()
        {
            InitializeComponent();
            // Default page
            NavigateToPage("Sales");
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string page = btn.Content.ToString();
                NavigateToPage(page);
                HighlightNavButton(btn);
            }
        }

        private void NavigateToPage(string page)
        {
            UserControl pageControl = page switch
            {
                "Sales" => new SalesPage(),
                "Products" => new ProductsPage(),
                "Stock" => new StockPage(),
                "Reports" => new ReportsPage(),
                _ => null
            };
            MainContent.Content = pageControl;

            // Highlight correct nav button
            switch (page)
            {
                case "Sales": HighlightNavButton(SalesNavBtn); break;
                case "Products": HighlightNavButton(ProductsNavBtn); break;
                case "Stock": HighlightNavButton(StockNavBtn); break;
                case "Reports": HighlightNavButton(ReportsNavBtn); break;
            }
        }

        private void HighlightNavButton(Button btn)
        {
            // Reset all nav buttons
            SalesNavBtn.Background = Brushes.Transparent;
            ProductsNavBtn.Background = Brushes.Transparent;
            StockNavBtn.Background = Brushes.Transparent;
            ReportsNavBtn.Background = Brushes.Transparent;

            SalesNavBtn.Foreground = Brushes.White;
            ProductsNavBtn.Foreground = Brushes.White;
            StockNavBtn.Foreground = Brushes.White;
            ReportsNavBtn.Foreground = Brushes.White;

            // Highlight active
            btn.Background = (Brush)FindResource("AccentBrush");
            btn.Foreground = Brushes.White;
            _activeNavButton = btn;
        }
    }
}
