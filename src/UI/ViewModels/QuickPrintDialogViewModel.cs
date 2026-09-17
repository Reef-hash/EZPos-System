using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using EZPos.Business.Services;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;

namespace EZPos.UI.ViewModels
{
    /// <summary>State + commands for QuickPrintDialog — single-product quick print from ProductsPage.</summary>
    public sealed class QuickPrintDialogViewModel : INotifyPropertyChanged
    {
        private readonly Product _product;
        private readonly ProductService _productService;
        private readonly LabelPrintService _printService;
        private readonly BarcodeLabelRepository _historyRepo;
        private readonly BarcodeService _barcodeService = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>Raised after a successful print — the dialog's code-behind closes on this.</summary>
        public event Action? PrintCompleted;
        public event Action<string>? StatusMessage;

        public string ProductName => _product.Name;
        public string Barcode => _product.Barcode;

        public ObservableCollection<LabelTemplate> Templates { get; } = new();
        public Array AvailableFormats { get; } = Enum.GetValues(typeof(BarcodeFormat));
        public ObservableCollection<string> Printers { get; } = new();

        private LabelTemplate? selectedTemplate;
        public LabelTemplate? SelectedTemplate
        {
            get => selectedTemplate;
            set => SetProperty(ref selectedTemplate, value);
        }

        private BarcodeFormat selectedFormat;
        public BarcodeFormat SelectedFormat
        {
            get => selectedFormat;
            set => SetProperty(ref selectedFormat, value);
        }

        private string? selectedPrinter;
        public string? SelectedPrinter
        {
            get => selectedPrinter;
            set => SetProperty(ref selectedPrinter, value);
        }

        private int quantity = 1;
        public int Quantity
        {
            get => quantity;
            set => SetProperty(ref quantity, Math.Max(1, value));
        }

        public RelayCommand PrintCommand { get; }

        public QuickPrintDialogViewModel(
            Product product,
            ProductService productService,
            LabelPrintService printService,
            LabelTemplateRepository templateRepo,
            BarcodeLabelRepository historyRepo)
        {
            _product = product;
            _productService = productService;
            _printService = printService;
            _historyRepo = historyRepo;

            foreach (var t in templateRepo.GetAll())
                Templates.Add(t);
            selectedTemplate = Templates.FirstOrDefault(t => t.IsDefault) ?? Templates.FirstOrDefault();

            foreach (var p in printService.GetInstalledPrinters())
                Printers.Add(p);
            selectedPrinter = Printers.FirstOrDefault();

            selectedFormat = product.BarcodeFormat;

            PrintCommand = new RelayCommand(_ => Print(), _ => SelectedTemplate != null);
        }

        private void Print()
        {
            var template = SelectedTemplate;
            if (template == null)
                return;

            _product.BarcodeFormat = SelectedFormat;
            try
            {
                _productService.Update(_product);
            }
            catch
            {
                // Non-fatal — proceed to print even if the format couldn't be persisted
            }

            var job = new LabelPrintJob
            {
                ProductId = _product.Id,
                ProductName = _product.Name,
                Barcode = _product.Barcode,
                Category = _product.Category,
                Price = _product.Price,
                Format = SelectedFormat,
                Quantity = Quantity
            };

            if (SelectedFormat == BarcodeFormat.EAN13 && !_barcodeService.ValidateEan13(job.Barcode))
            {
                StatusMessage?.Invoke("Warning: not a valid 13-digit EAN-13 barcode. The label may not scan correctly.");
            }

            try
            {
                _printService.PrintLabels(new[] { job }, template, SelectedPrinter);

                try
                {
                    _historyRepo.Insert(new BarcodeLabelRecord
                    {
                        ProductId = job.ProductId,
                        PrintedAt = DateTime.Now,
                        Quantity = Math.Max(1, job.Quantity),
                        TemplateName = template.Name,
                        BarcodeFormat = job.Format
                    });
                }
                catch
                {
                    // History logging must never block a print that already succeeded
                }

                PrintCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke($"Print failed: {ex.Message}");
            }
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
