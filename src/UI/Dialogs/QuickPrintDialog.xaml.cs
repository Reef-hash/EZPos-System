using System.Windows;
using System.Windows.Controls;
using EZPos.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace EZPos.UI.Dialogs
{
    /// <summary>
    /// Single-product quick print, opened from the ProductsPage toolbar.
    /// Code-behind only wires DataContext and the DialogHost close commands — all
    /// printing logic lives in QuickPrintDialogViewModel.
    /// </summary>
    public partial class QuickPrintDialog : UserControl
    {
        private readonly QuickPrintDialogViewModel _viewModel;

        public QuickPrintDialog(QuickPrintDialogViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.PrintCompleted += OnPrintCompleted;
            _viewModel.StatusMessage += OnStatusMessage;
        }

        private void OnPrintCompleted()
        {
            Dispatcher.Invoke(() => DialogHost.CloseDialogCommand.Execute(true, this));
        }

        private void OnStatusMessage(string message)
        {
            Dispatcher.Invoke(() =>
                MessageBox.Show(message, "Print Label", MessageBoxButton.OK, MessageBoxImage.Warning));
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.PrintCommand.CanExecute(null))
                _viewModel.PrintCommand.Execute(null);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogHost.CloseDialogCommand.Execute(null, this);
        }
    }
}
