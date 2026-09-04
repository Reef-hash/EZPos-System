using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using EZPos.Business.Services;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;
using EZPos.UI.State;

namespace EZPos.UI.ViewModels
{
    /// <summary>Read-only projection of a BarcodeLabelRecord for display in the History panel.</summary>
    public sealed class BarcodeLabelHistoryRow
    {
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public string TemplateName { get; init; } = string.Empty;
        public BarcodeFormat Format { get; init; }
        public DateTime PrintedAt { get; init; }
    }

    /// <summary>One product row shown on the Barcodes page, with a page-local selection flag.</summary>
    public sealed class BarcodeProductRow : ObservableEntity
    {
        private bool isSelected;

        public int Id { get; init; }
        public string Barcode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public BarcodeFormat Format { get; init; } = BarcodeFormat.Code128;

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
    }

    /// <summary>State + commands for BarcodesPage — product selection, template config, bulk print.</summary>
    public sealed class BarcodesPageViewModel : INotifyPropertyChanged
    {
        public const string AllCategoriesLabel = "All Categories";

        private readonly PosStateStore _stateStore;
        private readonly BarcodeService _barcodeService;
        private readonly LabelPrintService _printService;
        private readonly LabelTemplateRepository _templateRepo;
        private readonly CategoryService _categoryService;
        private readonly BarcodeLabelRepository _historyRepo;
        private readonly ProductService _productService;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<string>? StatusMessage;
        /// <summary>Raised when a scanned barcode matches a product — code-behind opens QuickPrintDialog.</summary>
        public event Action<Product>? OpenQuickPrintRequested;

        public ObservableCollection<BarcodeProductRow> Products { get; } = new();
        public ICollectionView FilteredProducts { get; }
        public ObservableCollection<LabelPrintJob> PrintJobs { get; } = new();

        public ObservableCollection<LabelTemplate> Templates { get; } = new();
        public Array AvailableFormats { get; } = Enum.GetValues(typeof(BarcodeFormat));
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<string> Printers { get; } = new();
        public ObservableCollection<BarcodeLabelHistoryRow> History { get; } = new();

        private LabelTemplate? selectedTemplate;
        public LabelTemplate? SelectedTemplate
        {
            get => selectedTemplate;
            set { if (SetProperty(ref selectedTemplate, value)) RefreshPreview(); }
        }

        private BarcodeFormat selectedFormat = BarcodeFormat.Code128;
        public BarcodeFormat SelectedFormat
        {
            get => selectedFormat;
            set
            {
                if (SetProperty(ref selectedFormat, value))
                {
                    foreach (var job in PrintJobs)
                        job.Format = value;
                    RefreshPreview();
                }
            }
        }

        private string searchText = string.Empty;
        public string SearchText
        {
            get => searchText;
            set { if (SetProperty(ref searchText, value)) FilteredProducts.Refresh(); }
        }

        private string selectedCategory = AllCategoriesLabel;
        public string SelectedCategory
        {
            get => selectedCategory;
            set { if (SetProperty(ref selectedCategory, value)) FilteredProducts.Refresh(); }
        }

        private string? selectedPrinter;
        public string? SelectedPrinter
        {
            get => selectedPrinter;
            set => SetProperty(ref selectedPrinter, value);
        }

        private BitmapImage? previewImage;
        public BitmapImage? PreviewImage
        {
            get => previewImage;
            private set => SetProperty(ref previewImage, value);
        }

        private LabelPrintJob? previewJob;
        public LabelPrintJob? PreviewJob
        {
            get => previewJob;
            private set => SetProperty(ref previewJob, value);
        }

        public RelayCommand SelectAllCommand { get; }
        public RelayCommand SelectByCategoryCommand { get; }
        public RelayCommand<LabelPrintJob> RemoveJobCommand { get; }
        public RelayCommand PrintCommand { get; }

        public BarcodesPageViewModel(
            PosStateStore stateStore,
            BarcodeService barcodeService,
            LabelPrintService printService,
            LabelTemplateRepository templateRepo,
            CategoryService categoryService,
            BarcodeLabelRepository historyRepo,
            ProductService productService)
        {
            _stateStore = stateStore;
            _barcodeService = barcodeService;
            _printService = printService;
            _templateRepo = templateRepo;
            _categoryService = categoryService;
            _historyRepo = historyRepo;
            _productService = productService;

            FilteredProducts = CollectionViewSource.GetDefaultView(Products);
            FilteredProducts.Filter = FilterProduct;

            SelectAllCommand = new RelayCommand(_ => SelectAll());
            SelectByCategoryCommand = new RelayCommand(_ => SelectByCategory());
            RemoveJobCommand = new RelayCommand<LabelPrintJob>(RemoveJob);
            PrintCommand = new RelayCommand(_ => Print(), _ => PrintJobs.Count > 0 && SelectedTemplate != null);

            PrintJobs.CollectionChanged += PrintJobs_CollectionChanged;
            _stateStore.Products.CollectionChanged += StateStoreProducts_CollectionChanged;

            LoadTemplates();
            LoadCategories();
            LoadPrinters();
            BuildProductRows();
            LoadHistory();
        }

