using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using EZPos.Business.Services;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;
using EZPos.UI.State;

namespace EZPos.UI.ViewModels
{
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

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<string>? StatusMessage;

        public ObservableCollection<BarcodeProductRow> Products { get; } = new();
        public ICollectionView FilteredProducts { get; }
        public ObservableCollection<LabelPrintJob> PrintJobs { get; } = new();

        public ObservableCollection<LabelTemplate> Templates { get; } = new();
        public Array AvailableFormats { get; } = Enum.GetValues(typeof(BarcodeFormat));
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<string> Printers { get; } = new();

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
            CategoryService categoryService)
        {
            _stateStore = stateStore;
            _barcodeService = barcodeService;
            _printService = printService;
            _templateRepo = templateRepo;
            _categoryService = categoryService;

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
        }

        private void LoadTemplates()
        {
            Templates.Clear();
            foreach (var t in _templateRepo.GetAll())
                Templates.Add(t);

            selectedTemplate = Templates.FirstOrDefault(t => t.IsDefault) ?? Templates.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedTemplate));
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
                _printService.PrintLabels(PrintJobs.ToList(), template, SelectedPrinter);
                StatusMessage?.Invoke($"Sent {PrintJobs.Sum(j => Math.Max(1, j.Quantity))} label(s) to print.");
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
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
