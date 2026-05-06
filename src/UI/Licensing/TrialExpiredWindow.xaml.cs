using System.Windows;
using System.Windows.Input;
using EZPos.Core.Licensing;

namespace EZPos.UI.Licensing
{
    /// <summary>
    /// Shown when the trial period has ended.
    ///
    /// Displays the expiry date, a brief purchase prompt, and a single close button.
    /// The window is modal: after Close is clicked (or the window is dismissed),
    /// App.xaml.cs shuts down the application.
    ///
    /// This window is intentionally read-only — it has no activation/key-entry UI.
    /// When real licensing is introduced, remove the call to this window in App.xaml.cs
    /// and route the Expired status through the full LicenseRequiredWindow instead.
    /// </summary>
    public partial class TrialExpiredWindow : Window
    {
        public TrialExpiredWindow(LicenseInfo licenseInfo)
        {
            InitializeComponent();

            // Populate the expiry date label from the license metadata.
            if (licenseInfo.ExpiryDate.HasValue)
            {
                var local = licenseInfo.ExpiryDate.Value.ToLocalTime();
                ExpiryDateText.Text = $"Trial expired on {local:dddd, d MMMM yyyy}";
            }
            else
            {
                ExpiryDateText.Text = "Trial period has ended.";
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
