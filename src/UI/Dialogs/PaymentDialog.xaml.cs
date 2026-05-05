using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EZPos.DataAccess.Repositories;

namespace EZPos.UI.Dialogs
{
    public partial class PaymentDialog : Window
    {
        private readonly decimal _baseTotal;
        private readonly decimal _subtotal;
        private readonly decimal _tax;
        private readonly Dictionary<Key, Action> _paymentShortcuts;
        private readonly Button[] _paymentButtons;
        private readonly Dictionary<Button, string> _paymentButtonMethods;
        private readonly List<Button> _quickAmountButtons = new();

        private int _paymentFocusIndex;
        private int _quickAmountFocusIndex;

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

            _paymentButtons = new[] { CashBtn, CardBtn, QrBtn, ChequeBtn };
            _paymentButtonMethods = new Dictionary<Button, string>
            {
                [CashBtn] = "Cash",
                [CardBtn] = "Card",
                [QrBtn] = "QR Code",
                [ChequeBtn] = "Cheque"
            };

            _paymentShortcuts = new Dictionary<Key, Action>
            {
            };

            RegisterConfiguredShortcut("PaymentHotkeyCash", "F1", () => SetPaymentMethod("Cash"));
            RegisterConfiguredShortcut("PaymentHotkeyQr", "F2", () => SetPaymentMethod("QR Code"));
            RegisterConfiguredShortcut("PaymentHotkeyCard", "F3", () => SetPaymentMethod("Card"));
            RegisterConfiguredShortcut("PaymentHotkeyCheque", "F4", () => SetPaymentMethod("Cheque"));

            PreviewKeyDown += PaymentDialog_PreviewKeyDown;
            PreviewTextInput += PaymentDialog_PreviewTextInput;
            Loaded += PaymentDialog_Loaded;

            foreach (var btn in _paymentButtons)
            {
                btn.GotKeyboardFocus += PaymentMethodButton_GotKeyboardFocus;
            }

            var taxRate = ConfigHelper.Get("TaxRate", "6");
            TaxLabel.Text = $"Tax ({taxRate}%)";
            SubtotalText.Text = $"RM {_subtotal:F2}";
            TaxText.Text = $"RM {_tax:F2}";

            SetPaymentMethod("Cash");
        }

        private void RegisterConfiguredShortcut(string configKey, string defaultKey, Action action)
        {
            var key = ConfigHelper.GetKey(configKey, defaultKey);
            if (key != Key.None)
                _paymentShortcuts[key] = action;
        }

