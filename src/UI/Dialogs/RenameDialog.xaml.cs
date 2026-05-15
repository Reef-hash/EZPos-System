using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace EZPos.UI.Dialogs
{
    public partial class RenameDialog : UserControl
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
            if (e.Key == Key.Escape) DialogHost.CloseDialogCommand.Execute(null, this);
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)     => TryCommit();
        private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogHost.CloseDialogCommand.Execute(null, this);

        private void TryCommit()
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            NewName = name;
            DialogHost.CloseDialogCommand.Execute(name, this);
        }
    }
}
