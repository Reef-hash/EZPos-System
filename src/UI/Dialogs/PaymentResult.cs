namespace EZPos.UI.Dialogs;

/// <summary>
/// Result returned by <see cref="PaymentDialog"/> via DialogHost.CloseDialogCommand.
/// </summary>
public sealed record PaymentResult(
    string  SelectedPaymentMethod,
    decimal TenderedAmount,
    decimal RoundingAdjustment,
    decimal PayableTotal);