        private void PaymentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (SelectedPaymentMethod == "Cash")
            {
                AmountPaidBox.Focus();
                AmountPaidBox.SelectAll();
            }
            else
            {
                _paymentButtons[_paymentFocusIndex].Focus();
            }
        }

        private void PaymentMethodButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not Button btn || !_paymentButtonMethods.TryGetValue(btn, out var method))
                return;

            SetPaymentMethod(method);
        }

        private void PaymentDialog_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (SelectedPaymentMethod != "Cash")
                return;

            if (Keyboard.FocusedElement == AmountPaidBox)
                return;

            if (string.IsNullOrEmpty(e.Text))
                return;

            var c = e.Text[0];
            if (!char.IsDigit(c) && c != '.' && c != ',')
                return;

            AmountPaidBox.Focus();
            AmountPaidBox.SelectAll();

            var start = AmountPaidBox.SelectionStart;
            var len = AmountPaidBox.SelectionLength;
            var current = AmountPaidBox.Text ?? string.Empty;
            var next = current.Remove(start, len).Insert(start, e.Text);

            AmountPaidBox.Text = next;
            AmountPaidBox.SelectionStart = start + e.Text.Length;
            AmountPaidBox.SelectionLength = 0;
            e.Handled = true;
        }

        private void PaymentDialog_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_paymentShortcuts.TryGetValue(e.Key, out var action))
            {
                if (TryHandleArrowNavigation(e.Key))
                    e.Handled = true;

                return;
            }

            action();
            e.Handled = true;
        }

        private bool TryHandleArrowNavigation(Key key)
        {
            if (key is not (Key.Up or Key.Down or Key.Left or Key.Right))
                return false;

            var focused = Keyboard.FocusedElement;

            if (focused == AmountPaidBox)
                return HandleAmountBoxArrowKey(key);

            if (focused is Button focusedButton && _quickAmountButtons.Contains(focusedButton))
                return HandleQuickAmountArrowKey(key, focusedButton);

            if (focused is Button paymentButton && _paymentButtons.Contains(paymentButton))
                return HandlePaymentButtonArrowKey(key, paymentButton);

            // If focus is elsewhere inside this dialog, Up/Down can still move through the payment workflow.
            if (key == Key.Up)
            {
                _paymentButtons[_paymentFocusIndex].Focus();
                return true;
            }

            if (key == Key.Down && SelectedPaymentMethod == "Cash")
            {
                AmountPaidBox.Focus();
                AmountPaidBox.SelectAll();
                return true;
            }

            return false;
        }

        private bool HandleAmountBoxArrowKey(Key key)
        {
            if (SelectedPaymentMethod != "Cash")
                return false;

            if (key == Key.Down)
            {
                if (_quickAmountButtons.Count > 0)
                {
                    _quickAmountFocusIndex = 0;
                    _quickAmountButtons[_quickAmountFocusIndex].Focus();
                }
                else
                {
                    ConfirmBtn.Focus();
                }

                return true;
            }

            if (key == Key.Up)
            {
                _paymentButtons[_paymentFocusIndex].Focus();
                return true;
            }

            return false;
        }

        private bool HandleQuickAmountArrowKey(Key key, Button focusedButton)
        {
            var index = _quickAmountButtons.IndexOf(focusedButton);
            if (index < 0)
                return false;

            if (key == Key.Left)
            {
                _quickAmountFocusIndex = Math.Max(0, index - 1);
                _quickAmountButtons[_quickAmountFocusIndex].Focus();
                return true;
            }

            if (key == Key.Right)
            {
                _quickAmountFocusIndex = Math.Min(_quickAmountButtons.Count - 1, index + 1);
                _quickAmountButtons[_quickAmountFocusIndex].Focus();
                return true;
            }

            if (key == Key.Up)
            {
                AmountPaidBox.Focus();
                AmountPaidBox.SelectAll();
                return true;
            }

            if (key == Key.Down)
            {
                ConfirmBtn.Focus();
                return true;
            }

            return false;
        }

        private bool HandlePaymentButtonArrowKey(Key key, Button focusedButton)
        {
            var index = Array.IndexOf(_paymentButtons, focusedButton);
            if (index < 0)
                return false;

            const int cols = 2;
            var row = index / cols;
            var col = index % cols;

            if (key == Key.Left)
                col = Math.Max(0, col - 1);
            else if (key == Key.Right)
                col = Math.Min(cols - 1, col + 1);
            else if (key == Key.Up)
                row = Math.Max(0, row - 1);
            else if (key == Key.Down)
            {
                if (row == 1)
                {
                    if (SelectedPaymentMethod == "Cash")
                    {
                        AmountPaidBox.Focus();
                        AmountPaidBox.SelectAll();
                    }
                    else
                    {
                        ConfirmBtn.Focus();
                    }

                    return true;
                }

                row = 1;
            }

            var newIndex = row * cols + col;
            _paymentFocusIndex = newIndex;
            _paymentButtons[newIndex].Focus();
            return true;
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
            _paymentFocusIndex = method switch
            {
                "Cash" => 0,
                "Card" => 1,
                "QR Code" => 2,
                _ => 3
            };

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

                if (IsLoaded)
                {
                    AmountPaidBox.Focus();
                    AmountPaidBox.SelectAll();
                }
            }
            else
            {
                ChangeLabel.Text = "Paid";
                ChangeValueText.Text = $"RM {PayableTotal:F2}";
                TenderedAmount = PayableTotal;

                if (IsLoaded)
                    _paymentButtons[_paymentFocusIndex].Focus();
            }
        }

        private void RebuildQuickAmounts()
        {
            QuickAmountsPanel.Children.Clear();
            _quickAmountButtons.Clear();

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
                _quickAmountButtons.Add(btn);
            }

            _quickAmountFocusIndex = 0;
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
                ChangeLabel.Text      = "Balance Due";
                ChangeValueText.Text  = $"-RM {Math.Abs(diff):F2}";
                ChangeCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3FEF4444"));
                ChangeLabel.Foreground     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEF4444"));
                ChangeValueText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEF4444"));
            }
            else
            {
                ChangeLabel.Text      = "Change";
                ChangeValueText.Text  = $"RM {diff:F2}";
                ChangeCard.Background = (Brush)FindResource("CardBackgroundBrush");
                ChangeLabel.Foreground     = (Brush)FindResource("PrimaryBrush");
                ChangeValueText.Foreground = (Brush)FindResource("PrimaryBrush");
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
