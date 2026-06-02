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

            bool isGraceExpired = licenseInfo.Status == LicenseStatus.Expired
                && string.IsNullOrWhiteSpace(licenseInfo.ReasonCode) == false
                    ? licenseInfo.ReasonCode.Contains("GRACE", System.StringComparison.OrdinalIgnoreCase)
                    : licenseInfo.ApiMessage?.Contains("grace", System.StringComparison.OrdinalIgnoreCase) == true;

            if (isGraceExpired)
            {
                TitleBarText.Text     = "EZPos — Offline Grace Expired";
                HeadingText.Text      = "Offline Grace Period Expired";
                ExpiryDateText.Text   = "Could not reach license server. Grace period has ended.";
                BodyText.Text         = "EZPos could not contact the license server and the offline grace period has expired. Please connect to the internet and restart the application.";
            }
            else if (licenseInfo.ExpiryDate.HasValue)
            {
                var local = licenseInfo.ExpiryDate.Value.ToLocalTime();
                ExpiryDateText.Text = $"License expired on {local:dddd, d MMMM yyyy}";
            }
            else
            {
                ExpiryDateText.Text = licenseInfo.ApiMessage ?? "License period has ended.";
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
