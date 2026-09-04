using System.Windows;
using System.Windows.Controls;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;
using EZPos.UI.Dialogs;
using EZPos.UI.Input;
using EZPos.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace EZPos.UI.Pages
{
    /// <summary>
    /// Main barcode page — product selection, template config, bulk print.
    /// Code-behind is limited to InitializeComponent, DataContext wiring, the scanner event hookup,
    /// and opening WPF-chrome windows/dialogs (SaveFileDialog, print preview, template editor,
    /// QuickPrintDialog) that a ViewModel cannot own directly. All business logic lives in
    /// BarcodesPageViewModel.
    /// </summary>
    public partial class BarcodesPage : UserControl
    {
        private readonly BarcodesPageViewModel _viewModel;
        private readonly SalesKeyboardInputService _barcodeScanner = new();

        public BarcodesPage(BarcodesPageViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.StatusMessage += OnStatusMessage;
            _viewModel.OpenQuickPrintRequested += OnOpenQuickPrintRequested;

            // Damaged-label replacement: scanning a registered barcode opens QuickPrintDialog for it.
            _barcodeScanner.BarcodeCompleted += barcode => _viewModel.HandleBarcodeScanned(barcode);
            PreviewTextInput += (_, e) => _barcodeScanner.RegisterTextInput(e.Text);
            PreviewKeyDown += (_, e) => _barcodeScanner.TryHandleKeyDown(e.Key);

            Unloaded += (_, _) =>
            {
                _viewModel.StatusMessage -= OnStatusMessage;
                _viewModel.OpenQuickPrintRequested -= OnOpenQuickPrintRequested;
            };
        }

        private void OnStatusMessage(string message)
        {
            Dispatcher.Invoke(() =>
                MessageBox.Show(message, "Barcodes", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private async void OnOpenQuickPrintRequested(Product product)
        {
            var vm = _viewModel.CreateQuickPrintViewModel(product);
            var view = new QuickPrintDialog(vm);
            await DialogHost.Show(view, "RootDialog");
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            var document = _viewModel.BuildPreviewDocument();
            if (document == null)
            {
                MessageBox.Show("Select at least one product to preview.", "Nothing to Preview", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var previewWindow = new PrintPreviewWindow(document)
            {
                Owner = Window.GetWindow(this)
            };
            previewWindow.ShowDialog();
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = "EZPos-Labels.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.ExportPdf(dialog.FileName);
            }
        }

        private async void EditTemplates_Click(object sender, RoutedEventArgs e)
        {
            var editorVm = new LabelTemplateEditorViewModel(new LabelTemplateRepository());
            var view = new LabelTemplateEditorDialog(editorVm);
            await DialogHost.Show(view, "RootDialog");
            _viewModel.ReloadTemplates();
        }
    }
}