        /// <summary>Reloads the template list from disk — call after the template editor dialog closes.</summary>
        public void ReloadTemplates() => LoadTemplates();

        private void LoadTemplates()
        {
            Templates.Clear();
            foreach (var t in _templateRepo.GetAll())
                Templates.Add(t);

            var stillSelected = selectedTemplate != null && Templates.Any(t => t.Id == selectedTemplate.Id);
            if (!stillSelected)
            {
                selectedTemplate = Templates.FirstOrDefault(t => t.IsDefault) ?? Templates.FirstOrDefault();
                OnPropertyChanged(nameof(SelectedTemplate));
            }
        }

        private void LoadHistory()
        {
            History.Clear();
            foreach (var record in _historyRepo.GetRecent(100))
            {
                var productName = Products.FirstOrDefault(p => p.Id == record.ProductId)?.Name
                    ?? _stateStore.Products.FirstOrDefault(p => p.Id == record.ProductId)?.Name
                    ?? $"Product #{record.ProductId}";

                History.Add(new BarcodeLabelHistoryRow
                {
                    ProductName = productName,
                    Quantity = record.Quantity,
                    TemplateName = record.TemplateName,
                    Format = record.BarcodeFormat,
                    PrintedAt = record.PrintedAt
                });
            }
        }

        private void LogPrintJobs(IEnumerable<LabelPrintJob> jobs, string templateName)
        {
            foreach (var job in jobs)
            {
                try
                {
                    _historyRepo.Insert(new BarcodeLabelRecord
                    {
                        ProductId = job.ProductId,
                        PrintedAt = DateTime.Now,
                        Quantity = Math.Max(1, job.Quantity),
                        TemplateName = templateName,
                        BarcodeFormat = job.Format
                    });
                }
                catch
                {
                    // History logging must never block a print/export that already succeeded
                }
            }

            LoadHistory();
        }

        /// <summary>Looks up a scanned barcode and asks the view to open QuickPrintDialog for it (damaged-label replacement).</summary>
        public void HandleBarcodeScanned(string barcode)
        {
            var record = _stateStore.Products.FirstOrDefault(p => string.Equals(p.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
            if (record == null)
            {
                StatusMessage?.Invoke($"Barcode not registered: {barcode}");
                return;
            }

            var product = new Product
            {
                Id = record.Id,
                Barcode = record.Barcode,
                Name = record.Name,
                Category = record.Category,
                Price = record.Price,
                CostPrice = record.CostPrice,
                Stock = record.Stock,
                ReorderLevel = record.ReorderLevel,
                MaxStock = record.MaxStock,
                LastUpdated = record.LastUpdated,
                UnitType = record.UnitType,
                ConversionRate = record.ConversionRate,
                ParentProductId = record.ParentProductId,
                BarcodeFormat = record.BarcodeFormat
            };

            OpenQuickPrintRequested?.Invoke(product);
        }

        /// <summary>Builds a QuickPrintDialogViewModel sharing this page's services — used for the damaged-label flow.</summary>
        public QuickPrintDialogViewModel CreateQuickPrintViewModel(Product product)
        {
            return new QuickPrintDialogViewModel(product, _productService, _printService, _templateRepo, _historyRepo);
        }

        private void LoadCategories()
        {
            Categories.Clear();
            Categories.Add(AllCategoriesLabel);
            foreach (var c in _categoryService.GetAll())
                Categories.Add(c);
        }

        private void LoadPrinters()
        {
            Printers.Clear();
            foreach (var p in _printService.GetInstalledPrinters())
                Printers.Add(p);

            selectedPrinter = Printers.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedPrinter));
        }

        private void BuildProductRows()
        {
            foreach (var row in Products)
                row.PropertyChanged -= Row_PropertyChanged;
            Products.Clear();

            foreach (var p in _stateStore.Products)
            {
                var row = new BarcodeProductRow
                {
                    Id = p.Id,
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Category = p.Category,
                    Price = p.Price,
                    Format = p.BarcodeFormat,
                    IsSelected = PrintJobs.Any(j => j.ProductId == p.Id)
                };
                row.PropertyChanged += Row_PropertyChanged;
                Products.Add(row);
            }
        }

