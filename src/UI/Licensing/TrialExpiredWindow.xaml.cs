using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EZPos.Core.Licensing;
using EZPos.Infrastructure.Licensing;

namespace EZPos.UI.Licensing
{
    /// <summary>
    /// Shown when the trial period has ended, the license was rejected outright,
    /// or the offline grace period has lapsed.
    ///
    /// Offers a manual "Check Connection & Retry" button: a sleeping free-tier
    /// backend can take longer to wake than the bounded retry already attempted
    /// during startup, and it sometimes needs to be pinged more than once. Retry
    /// re-runs the same wake-up-then-validate flow with visible progress so the
    /// user isn't stuck waiting on a silent freeze. If the retry confirms the
    /// license is valid, DialogResult is set true and App.xaml.cs continues
    /// startup instead of shutting down.
    /// </summary>
    public partial class TrialExpiredWindow : Window
    {
        private readonly ILicenseService? _licenseService;
        private readonly LicenseApiClient? _apiClient;

        public TrialExpiredWindow(LicenseInfo licenseInfo, ILicenseService? licenseService = null, LicenseApiClient? apiClient = null)
        {
            InitializeComponent();

            _licenseService = licenseService;
            _apiClient      = apiClient;

            RetryBtn.Visibility = licenseService is not null && apiClient is not null
                ? Visibility.Visible
                : Visibility.Collapsed;

            ApplyInfo(licenseInfo);
        }

        private void ApplyInfo(LicenseInfo licenseInfo)
        {
            bool isGraceExpired = licenseInfo.Status == LicenseStatus.Expired
                && string.IsNullOrWhiteSpace(licenseInfo.ReasonCode) == false
                    ? licenseInfo.ReasonCode.Contains("GRACE", System.StringComparison.OrdinalIgnoreCase)
                    : licenseInfo.ApiMessage?.Contains("grace", System.StringComparison.OrdinalIgnoreCase) == true;

            if (isGraceExpired)
            {
                TitleBarText.Text     = "EZPos — Offline Grace Expired";
                HeadingText.Text      = "Offline Grace Period Expired";
                ExpiryDateText.Text   = "Could not reach license server. Grace period has ended.";
                BodyText.Text         = "EZPos could not contact the license server and the offline grace period has expired. Check your internet connection, then press Retry — or contact support if this keeps happening.";
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

        private async void RetryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_apiClient is null || _licenseService is null)
                return;

            RetryBtn.IsEnabled  = false;
            CloseBtn.IsEnabled  = false;
            RetryResultText.Visibility = Visibility.Collapsed;
            RetryPanel.Visibility = Visibility.Visible;
            RetryStatusText.Text = "Connecting to server...";

            using var wakeCts = new CancellationTokenSource(TimeSpan.FromSeconds(75));
            var awake = await _apiClient.WakeUpAsync(
                elapsed =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        RetryStatusText.Text = elapsed < 10
                            ? "Connecting to server..."
                            : $"Server is waking up, please wait... ({elapsed}s)";
                    });
                },
                wakeCts.Token);

            if (!awake)
            {
                RetryPanel.Visibility = Visibility.Collapsed;
                ShowRetryResult("You're not connected to the internet, or the server could not be reached. Please check your connection and try again.", isError: true);
                RetryBtn.IsEnabled = true;
                CloseBtn.IsEnabled = true;
                return;
            }

            RetryStatusText.Text = "Validating license...";
            var info = await Task.Run(() => _licenseService.LoadAndValidate());
            RetryPanel.Visibility = Visibility.Collapsed;

            if (info.IsLicensed)
            {
                ShowRetryResult("Valid! Starting EZPos...", isError: false);
                await Task.Delay(700);
                DialogResult = true;
                Close();
                return;
            }

            ApplyInfo(info);
            var message = info.ApiMessage;
            if (string.IsNullOrWhiteSpace(message))
                message = "Invalid — license could not be verified. Please contact support.";
            ShowRetryResult(message, isError: true);

            RetryBtn.IsEnabled = true;
            CloseBtn.IsEnabled = true;
        }

        private void ShowRetryResult(string message, bool isError)
        {
            RetryResultText.Text = message;
            RetryResultText.Foreground = (System.Windows.Media.Brush)FindResource(isError ? "ErrorBrush" : "SuccessBrush");
            RetryResultText.Visibility = Visibility.Visible;
        }
    }
}
