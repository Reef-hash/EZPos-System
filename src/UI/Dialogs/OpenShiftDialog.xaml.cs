using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace EZPos.UI.Dialogs
{
    public partial class OpenShiftDialog : UserControl
    {
        public string CashierName      { get; private set; } = string.Empty;
        public decimal OpeningBalance  { get; private set; }
        public bool Confirmed          { get; private set; }

        public OpenShiftDialog()
        {
            InitializeComponent();
            Loaded += (_, _) => CashierNameBox.Focus();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var name = CashierNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("Please enter the cashier name.");
                return;
            }

            if (!decimal.TryParse(OpeningBalanceBox.Text, out var balance) || balance < 0)
            {
                ShowError("Opening balance must be a valid non-negative number.");
                return;
            }

            CashierName     = name;
            OpeningBalance  = balance;
            Confirmed       = true;
            DialogHost.CloseDialogCommand.Execute(null, this);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogHost.CloseDialogCommand.Execute(null, this);
        }

        private void ShowError(string message)
        {
            ErrorText.Text       = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
