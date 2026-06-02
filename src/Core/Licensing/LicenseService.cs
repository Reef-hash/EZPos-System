using System.Threading.Tasks;
using EZPos.Infrastructure.Licensing;

namespace EZPos.Core.Licensing
{
    /// <summary>
    /// Orchestrates license loading, API validation, and local grace-period caching.
    ///
    /// ── LOAD &amp; VALIDATE FLOW ────────────────────────────────────────────────
    ///   1. Load key from FileLicenseStorage (%ProgramData%\EZPos\license.dat).
    ///   2. No key found     → Missing  → LicenseRequiredWindow shown by App.xaml.cs.
    ///   3. Call POST /api/licenses/validate via LicenseApiClient (8-second timeout).
    ///   4. API online + valid    → save grace-period cache → Valid → app opens.
    ///   5. API online + invalid  → Invalid → LicenseRequiredWindow.
    ///   6. API offline (network error):
    ///        cache ≤ 7 days old → Valid (grace period).
    ///        cache > 7 days / no cache → Expired → show expiry message + Shutdown.
    ///
    /// ── ACTIVATION FLOW ─────────────────────────────────────────────────────
    ///   1. User types key in LicenseRequiredWindow → calls Activate(key).
    ///   2. API validates the key.
    ///   3. Valid   → save key to disk + save grace cache → return Valid.
    ///   4. Invalid → return Invalid (key not saved; user can retry).
    ///   5. Offline → return Invalid with message — user must retry when online.
    /// </summary>
    public class LicenseService : ILicenseService
    {
        private readonly ILicenseStorage  _storage;
        private readonly LicenseApiClient _apiClient;
        private LicenseInfo _current = new();

        public LicenseInfo Current    => _current;
        public bool        IsLicensed => _current.Status == LicenseStatus.Valid;

        public LicenseService(ILicenseStorage storage, LicenseApiClient apiClient)
        {
            _storage   = storage;
            _apiClient = apiClient;
        }

        /// <summary>
        /// Loads the stored key and validates it against the web API.
        /// Falls back to the grace-period cache when the API is unreachable.
        /// </summary>
        public LicenseInfo LoadAndValidate()
        {
            var key = _storage.LoadKey();

            if (string.IsNullOrWhiteSpace(key))
            {
                _current = new LicenseInfo { Status = LicenseStatus.Missing };
                return _current;
            }

            // Run async API call on thread pool — prevents WPF UI-thread deadlock
            var deviceId = DeviceFingerprint.GetDeviceId();
            var api = Task.Run(() => _apiClient.ValidateAsync(key, deviceId)).GetAwaiter().GetResult();

            if (api.IsOffline)
            {
                _current = LicenseValidationCache.IsWithinGracePeriod(key)
                    ? new LicenseInfo {
                        Key = key,
                        Status = LicenseStatus.Valid,
                        PlanName = "Professional",
                        ApiMessage = "Offline validation in grace period."
                    }
                    : new LicenseInfo {
                        Key = key,
                        Status = LicenseStatus.Expired,
                        PlanName = "Grace period expired",
                        ApiMessage = "Offline validation grace period has expired."
                    };
                return _current;
            }

            if (api.IsValid)
            {
                LicenseValidationCache.SaveValid(key);
                _current = new LicenseInfo
                {
                    Key = key,
                    Status = LicenseStatus.Valid,
                    PlanName = string.IsNullOrWhiteSpace(api.Plan) ? "Professional" : api.Plan,
                    ExpiryDate = api.ExpiresAt,
                    ReasonCode = api.ReasonCode,
                    ClientAction = api.ClientAction,
                    ApiMessage = api.Message,
                };
            }
            else
            {
                _current = BuildFailureInfo(key, api);
            }

            return _current;
        }

        /// <summary>
        /// Validates a new key against the API and saves it locally if valid.
        /// Called by LicenseRequiredWindow when the user submits a key.
        /// </summary>
        public LicenseInfo Activate(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _current = new LicenseInfo { Status = LicenseStatus.Invalid };
                return _current;
            }

            var trimmed  = key.Trim().ToUpperInvariant();
            var deviceId = DeviceFingerprint.GetDeviceId();
            var api      = Task.Run(() => _apiClient.ActivateAsync(trimmed, deviceId)).GetAwaiter().GetResult();

            if (api.IsOffline)
            {
                _current = new LicenseInfo
                {
                    Key = trimmed,
                    Status = LicenseStatus.Invalid,
                    ApiMessage = "Activation requires online connection.",
                    ReasonCode = "OFFLINE_ACTIVATION_BLOCKED",
                    ClientAction = "retry_when_online",
                };
                return _current;
            }

            if (!api.IsValid)
            {
                _current = BuildFailureInfo(trimmed, api);
                return _current;
            }

            _storage.SaveKey(trimmed);
            LicenseValidationCache.SaveValid(trimmed);
            _current = new LicenseInfo
            {
                Key = trimmed,
                Status = LicenseStatus.Valid,
                PlanName = string.IsNullOrWhiteSpace(api.Plan) ? "Professional" : api.Plan,
                ExpiryDate = api.ExpiresAt,
                ReasonCode = api.ReasonCode,
                ClientAction = api.ClientAction,
                ApiMessage = api.Message,
            };
            return _current;
        }

        private static LicenseInfo BuildFailureInfo(string key, LicenseApiResponse api)
        {
            return new LicenseInfo
            {
                Key = key,
                Status = MapStatus(api.Status),
                PlanName = string.IsNullOrWhiteSpace(api.Plan) ? string.Empty : api.Plan,
                ExpiryDate = api.ExpiresAt,
                ReasonCode = api.ReasonCode,
                ClientAction = api.ClientAction,
                ApiMessage = api.Message,
            };
        }

        private static LicenseStatus MapStatus(string status)
        {
            return status?.Trim().ToLowerInvariant() switch
            {
                "valid" => LicenseStatus.Valid,
                "expired" => LicenseStatus.Expired,
                "revoked" => LicenseStatus.Revoked,
                "product_mismatch" => LicenseStatus.ProductMismatch,
                "device_mismatch" => LicenseStatus.DeviceMismatch,
                "seat_exceeded" => LicenseStatus.SeatExceeded,
                "not_found" => LicenseStatus.Invalid,
                _ => LicenseStatus.Invalid,
            };
        }
    }
}
