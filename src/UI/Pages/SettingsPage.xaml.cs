using System;
using System.IO;
using System.Linq;
using System.Printing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.SQLite;
using EZPos.Business.Services;
using EZPos.DataAccess.Repositories;
using EZPos.UI.Dialogs;
using EZPos.UI.State;

namespace EZPos.UI.Pages
{
    public partial class SettingsPage : UserControl
    {
        private readonly PosStateStore _stateStore;

        public SettingsPage(PosStateStore stateStore)
        {
            InitializeComponent();
            _stateStore = stateStore;
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            var asm = System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly();
            var ver = (asm.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? asm.GetName().Version?.ToString() ?? "1.0").Split('+')[0];
            VersionRun.Text = $"Version {ver}  \u2022  Built with .NET 6 + WPF";

            StoreNameBox.Text     = ConfigHelper.Get("StoreName",      "EZPos Store");
            PrinterNameBox.Text   = ConfigHelper.Get("PrinterName",    "");
            TaxRateBox.Text       = ConfigHelper.Get("TaxRate",        "6");
            CurrencyBox.Text      = ConfigHelper.Get("Currency",       "RM");
            ReceiptFooterBox.Text = ConfigHelper.Get("ReceiptFooter",  "Thank you, come again!");
            DatabasePathText.Text = Database.DbFile;

            PaymentCashHotkeyBox.Text    = ConfigHelper.Get("PaymentHotkeyCash", "F1");
            PaymentQrHotkeyBox.Text      = ConfigHelper.Get("PaymentHotkeyQr", "F2");
            PaymentCardHotkeyBox.Text    = ConfigHelper.Get("PaymentHotkeyCard", "F3");
            PaymentChequeHotkeyBox.Text  = ConfigHelper.Get("PaymentHotkeyCheque", "F4");
            ReceiptNewSaleHotkeyBox.Text = ConfigHelper.Get("ReceiptHotkeyNewSale", "PageUp");
            ReceiptPrintHotkeyBox.Text   = ConfigHelper.Get("ReceiptHotkeyPrint", "PageDown");
            AutoPrintCheckBox.IsChecked  = ConfigHelper.Get("AutoPrint", "false") == "true";

            // Select matching TaxMode item
            var savedMode = ConfigHelper.Get("TaxMode", "PerReceipt");
            foreach (ComboBoxItem item in TaxModeCombo.Items)
                if (item.Tag?.ToString() == savedMode)
                { TaxModeCombo.SelectedItem = item; break; }
            if (TaxModeCombo.SelectedItem is null) TaxModeCombo.SelectedIndex = 0;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate tax rate is a non-negative number
            if (!decimal.TryParse(TaxRateBox.Text.Trim(), out var tax) || tax < 0 || tax > 100)
            {
                MessageBox.Show("Tax rate must be a number between 0 and 100.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TaxRateBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(StoreNameBox.Text.Trim()))
            {
                MessageBox.Show("Store name cannot be empty.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StoreNameBox.Focus();
                return;
            }

            if (!TryGetValidatedHotkey(PaymentCashHotkeyBox, "Payment: Cash", out var paymentCashKey)) return;
            if (!TryGetValidatedHotkey(PaymentQrHotkeyBox, "Payment: QR", out var paymentQrKey)) return;
            if (!TryGetValidatedHotkey(PaymentCardHotkeyBox, "Payment: Card", out var paymentCardKey)) return;
            if (!TryGetValidatedHotkey(PaymentChequeHotkeyBox, "Payment: Cheque", out var paymentChequeKey)) return;
            if (!TryGetValidatedHotkey(ReceiptNewSaleHotkeyBox, "Receipt: New Sale", out var receiptNewSaleKey)) return;
            if (!TryGetValidatedHotkey(ReceiptPrintHotkeyBox, "Receipt: Print", out var receiptPrintKey)) return;

            var duplicate = new[]
            {
                (Name: "Payment: Cash", Key: paymentCashKey),
                (Name: "Payment: QR", Key: paymentQrKey),
                (Name: "Payment: Card", Key: paymentCardKey),
                (Name: "Payment: Cheque", Key: paymentChequeKey),
                (Name: "Receipt: New Sale", Key: receiptNewSaleKey),
                (Name: "Receipt: Print", Key: receiptPrintKey)
            }
            .GroupBy(x => x.Key)
            .FirstOrDefault(g => g.Count() > 1);

            if (duplicate is not null)
            {
                var labels = string.Join(", ", duplicate.Select(x => x.Name));
                MessageBox.Show($"Duplicate hotkey detected ({duplicate.Key}): {labels}\n\nPlease use unique keys.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConfigHelper.Set("StoreName",     StoreNameBox.Text.Trim());
            ConfigHelper.Set("PrinterName",   PrinterNameBox.Text.Trim());
            ConfigHelper.Set("TaxRate",       tax.ToString());
            ConfigHelper.Set("Currency",      CurrencyBox.Text.Trim());
            ConfigHelper.Set("ReceiptFooter", ReceiptFooterBox.Text.Trim());
            ConfigHelper.SetKey("PaymentHotkeyCash", paymentCashKey);
            ConfigHelper.SetKey("PaymentHotkeyQr", paymentQrKey);
            ConfigHelper.SetKey("PaymentHotkeyCard", paymentCardKey);
            ConfigHelper.SetKey("PaymentHotkeyCheque", paymentChequeKey);
            ConfigHelper.SetKey("ReceiptHotkeyNewSale", receiptNewSaleKey);
            ConfigHelper.SetKey("ReceiptHotkeyPrint", receiptPrintKey);
            ConfigHelper.Set("AutoPrint", AutoPrintCheckBox.IsChecked == true ? "true" : "false");

            var taxMode = (TaxModeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "PerReceipt";
            ConfigHelper.Set("TaxMode", taxMode);

            // Apply new tax config to the live state store immediately
            _stateStore.ReloadTaxConfig();

            MessageBox.Show("Settings saved successfully.", "Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static bool TryGetValidatedHotkey(TextBox input, string label, out Key key)
        {
            key = Key.None;
            var raw = input.Text.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                MessageBox.Show($"{label} hotkey cannot be empty.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                input.Focus();
                return false;
            }

            if (!TryParseHotkey(raw, out key) || key == Key.None)
            {
                MessageBox.Show($"Invalid key for {label}: '{raw}'.\n\nExample values: F1, F2, F3, F4, PageUp, PageDown, Insert, Home",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                input.Focus();
                input.SelectAll();
                return false;
            }

            input.Text = key.ToString();
            return true;
        }

        private static bool TryParseHotkey(string raw, out Key key)
        {
            key = Key.None;
            var normalized = raw.Trim();

            if (normalized.Equals("PgUp", StringComparison.OrdinalIgnoreCase))
            {
                key = Key.PageUp;
                return true;
            }

            if (normalized.Equals("PgDn", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("PgDown", StringComparison.OrdinalIgnoreCase))
            {
                key = Key.PageDown;
                return true;
            }

            if (Enum.TryParse<Key>(normalized, true, out var parsed))
            {
                key = parsed;
                return true;
            }

            var converter = new KeyConverter();
            var converted = converter.ConvertFromString(normalized);
            if (converted is Key typed)
            {
                key = typed;
                return true;
            }

            return false;
        }

        private void DetectPrinters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var queue = new LocalPrintServer();
                var printers = queue.GetPrintQueues()
                                    .Select(q => q.FullName)
                                    .OrderBy(n => n)
                                    .ToList();

                if (printers.Count == 0)
                {
                    MessageBox.Show("No printers found on this computer.", "No Printers",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show a picker dialog listing all available printers
                var picker = new Window
                {
                    Title               = "Select Printer",
                    Width               = 420,
                    Height              = 340,
                    ResizeMode          = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner               = Window.GetWindow(this),
                    Background          = (System.Windows.Media.Brush)FindResource("ContentBrush"),
                    FontFamily          = (System.Windows.Media.FontFamily)FindResource("AppFont")
                };

                var listBox = new ListBox
                {
                    Margin          = new Thickness(16, 16, 16, 8),
                    FontSize        = 13,
                    Background      = (System.Windows.Media.Brush)FindResource("CardBackgroundBrush"),
                    Foreground      = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                    BorderBrush     = (System.Windows.Media.Brush)FindResource("BorderBrush"),
                    BorderThickness = new Thickness(1)
                };
                foreach (var p in printers) listBox.Items.Add(p);
                if (!string.IsNullOrWhiteSpace(PrinterNameBox.Text))
                    listBox.SelectedItem = printers.FirstOrDefault(p => p == PrinterNameBox.Text);

                var selectBtn = new Button
                {
                    Content  = "Select",
                    Height   = 38,
                    Margin   = new Thickness(16, 0, 16, 16),
                    Style    = (Style)FindResource("PrimaryButtonStyle")
                };

                selectBtn.Click += (_, _) =>
                {
                    if (listBox.SelectedItem is string chosen)
                    {
                        PrinterNameBox.Text = chosen;
                        PrinterHintText.Text = $"Selected: {chosen}";
                    }
                    picker.DialogResult = true;
                };

                var panel = new StackPanel();
                panel.Children.Add(listBox);
                panel.Children.Add(selectBtn);
                picker.Content = panel;
                picker.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not list printers:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(Database.DbFile))
                {
                    MessageBox.Show("The active database file could not be found.", "Backup Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Backup Database",
                    Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                    DefaultExt = ".db",
                    AddExtension = true,
                    FileName = $"EZPos_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
                };

                if (dialog.ShowDialog() != true)
                    return;

                File.Copy(Database.DbFile, dialog.FileName, overwrite: false);

                var result = MessageBox.Show(
                    $"Database backup created successfully.\n\nOpen the backup folder now?",
                    "Backup Complete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = Path.GetDirectoryName(dialog.FileName),
                        UseShellExecute = true
                    });
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Could not create backup:\n{ex.Message}", "Backup Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected backup error:\n{ex.Message}", "Backup Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestoreDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Restore Database Backup",
                    Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (dialog.ShowDialog() != true)
                    return;

                if (string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(Database.DbFile), StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Please choose a backup file, not the active live database.", "Restore Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!IsReadableSQLiteFile(dialog.FileName))
                {
                    MessageBox.Show("The selected file is not a readable SQLite database.", "Restore Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    "Restore will replace the current live database.\n\nA safety backup of the current database will be created automatically before restore.\n\nContinue?",
                    "Confirm Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var dbDirectory = Path.GetDirectoryName(Database.DbFile) ?? AppDomain.CurrentDomain.BaseDirectory;
                var safetyBackupPath = Path.Combine(dbDirectory, $"EZPos_PreRestore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                File.Copy(Database.DbFile, safetyBackupPath, overwrite: true);

                var stagedRestorePath = Path.Combine(dbDirectory, $"EZPos_Restore_{Guid.NewGuid():N}.db");
                File.Copy(dialog.FileName, stagedRestorePath, overwrite: true);

                try
                {
                    File.Copy(stagedRestorePath, Database.DbFile, overwrite: true);
                }
                finally
                {
                    if (File.Exists(stagedRestorePath))
                        File.Delete(stagedRestorePath);
                }

                MessageBox.Show(
                    $"Database restored successfully.\n\nSafety backup saved to:\n{safetyBackupPath}\n\nEZPos will now close. Reopen it to load the restored data.",
                    "Restore Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Application.Current.Shutdown();
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Could not restore backup:\n{ex.Message}", "Restore Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected restore error:\n{ex.Message}", "Restore Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool IsReadableSQLiteFile(string filePath)
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={filePath};Version=3;Read Only=True;");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA schema_version;";
                _ = command.ExecuteScalar();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            // Disable button during check
            CheckForUpdatesButton.IsEnabled = false;

            try
            {
                // Get manifest URL from config (disable if not set)
                var manifestUrl = ConfigHelper.Get("App:UpdateManifestUrl", "");
                if (string.IsNullOrWhiteSpace(manifestUrl))
                {
                    MessageBox.Show(
                        "Update checking is not configured for this installation.",
                        "Updates Disabled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Create updater service with current app version
                var currentVersion = GetCurrentAppVersion();
                var updater = new EZPos.Business.Services.UpdaterService(currentVersion, manifestUrl);

                // Check for available updates
                var manifest = await updater.CheckForUpdatesAsync();

                if (manifest == null)
                {
                    MessageBox.Show(
                        "You are on the latest version.",
                        "No Updates Available",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Show update dialog
                var dialog = new EZPos.UI.Dialogs.UpdateAvailableDialog(manifest, currentVersion)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() != true || !dialog.UserClickedUpdate)
                {
                    return;  // User skipped
                }

                // User wants to update; download installer
                MessageBox.Show(
                    "Starting update download...",
                    "Update",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Download installer to temp
                var installerPath = Path.Combine(Path.GetTempPath(), $"EZPos-Setup-v{manifest.Version}.exe");
                var downloadSuccess = await updater.DownloadInstallerAsync(
                    manifest.DownloadUrl ?? "",
                    manifest.Checksum?.Algorithm,
                    manifest.Checksum?.Value,
                    installerPath);

                if (!downloadSuccess)
                {
                    MessageBox.Show(
                        "Failed to download update. Please try again later.",
                        "Download Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Backup current database before exit
                try
                {
                    var dbDir = Path.GetDirectoryName(Database.DbFile) ?? AppDomain.CurrentDomain.BaseDirectory;
                    var backupPath = Path.Combine(dbDir, $"EZPos_PreUpdate_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                    File.Copy(Database.DbFile, backupPath, overwrite: false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create pre-update backup: {ex.Message}");
                }

                // Show confirmation and launch installer
                var confirm = MessageBox.Show(
                    $"Update ready to install (v{manifest.Version}).\n\nThe app will exit so the installer can run.\n\nContinue?",
                    "Ready to Install",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                // Launch installer silently
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = "/SILENT /NORESTART",
                        UseShellExecute = false
                    });

                    // Give installer a moment to start, then close app
                    await System.Threading.Tasks.Task.Delay(1000);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to launch installer:\n{ex.Message}",
                        "Installation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Update check failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                CheckForUpdatesButton.IsEnabled = true;
            }
        }

        private static string GetCurrentAppVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            // Prefer informational version when present (supports semantic versioning with pre-release labels).
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informational))
            {
                return informational.Split('+')[0];
            }

            var version = assembly.GetName().Version;
            if (version == null)
            {
                return "1.0.0";
            }

            var build = version.Build < 0 ? 0 : version.Build;
            return $"{version.Major}.{version.Minor}.{build}";
        }
    }
}
