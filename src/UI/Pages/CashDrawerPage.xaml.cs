using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using EZPos.Business.Services;
using EZPos.Models.Domain;
using MaterialDesignThemes.Wpf;

namespace EZPos.UI.Pages
{
    // ── View model wrapping CashSession for the DataGrid ─────────────────────

    internal sealed class CashSessionRow
    {
        private readonly CashSession _s;
        public CashSessionRow(CashSession s) { _s = s; }

        public int     Id                    => _s.Id;
        public string  Status                => _s.Status;
        public bool    IsOpen                => _s.Status == "Open";
        public string  OpenedByUser          => _s.OpenedByUser;
        public DateTime OpenedAtRaw          => _s.OpenedAt;
        public string  OpenedAtDisplay       => _s.OpenedAt.ToString("dd MMM yyyy  HH:mm");
        public string? ClosedByUser          => _s.ClosedByUser;
        public string  ClosedAtDisplay       => _s.ClosedAt.HasValue
                                                    ? _s.ClosedAt.Value.ToString("dd MMM yyyy  HH:mm")
                                                    : "—";
        public string  OpeningBalanceDisplay => $"RM {_s.OpeningBalance:N2}";
        public string  ExpectedCashDisplay   => _s.Status == "Open" ? "Pending..." : $"RM {_s.ExpectedCash:N2}";
        public string  ActualCashDisplay     => _s.ActualCash.HasValue
                                                    ? $"RM {_s.ActualCash.Value:N2}"
                                                    : "—";
        public string  DiscrepancyDisplay
        {
            get
            {
                if (!_s.DiscrepancyAmount.HasValue) return "—";
                var d = _s.DiscrepancyAmount.Value;
                if (d == 0)  return "Balanced";
                if (d > 0)   return $"OVER  RM {d:N2}";
                return               $"SHORT RM {Math.Abs(d):N2}";
            }
        }
        public string? Notes => _s.Notes;

        // Grouping support
        public string DateGroup
        {
            get
            {
                var d = _s.OpenedAt.Date;
                if (d == DateTime.Today)             return "TODAY";
                if (d == DateTime.Today.AddDays(-1)) return "YESTERDAY";
                return "EARLIER";
            }
        }
        public int DateGroupOrder => DateGroup == "TODAY" ? 0 : DateGroup == "YESTERDAY" ? 1 : 2;
    }

    // ── Page ─────────────────────────────────────────────────────────────────

    public partial class CashDrawerPage : UserControl
    {
        private readonly CashDrawerService _service = new();
        private CashSession? _activeSession;

        public CashDrawerPage()
        {
            InitializeComponent();
            Loaded += CashDrawerPage_Loaded;
        }

        private void CashDrawerPage_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAll();
        }

        // ── Refresh helpers ───────────────────────────────────────────────────

        private void RefreshAll()
        {
            _activeSession = _service.GetActiveSession();
            RefreshStatusPanel();
            RefreshHistory();
        }

        private void RefreshStatusPanel()
        {
            var isOpen = _activeSession != null;

            OpenShiftBtn.IsEnabled  = !isOpen;
            CloseShiftBtn.IsEnabled = isOpen;

            if (isOpen && _activeSession != null)
            {
                StatusIcon.Foreground     = new SolidColorBrush(Color.FromRgb(52, 211, 153));
                StatusLabel.Text          = $"Shift #{_activeSession.Id} is OPEN";
                ActiveSessionDetails.Visibility = Visibility.Visible;

                ActiveShiftId.Text   = _activeSession.Id.ToString();
                ActiveOpenedBy.Text  = _activeSession.OpenedByUser;
                ActiveOpenedAt.Text  = _activeSession.OpenedAt.ToString("dd MMM yyyy  HH:mm");

                var expected = _service.PreviewExpectedCash() ?? _activeSession.OpeningBalance;
                ActiveExpectedCash.Text = $"RM {expected:N2}";
            }
            else
            {
                StatusIcon.Foreground     = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                StatusLabel.Text          = "No shift is currently open.";
                ActiveSessionDetails.Visibility = Visibility.Collapsed;
            }
        }

        private void RefreshHistory()
        {
            var sessions = _service.GetSessionHistory(100);
            var rows = sessions
                .Select(s => new CashSessionRow(s))
                .OrderBy(r => r.DateGroupOrder)
                .ThenByDescending(r => r.OpenedAtRaw)
                .ToList();

            var view = CollectionViewSource.GetDefaultView(rows);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(CashSessionRow.DateGroup)));
            HistoryGrid.ItemsSource = view;
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private async void OpenShift_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.OpenShiftDialog();
            await DialogHost.Show(dialog, "RootDialog");

            if (!dialog.Confirmed) return;

            var result = _service.OpenShift(dialog.CashierName, dialog.OpeningBalance);
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "Cannot Open Shift",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RefreshAll();
        }

        private async void CloseShift_Click(object sender, RoutedEventArgs e)
        {
            if (_activeSession == null)
            {
                MessageBox.Show("No active shift to close.", "Close Shift",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var expectedCash = _service.PreviewExpectedCash() ?? _activeSession.OpeningBalance;
            var breakdown    = _service.GetShiftPaymentBreakdown();
            var dialog = new Dialogs.CloseShiftDialog(_activeSession, expectedCash, breakdown);
            await DialogHost.Show(dialog, "RootDialog");

            if (!dialog.Confirmed) return;

            var result = _service.CloseShift(_activeSession.Id, dialog.ClosedByUser,
                                              dialog.ActualCash, dialog.Notes);
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "Cannot Close Shift",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Show reconciliation summary
            var msg = BuildSummaryMessage(result);
            MessageBox.Show(msg, "Shift Closed", MessageBoxButton.OK, MessageBoxImage.Information);

            RefreshAll();
        }

        private static string BuildSummaryMessage(CloseShiftResult r)
        {
            return $"Shift closed successfully.\n\n" +
                   $"  Expected Cash :  RM {r.ExpectedCash:N2}\n" +
                   $"  Actual Cash   :  RM {r.ActualCash:N2}\n" +
                   $"  ─────────────────────────────\n" +
                   $"  Result        :  {r.DiscrepancyLabel}";
        }
    }
}
