using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FontAwesome.Sharp;
using MaterialDesignThemes.Wpf;
using EZPos.Models.Domain;

namespace EZPos.UI.Dialogs
{
    public partial class CloseShiftDialog : UserControl
    {
        private readonly CashSession _session;
        private readonly decimal     _expectedCash;

        public string  ClosedByUser  { get; private set; } = string.Empty;
        public decimal ActualCash    { get; private set; }
        public string? Notes         { get; private set; }
        public bool    Confirmed     { get; private set; }

        public CloseShiftDialog(CashSession session, decimal expectedCash,
                                List<(string Method, decimal Revenue)> breakdown)
        {
            InitializeComponent();

            _session      = session;
            _expectedCash = expectedCash;

            // Populate session summary
            OpenedByText.Text       = session.OpenedByUser;
            OpenedAtText.Text       = session.OpenedAt.ToString("dd MMM yyyy  HH:mm");
            OpeningBalanceText.Text = $"RM {session.OpeningBalance:N2}";
            ExpectedCashText.Text   = $"RM {expectedCash:N2}";
            ClosedByBox.Text        = session.OpenedByUser; // pre-fill with same cashier

            PopulatePaymentBreakdown(breakdown);

            Loaded += (_, _) => ActualCashBox.Focus();
        }

        private void PopulatePaymentBreakdown(List<(string Method, decimal Revenue)> breakdown)
        {
            var mutedBrush   = (Brush)FindResource("DashboardTextMutedBrush");
            var primaryBrush = (Brush)FindResource("DashboardTextPrimaryBrush");

            decimal total = 0;
            foreach (var (method, revenue) in breakdown)
            {
                total += revenue;
                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var methodText  = new TextBlock { Text = method,                Foreground = mutedBrush,   FontSize = 12 };
                var revenueText = new TextBlock { Text = $"RM {revenue:N2}",    Foreground = primaryBrush, FontSize = 12,
                                                  HorizontalAlignment = HorizontalAlignment.Right };
                Grid.SetColumn(revenueText, 1);
                row.Children.Add(methodText);
                row.Children.Add(revenueText);
                PaymentBreakdownPanel.Children.Add(row);
            }

            if (breakdown.Count == 0)
            {
                PaymentBreakdownPanel.Children.Add(new TextBlock
                {
                    Text       = "No sales recorded this shift.",
                    Foreground = mutedBrush,
                    FontSize   = 12
                });
            }

            TotalSalesText.Text = $"RM {total:N2}";
        }

        private void ActualCashBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Guard: named elements are null while InitializeComponent() is still running
            if (DiscrepancyPreviewBorder == null) return;

            if (!decimal.TryParse(ActualCashBox.Text, out var actual) || actual < 0)
            {
                DiscrepancyPreviewBorder.Visibility = Visibility.Collapsed;
                return;
            }

            var diff = actual - _expectedCash;
            UpdateDiscrepancyPreview(diff);
        }

        private void UpdateDiscrepancyPreview(decimal diff)
        {
            DiscrepancyPreviewBorder.Visibility = Visibility.Visible;

            if (diff == 0)
            {
                DiscrepancyPreviewBorder.Background    = new SolidColorBrush(Color.FromRgb(6, 78, 59));   // dark green
                DiscrepancyPreviewBorder.BorderBrush   = new SolidColorBrush(Color.FromRgb(52, 211, 153));
                DiscrepancyPreviewBorder.BorderThickness = new Thickness(1);
                DiscrepancyIcon.Icon                   = IconChar.CheckCircle;
                DiscrepancyIcon.Foreground             = new SolidColorBrush(Color.FromRgb(52, 211, 153));
                DiscrepancyPreviewText.Text            = "Balanced";
                DiscrepancyPreviewText.Foreground      = new SolidColorBrush(Color.FromRgb(52, 211, 153));
            }
            else if (diff > 0)
            {
                DiscrepancyPreviewBorder.Background    = new SolidColorBrush(Color.FromRgb(12, 74, 110));  // dark blue
                DiscrepancyPreviewBorder.BorderBrush   = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                DiscrepancyPreviewBorder.BorderThickness = new Thickness(1);
                DiscrepancyIcon.Icon                   = IconChar.ArrowUp;
                DiscrepancyIcon.Foreground             = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                DiscrepancyPreviewText.Text            = $"OVER  RM {diff:N2}";
                DiscrepancyPreviewText.Foreground      = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            }
            else
            {
                DiscrepancyPreviewBorder.Background    = new SolidColorBrush(Color.FromRgb(127, 29, 29));  // dark red
                DiscrepancyPreviewBorder.BorderBrush   = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                DiscrepancyPreviewBorder.BorderThickness = new Thickness(1);
                DiscrepancyIcon.Icon                   = IconChar.ArrowDown;
                DiscrepancyIcon.Foreground             = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                DiscrepancyPreviewText.Text            = $"SHORT  RM {Math.Abs(diff):N2}";
                DiscrepancyPreviewText.Foreground      = new SolidColorBrush(Color.FromRgb(248, 113, 113));
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var name = ClosedByBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("Please enter the cashier name for closing.");
                return;
            }

            if (!decimal.TryParse(ActualCashBox.Text, out var actual) || actual < 0)
            {
                ShowError("Actual cash must be a valid non-negative number.");
                return;
            }

            ClosedByUser = name;
            ActualCash   = actual;
            Notes        = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
            Confirmed    = true;
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
