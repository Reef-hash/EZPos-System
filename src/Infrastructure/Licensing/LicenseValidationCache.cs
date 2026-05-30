using System;
using System.Globalization;
using System.IO;

namespace EZPos.Infrastructure.Licensing
{
    /// <summary>
    /// Caches the last successful API validation result on disk.
    ///
    /// PURPOSE — GRACE PERIOD:
    ///   When EZPos cannot reach the license server (no internet, server down),
    ///   it allows the app to keep running for up to 7 days after the last
    ///   successful online validation.  If the grace period expires, EZPos
    ///   shows LicenseRequiredWindow and refuses to open the main window.
    ///
    /// CACHE FILE:
    ///   Location : %ProgramData%\EZPos\license-cache.dat
    ///   Format   : key=value, one entry per line
    ///
    ///     LAST_VALIDATED=2026-05-17T14:48:00Z
    ///     KEY=EZPOS-XXXX-XXXX-XXXX
    ///     STATUS=Valid
    ///
    /// All methods are static and fail silently on I/O errors — non-critical path.
    /// </summary>
    public static class LicenseValidationCache
    {
        public const int GracePeriodDays = 7;

        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EZPos", "license-cache.dat");

        /// <summary>
        /// Writes (or overwrites) the cache file with the current UTC time and the given key.
        /// Call this every time the API confirms the key is valid.
        /// </summary>
        public static void SaveValid(string key)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);
                File.WriteAllText(CacheFilePath,
                    $"LAST_VALIDATED={DateTime.UtcNow:O}{Environment.NewLine}" +
                    $"KEY={key}{Environment.NewLine}" +
                    $"STATUS=Valid{Environment.NewLine}");
            }
            catch { /* fail safe — losing the cache file is not fatal */ }
        }

        /// <summary>
        /// Returns <c>true</c> when:
        ///   1. The cache file exists and STATUS=Valid
        ///   2. The cached KEY matches the supplied key
        ///   3. LAST_VALIDATED is within the grace period (7 days)
        ///
        /// Returns <c>false</c> on any I/O error, parse error, or mismatch.
        /// </summary>
        public static bool IsWithinGracePeriod(string key)
        {
            try
            {
                if (!File.Exists(CacheFilePath)) return false;

                DateTime? lastValidated = null;
                string?   cachedKey    = null;
                bool      statusValid  = false;

                foreach (var line in File.ReadAllLines(CacheFilePath))
                {
                    if (line.StartsWith("LAST_VALIDATED=", StringComparison.OrdinalIgnoreCase))
                    {
                        if (DateTime.TryParse(
                                line["LAST_VALIDATED=".Length..],
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.RoundtripKind,
                                out var dt))
                            lastValidated = dt;
                    }
                    else if (line.StartsWith("KEY=", StringComparison.OrdinalIgnoreCase))
                    {
                        cachedKey = line["KEY=".Length..].Trim();
                    }
                    else if (line.StartsWith("STATUS=", StringComparison.OrdinalIgnoreCase))
                    {
                        statusValid = line["STATUS=".Length..].Trim()
                            .Equals("Valid", StringComparison.OrdinalIgnoreCase);
                    }
                }

                return statusValid
                    && string.Equals(cachedKey, key, StringComparison.OrdinalIgnoreCase)
                    && lastValidated.HasValue
                    && (DateTime.UtcNow - lastValidated.Value).TotalDays <= GracePeriodDays;
            }
            catch { return false; }
        }
    }
}
