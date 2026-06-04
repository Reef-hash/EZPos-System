using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.Threading.Tasks;
using EZPos.Core.Licensing;
using EZPos.Infrastructure.Licensing;

namespace EZPos.UI.Licensing
{
    public partial class LicenseRequiredWindow : Window
    {
        private readonly ILicenseService _licenseService;
        private readonly LicenseApiClient? _apiClient;
        private CancellationTokenSource? _wakeCts;

        public LicenseRequiredWindow(ILicenseService licenseService, LicenseApiClient? apiClient = null)
        {
            _licenseService = licenseService;
            _apiClient = apiClient;
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
                _ = TryActivateAsync();
        }

        private void ActivateBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = TryActivateAsync();
        }

        // ── Core logic ────────────────────────────────────────────────────

        private async Task TryActivateAsync()
        {
            var key = LicenseKeyBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                ShowError("Please enter a license key.");
                return;
            }

            ActivateBtn.IsEnabled   = false;
            LicenseKeyBox.IsEnabled = false;
            StatusText.Visibility   = System.Windows.Visibility.Collapsed;

            // Wake up the backend if an API client was provided
            if (_apiClient is not null)
            {
                _wakeCts = new CancellationTokenSource();
                WakePanel.Visibility = System.Windows.Visibility.Visible;
                WakeStatusText.Text  = "Connecting to server...";

                var awake = await _apiClient.WakeUpAsync(
                    elapsed =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            WakeStatusText.Text = elapsed < 10
                                ? "Connecting to server..."
                                : $"Starting server... ({elapsed}s)";
                        });
                    },
                    _wakeCts.Token);

                WakePanel.Visibility = System.Windows.Visibility.Collapsed;

                if (!awake)
                {
                    ActivateBtn.IsEnabled   = true;
                    LicenseKeyBox.IsEnabled = true;
                    ShowError("Cannot reach license server. Please check your internet connection and try again.");
                    return;
                }
            }

            var info = _licenseService.Activate(key);

            if (info.IsLicensed)
            {
                ShowSuccessPanel(info);
            }
            else
            {
                ActivateBtn.IsEnabled   = true;
                LicenseKeyBox.IsEnabled = true;

                var message = info.Status switch
                {
                    LicenseStatus.Expired         => "This license is no longer valid. Please contact support.",
                    LicenseStatus.Revoked         => "This license has been revoked. Please contact support.",
                    LicenseStatus.ProductMismatch => "This key is for a different product. Please use an EZPos license key.",
                    LicenseStatus.DeviceMismatch  => "This key is already bound to another device. Request a transfer from support.",
                    LicenseStatus.SeatExceeded    => "Seat/device limit reached. Please contact support.",
                    LicenseStatus.Invalid         => "Invalid license key. Please check the key and try again.",
                    LicenseStatus.NotActivated    => "Key found but not activated. Please contact support.",
                    _                             => "Activation failed. Please try again or contact support."
                };
                if (!string.IsNullOrWhiteSpace(info.ApiMessage))
                    message = info.ApiMessage;
                ShowError(message);
            }
        }

        private void ShowSuccessPanel(LicenseInfo info)
        {
            ActivationForm.Visibility = System.Windows.Visibility.Collapsed;

            SuccessPlanText.Text   = string.IsNullOrWhiteSpace(info.PlanName) ? "Standard" : info.PlanName;
            SuccessExpiryText.Text = "Lifetime (One-Time Purchase)";
            SuccessCustomerText.Text = string.IsNullOrWhiteSpace(info.ApiMessage) ? "—" : info.ApiMessage;

            SuccessPanel.Visibility = System.Windows.Visibility.Visible;
        }

        private void ContinueBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ShowError(string message)
        {
            StatusText.Text       = message;
            StatusText.Visibility = System.Windows.Visibility.Visible;
        }
    }
}
