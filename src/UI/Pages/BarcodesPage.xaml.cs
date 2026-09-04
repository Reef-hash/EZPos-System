using System.Windows;
using System.Windows.Controls;
using EZPos.UI.ViewModels;

namespace EZPos.UI.Pages
{
    /// <summary>
    /// Main barcode page — product selection, template config, bulk print.
    /// Code-behind is limited to InitializeComponent, DataContext wiring, and forwarding
    /// the ViewModel's status messages to the user. All logic lives in BarcodesPageViewModel.
    /// </summary>
    public partial class BarcodesPage : UserControl
    {
        private readonly BarcodesPageViewModel _viewModel;

        public BarcodesPage(BarcodesPageViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.StatusMessage += OnStatusMessage;
            Unloaded += (_, _) => _viewModel.StatusMessage -= OnStatusMessage;
        }

        private void OnStatusMessage(string message)
        {
            Dispatcher.Invoke(() =>
                MessageBox.Show(message, "Barcodes", MessageBoxButton.OK, MessageBoxImage.Information));
        }
    }
}
