using System.Windows;
using System.Windows.Input;
using EZPos.Core.Licensing;

namespace EZPos.UI.Licensing
{
    /// <summary>
    /// Blocking activation window shown when the license check on startup returns
    /// anything other than LicenseStatus.Valid.
    ///
    /// Flow:
    ///   1. User types a license key and clicks Activate.
    ///   2. LicenseService.Activate(key) is called (currently mock — any key passes).
    ///   3. On success, DialogResult = true → App.xaml.cs continues startup.
    ///   4. On failure, an error message is shown inline.
    ///
    /// TODO (when real API is ready):
    ///   - Show a loading spinner during the async API call.
    ///   - Handle network timeout / offline gracefully with a retry option.
    ///   - Display plan name and expiry date from LicenseInfo after activation.
    /// </summary>
    public partial class LicenseRequiredWindow : Window
    {
        private readonly ILicenseService _licenseService;

        public LicenseRequiredWindow(ILicenseService licenseService)
        {
            _licenseService = licenseService;
            InitializeComponent();
            Loaded += (_, _) => LicenseKeyBox.Focus();
        }

        // ── Event handlers ────────────────────────────────────────────────

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void LicenseKeyBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                TryActivate();
        }

        private void ActivateBtn_Click(object sender, RoutedEventArgs e)
        {
            TryActivate();
        }

        // ── Core logic ────────────────────────────────────────────────────

        private void TryActivate()
        {
            var key = LicenseKeyBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                ShowError("Please enter a license key.");
                return;
            }

            // Disable controls while processing (future: show spinner here)
            ActivateBtn.IsEnabled    = false;
            LicenseKeyBox.IsEnabled  = false;
            StatusText.Visibility    = Visibility.Collapsed;

            // TODO: make this async when LicenseService.ActivateAsync() is introduced:
            //   var info = await _licenseService.ActivateAsync(key);
            var info = _licenseService.Activate(key);

            if (info.IsLicensed)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                // Re-enable controls so the user can correct their key
                ActivateBtn.IsEnabled   = true;
                LicenseKeyBox.IsEnabled = true;

                var message = info.Status switch
                {
                    LicenseStatus.Expired     => "This license key has expired. Please renew your subscription.",
                    LicenseStatus.Invalid     => "Invalid license key. Please check the key and try again.",
                    LicenseStatus.NotActivated => "Key found but not activated. Please contact support.",
                    _                          => "Activation failed. Please try again or contact support."
                };
                ShowError(message);
            }
        }

        private void ShowError(string message)
        {
            StatusText.Text       = message;
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
