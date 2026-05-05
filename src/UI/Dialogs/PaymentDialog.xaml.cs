using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EZPos.DataAccess.Repositories;

namespace EZPos.UI.Dialogs
{
    public partial class PaymentDialog : Window
    {
        private readonly decimal _baseTotal;
        private readonly decimal _subtotal;
        private readonly decimal _tax;

        public string SelectedPaymentMethod { get; private set; } = "Cash";
        public decimal TenderedAmount { get; private set; }
        public decimal RoundingAdjustment { get; private set; }
        public decimal PayableTotal { get; private set; }

        public PaymentDialog(decimal subtotal, decimal tax, decimal baseTotal)
        {
            InitializeComponent();

            _subtotal  = subtotal;
            _tax       = tax;
            _baseTotal = baseTotal;

            var taxRate = ConfigHelper.Get("TaxRate", "6");
            TaxLabel.Text = $"Tax ({taxRate}%)";
            SubtotalText.Text = $"RM {_subtotal:F2}";
            TaxText.Text = $"RM {_tax:F2}";

            SetPaymentMethod("Cash");
        }

        private static decimal RoundCash(decimal amount)
            => Math.Round(amount / 0.05m, MidpointRounding.AwayFromZero) * 0.05m;

        private static List<decimal> BuildQuickAmounts(decimal payable)
        {
            var chips = new List<decimal>();

            var ceil = Math.Ceiling(payable);
            if (ceil <= payable) ceil += 1;
            chips.Add((decimal)ceil);

            var next5 = Math.Ceiling(payable / 5m) * 5m;
            if (next5 <= payable) next5 += 5m;
            chips.Add(next5);

            foreach (var note in new[] { 10m, 20m, 50m, 100m, 200m })
                if (note > payable) chips.Add(note);

            return chips.Distinct().OrderBy(v => v).Take(5).ToList();
        }

        private void SetPaymentMethod(string method)
        {
            SelectedPaymentMethod = method;

            var isCash = method == "Cash";
            RoundingAdjustment = isCash ? RoundCash(_baseTotal) - _baseTotal : 0m;
            PayableTotal = _baseTotal + RoundingAdjustment;

            if (RoundingAdjustment != 0)
            {
                RoundingLabel.Visibility = Visibility.Visible;
                RoundingText.Visibility = Visibility.Visible;
                RoundingText.Text = $"{(RoundingAdjustment > 0 ? "+" : "")}RM {RoundingAdjustment:F2}";
            }
            else
            {
                RoundingLabel.Visibility = Visibility.Collapsed;
                RoundingText.Visibility = Visibility.Collapsed;
            }

            TotalText.Text = $"RM {PayableTotal:F2}";
            ConfirmBtn.Content = $"Confirm Payment - RM {PayableTotal:F2}";

            CashBtn.Background = Brushes.Transparent;
            CardBtn.Background = Brushes.Transparent;
            QrBtn.Background = Brushes.Transparent;
            ChequeBtn.Background = Brushes.Transparent;

            var activeBg = (Brush)FindResource("PrimaryBrush");
            var activeFg = (Brush)FindResource("SidebarBrush");
            var normalFg = (Brush)FindResource("TextPrimaryBrush");

            foreach (var btn in new[] { CashBtn, CardBtn, QrBtn, ChequeBtn })
                btn.Foreground = normalFg;

            var selectedBtn = method switch
            {
                "Cash" => CashBtn,
                "Card" => CardBtn,
                "QR Code" => QrBtn,
                _ => ChequeBtn
            };
            selectedBtn.Background = activeBg;
            selectedBtn.Foreground = activeFg;

            AmountPaidLabel.Visibility = isCash ? Visibility.Visible : Visibility.Collapsed;
            AmountPaidBox.Visibility = isCash ? Visibility.Visible : Visibility.Collapsed;
            QuickAmountsPanel.Visibility = isCash ? Visibility.Visible : Visibility.Collapsed;
            ChangeCard.Visibility = isCash ? Visibility.Visible : Visibility.Collapsed;

            if (isCash)
            {
                AmountPaidBox.Text = PayableTotal.ToString("F2");
                RebuildQuickAmounts();
                RefreshChange();
            }
            else
            {
                ChangeLabel.Text = "Paid";
                ChangeValueText.Text = $"RM {PayableTotal:F2}";
                TenderedAmount = PayableTotal;
            }
        }

        private void RebuildQuickAmounts()
        {
            QuickAmountsPanel.Children.Clear();
            foreach (var amount in BuildQuickAmounts(PayableTotal))
            {
                var a = amount;
                var btn = new Button
                {
                    Content = $"RM {a:F0}",
                    Margin = new Thickness(0, 0, 8, 8),
                    Padding = new Thickness(16, 8, 16, 8),
                    MinWidth = 88
                };
                btn.Click += (_, _) => AmountPaidBox.Text = a.ToString("F2");
                QuickAmountsPanel.Children.Add(btn);
            }
        }

        private void RefreshChange()
        {
            if (SelectedPaymentMethod != "Cash")
            {
                ChangeLabel.Text = "Paid";
                ChangeValueText.Text = $"RM {PayableTotal:F2}";
                return;
            }

            if (!decimal.TryParse(AmountPaidBox.Text.Trim(), out var paid))
            {
                ChangeLabel.Text = "Invalid";
                ChangeValueText.Text = "RM -";
                return;
            }

            TenderedAmount = paid;
            var diff = paid - PayableTotal;
            if (diff < 0)
            {
                ChangeLabel.Text = "Balance";
                ChangeValueText.Text = $"RM {Math.Abs(diff):F2}";
            }
            else
            {
                ChangeLabel.Text = "Change";
                ChangeValueText.Text = $"RM {diff:F2}";
            }
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPaymentMethod == "Cash")
            {
                if (!decimal.TryParse(AmountPaidBox.Text.Trim(), out var paid) || paid < PayableTotal)
                {
                    MessageBox.Show($"Cash paid must be at least RM {PayableTotal:F2}.",
                        "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TenderedAmount = paid;
            }
            else
            {
                TenderedAmount = PayableTotal;
            }

            DialogResult = true;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private void AmountPaidBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshChange();

        private void CashBtn_Click(object sender, RoutedEventArgs e) => SetPaymentMethod("Cash");
        private void CardBtn_Click(object sender, RoutedEventArgs e) => SetPaymentMethod("Card");
        private void QrBtn_Click(object sender, RoutedEventArgs e) => SetPaymentMethod("QR Code");
        private void ChequeBtn_Click(object sender, RoutedEventArgs e) => SetPaymentMethod("Cheque");
    }
}
