using System;
using System.Globalization;
using System.IO;

namespace EZPos.Core.Licensing
{
    /// <summary>
    /// Temporary trial-based implementation of ILicenseService.
    ///
    /// Grants a 30-day evaluation period starting from the date written to
    /// %ProgramData%\EZPos\trial.dat by the Inno Setup installer.
    /// If the file is absent (e.g. direct-run without installer), the trial is
    /// auto-initialized on first launch so the app never hard-crashes.
    ///
    /// ── REPLACEMENT PLAN ──────────────────────────────────────────────────────
    ///   When HWID / online licensing is ready, change one line in App.xaml.cs:
    ///     Before:  ILicenseService licenseService = new TrialLicenseService();
    ///     After:   ILicenseService licenseService = new LicenseService(storage, apiClient);
    ///   Everything else (ILicenseService contract, App routing, UI) stays unchanged.
    /// ──────────────────────────────────────────────────────────────────────────
    /// </summary>
    public sealed class TrialLicenseService : ILicenseService
    {
        // ── Configuration constants ────────────────────────────────────────────

        /// <summary>How many days the trial is valid from first install.</summary>
        public const int TrialDurationDays = 30;

        /// <summary>
        /// Absolute path to the trial metadata file.
        /// Written by the Inno Setup installer; read by this service.
        /// </summary>
        public static readonly string TrialFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EZPos",
            "trial.dat");

        // ── State ──────────────────────────────────────────────────────────────

        private LicenseInfo _current = new();

        // ── ILicenseService ────────────────────────────────────────────────────

        /// <inheritdoc/>
        public LicenseInfo Current    => _current;

        /// <inheritdoc/>
        public bool        IsLicensed => _current.Status == LicenseStatus.Valid;

        /// <summary>
        /// Reads trial.dat, evaluates expiry against UTC now, and populates Current.
        /// Safe to call multiple times; always re-reads from disk.
        /// </summary>
        public LicenseInfo LoadAndValidate()
        {
            var installDate = ReadInstallDate();

            if (installDate is null)
            {
                // trial.dat was not found — initialize now so the app can run.
                // This covers: direct-run without installer, or first launch on dev machine.
                installDate = DateTime.UtcNow;
                WriteInstallDate(installDate.Value);
            }

            var expiryDate = installDate.Value.AddDays(TrialDurationDays);
            var isExpired  = DateTime.UtcNow > expiryDate;

            _current = new LicenseInfo
            {
                Key         = string.Empty,
                Status      = isExpired ? LicenseStatus.Expired : LicenseStatus.Valid,
                ExpiryDate  = expiryDate,
                ActivatedAt = installDate,
                PlanName    = "Trial"
            };

            return _current;
        }

        /// <summary>
        /// Not applicable in trial mode — trial is date-based, not key-based.
        /// Returns the current trial status unchanged. Required by ILicenseService.
        /// </summary>
        public LicenseInfo Activate(string key) => _current;

        // ── Internal file helpers ──────────────────────────────────────────────

        private static DateTime? ReadInstallDate()
        {
            try
            {
                if (!File.Exists(TrialFilePath))
                    return null;

                const string prefix = "INSTALL_DATE=";

                foreach (var line in File.ReadAllLines(TrialFilePath))
                {
                    if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var value = line[prefix.Length..].Trim();

                        // Try round-trip (ISO 8601 with Z) — written by app self-init on dev machines.
                        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                                              DateTimeStyles.RoundtripKind, out var dt))
                        {
                            return dt.ToUniversalTime();
                        }

                        // Fallback: local-time string written by Inno Setup (e.g. '2026-05-07 14:30:00').
                        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                                              DateTimeStyles.AssumeLocal, out var dtLocal))
                        {
                            return dtLocal.ToUniversalTime();
                        }
                    }
                }
            }
            catch
            {
                // Fail safe: any I/O or parse error is treated as "no trial file".
                // The caller will auto-initialize a fresh trial.
            }

            return null;
        }

        private static void WriteInstallDate(DateTime utcDate)
        {
            try
            {
                var dir = Path.GetDirectoryName(TrialFilePath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Simple key=value format. Round-trip ("O") format preserves full UTC precision.
                File.WriteAllText(TrialFilePath, $"INSTALL_DATE={utcDate:O}{Environment.NewLine}");
            }
            catch
            {
                // Fail safe: if we cannot write (e.g. permissions), the app continues
                // without trial persistence. Next launch will re-attempt initialization.
            }
        }
    }
}
