using System.Windows;
using System.Windows.Input;

namespace EZPos.UI.Dialogs
{
    public partial class RenameDialog : Window
    {
        public string NewName { get; private set; } = string.Empty;

        public RenameDialog(string currentName)
        {
            InitializeComponent();
            PromptText.Text = $"New name for '{currentName}':";
            NameBox.Text    = currentName;
            Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
        }

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)  TryCommit();
            if (e.Key == Key.Escape) Close();
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)     => TryCommit();
        private void CancelBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void TryCommit()
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            NewName      = name;
            DialogResult = true;
            Close();
        }
    }
}