        private void StateStoreProducts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            BuildProductRows();
        }

        private bool FilterProduct(object obj)
        {
            if (obj is not BarcodeProductRow row)
                return false;

            var matchesSearch = string.IsNullOrWhiteSpace(SearchText)
                || row.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || row.Barcode.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

            var matchesCategory = SelectedCategory == AllCategoriesLabel || row.Category == SelectedCategory;

            return matchesSearch && matchesCategory;
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(BarcodeProductRow.IsSelected) || sender is not BarcodeProductRow row)
                return;

            if (row.IsSelected)
            {
                if (!PrintJobs.Any(j => j.ProductId == row.Id))
                {
                    PrintJobs.Add(new LabelPrintJob
                    {
                        ProductId = row.Id,
                        ProductName = row.Name,
                        Barcode = row.Barcode,
                        Category = row.Category,
                        Price = row.Price,
                        Format = SelectedFormat,
                        Quantity = 1
                    });
                }
            }
            else
            {
                var existing = PrintJobs.FirstOrDefault(j => j.ProductId == row.Id);
                if (existing != null)
                    PrintJobs.Remove(existing);
            }
        }

        private void SelectAll()
        {
            foreach (var row in FilteredProducts.Cast<BarcodeProductRow>().ToList())
                row.IsSelected = true;
        }

        private void SelectByCategory()
        {
            if (SelectedCategory == AllCategoriesLabel)
                return;

            foreach (var row in Products.Where(r => r.Category == SelectedCategory).ToList())
                row.IsSelected = true;
        }

        private void RemoveJob(LabelPrintJob? job)
        {
            if (job is null)
                return;

            var row = Products.FirstOrDefault(r => r.Id == job.ProductId);
            if (row != null)
                row.IsSelected = false;
            else
                PrintJobs.Remove(job);
        }

        private void PrintJobs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            PrintCommand.RaiseCanExecuteChanged();
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            var job = PrintJobs.FirstOrDefault();
            PreviewJob = job;

            if (job == null || SelectedTemplate == null)
            {
                PreviewImage = null;
                return;
            }

            try
            {
                PreviewImage = _barcodeService.GenerateImage(job.Barcode, SelectedFormat, 300, 120);
            }
            catch
            {
                PreviewImage = null;
            }
        }

        private void Print()
        {
            var template = SelectedTemplate;
            if (PrintJobs.Count == 0 || template == null)
                return;

            try
            {
                var jobs = PrintJobs.ToList();
                WarnAboutInvalidEan13(jobs);
                _printService.PrintLabels(jobs, template, SelectedPrinter);
                LogPrintJobs(jobs, template.Name);
                StatusMessage?.Invoke($"Sent {jobs.Sum(j => Math.Max(1, j.Quantity))} label(s) to print.");
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke($"Print failed: {ex.Message}");
            }
        }

        /// <summary>EAN-13 requires exactly 13 digits with a valid check digit — warn (non-blocking) when a job's barcode doesn't qualify.</summary>
        private void WarnAboutInvalidEan13(List<LabelPrintJob> jobs)
        {
            var invalidNames = jobs
                .Where(j => j.Format == BarcodeFormat.EAN13 && !_barcodeService.ValidateEan13(j.Barcode))
                .Select(j => j.ProductName)
                .Distinct()
                .ToList();

            if (invalidNames.Count > 0)
            {
                StatusMessage?.Invoke(
                    $"Warning: not a valid 13-digit EAN-13 barcode for: {string.Join(", ", invalidNames)}. The label may not scan correctly.");
            }
        }

        /// <summary>Exports the current print jobs to a PDF file at the given path (chosen by the view via SaveFileDialog).</summary>
        public void ExportPdf(string filePath)
        {
            var template = SelectedTemplate;
            if (PrintJobs.Count == 0 || template == null)
                return;

            try
            {
                var jobs = PrintJobs.ToList();
                WarnAboutInvalidEan13(jobs);
                _printService.ExportToPdf(jobs, template, filePath);
                LogPrintJobs(jobs, template.Name);
                StatusMessage?.Invoke($"Exported {jobs.Sum(j => Math.Max(1, j.Quantity))} label(s) to PDF.");
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke($"PDF export failed: {ex.Message}");
            }
        }

        /// <summary>Builds the print-ready FixedDocument for the preview window, or null if there is nothing to preview.</summary>
        public FixedDocument? BuildPreviewDocument()
        {
            var template = SelectedTemplate;
            if (PrintJobs.Count == 0 || template == null)
                return null;

            return _printService.BuildFixedDocument(PrintJobs.ToList(), template);
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
