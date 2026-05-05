using System.Windows;
using System.Windows.Input;
using EZPos.Business.Services;

namespace EZPos.UI.Dialogs
{
    public partial class CategoryManagementDialog : Window
    {
        private readonly CategoryService _categoryService;

        public CategoryManagementDialog(CategoryService categoryService)
        {
            _categoryService = categoryService;
            InitializeComponent();
            Loaded += (_, _) => { RefreshList(); NewCategoryBox.Focus(); };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RefreshList()
        {
            var selected = CategoryList.SelectedItem as string;
            CategoryList.Items.Clear();
            foreach (var cat in _categoryService.GetAll())
                CategoryList.Items.Add(cat);

            // Re-select same item if it still exists
            if (selected != null)
            {
                foreach (var item in CategoryList.Items)
                    if (item as string == selected) { CategoryList.SelectedItem = item; break; }
            }
            UpdateFooter();
        }

        private void UpdateFooter()
        {
            int count = CategoryList.Items.Count;
            FooterText.Text = $"{count} categor{(count == 1 ? "y" : "ies")} total. 'General' cannot be deleted.";
        }

        private void ShowStatus(string message, bool isError = true)
        {
            StatusText.Text       = message;
            StatusText.Foreground = isError
                ? (System.Windows.Media.Brush)FindResource("ErrorBrush")
                : (System.Windows.Media.Brush)FindResource("SuccessBrush");
            StatusText.Visibility = Visibility.Visible;
        }

        private void ClearStatus() => StatusText.Visibility = Visibility.Collapsed;

        // ── Event handlers ────────────────────────────────────────────────────

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void CategoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selected = CategoryList.SelectedItem as string;
            bool hasSelection = selected != null;
            bool isGeneral    = selected == "General";

            RenameBtn.IsEnabled = hasSelection;
            DeleteBtn.IsEnabled = hasSelection && !isGeneral;
            ClearStatus();
        }

        private void NewCategoryBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryAdd();
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e) => TryAdd();

        private void TryAdd()
        {
            var name = NewCategoryBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { ShowStatus("Please enter a category name."); return; }

            bool ok = _categoryService.Add(name);
            if (ok)
            {
                NewCategoryBox.Clear();
                RefreshList();
                // Select the newly added item
                foreach (var item in CategoryList.Items)
                    if (item as string == name) { CategoryList.SelectedItem = item; break; }
                ShowStatus($"'{name}' added.", isError: false);
            }
            else
            {
                ShowStatus($"'{name}' already exists.");
            }
        }

        private void RenameBtn_Click(object sender, RoutedEventArgs e)
        {
            var selected = CategoryList.SelectedItem as string;
            if (selected == null) return;

            var dialog = new RenameDialog(selected) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            var newName = dialog.NewName;
            if (newName == selected) return;

            bool ok = _categoryService.Rename(selected, newName);
            if (ok)
            {
                RefreshList();
                foreach (var item in CategoryList.Items)
                    if (item as string == newName) { CategoryList.SelectedItem = item; break; }
                ShowStatus($"Renamed to '{newName}'.", isError: false);
            }
            else
            {
                ShowStatus($"Could not rename — '{newName}' may already exist.");
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var selected = CategoryList.SelectedItem as string;
            if (selected == null || selected == "General") return;

            int count = _categoryService.GetProductCount(selected);
            var msg = count > 0
                ? $"Delete '{selected}'?\n\n{count} product(s) will be moved to 'General'."
                : $"Delete '{selected}'?";

            var result = MessageBox.Show(msg, "Delete Category",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            _categoryService.Delete(selected);
            RefreshList();
            ShowStatus($"'{selected}' deleted.", isError: false);
        }
    }
}
