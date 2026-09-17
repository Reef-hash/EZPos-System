using System.Windows;
using System.Windows.Controls;
using EZPos.UI.ViewModels;

namespace EZPos.UI.Dialogs
{
    /// <summary>
    /// Create/edit/delete label templates. Code-behind only wires DataContext and forwards
    /// status messages — all logic lives in LabelTemplateEditorViewModel.
    /// </summary>
    public partial class LabelTemplateEditorDialog : UserControl
    {
        private readonly LabelTemplateEditorViewModel _viewModel;

        public LabelTemplateEditorDialog(LabelTemplateEditorViewModel viewModel)
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
                MessageBox.Show(message, "Label Templates", MessageBoxButton.OK, MessageBoxImage.Information));
        }
    }
}
