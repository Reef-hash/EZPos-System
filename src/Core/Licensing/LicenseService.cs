namespace EZPos.Core.Licensing
{
    /// <summary>
    /// Orchestrates license loading, storage, and validation.
    ///
    /// ── CURRENT STATE: MOCK ──────────────────────────────────────────────────
    /// All validation is mocked — no API calls are made.
    /// LoadAndValidate() always returns LicenseStatus.Valid so the app runs normally.
    /// Activate() accepts any non-empty key and saves it, returning Valid.
    ///
    /// This lets the full licensing structure be wired up without blocking the app
    /// until the real Stripe + license-key backend is ready.
    ///
    /// ── WHEN BACKEND IS READY ────────────────────────────────────────────────
    /// 1. Inject LicenseApiClient (Infrastructure/Licensing/) into this constructor.
    /// 2. In LoadAndValidate():  call _apiClient.ValidateAsync(key, deviceId).
    /// 3. In Activate():         call _apiClient.ActivateAsync(key, deviceId).
    /// 4. Map the API response to LicenseStatus enum values.
    /// 5. Remove all MOCK sections marked with TODO below.
    /// </summary>
    public class LicenseService : ILicenseService
    {
        private readonly ILicenseStorage _storage;
        private LicenseInfo _current = new();

        // TODO: inject LicenseApiClient here when backend is ready:
        // private readonly LicenseApiClient _apiClient;

        public LicenseInfo Current    => _current;
        public bool        IsLicensed => _current.Status == LicenseStatus.Valid;

        public LicenseService(ILicenseStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// Loads stored key and validates it.
        ///
        /// ── MOCK ─────────────────────────────────────────────────────────────
        /// If any key is found in storage → Valid.
        /// If no key found                → also Valid (development bypass).
        /// TODO: replace MOCK block with:
        ///   var result = await _apiClient.ValidateAsync(key, GetDeviceId());
        ///   _current   = MapApiResponse(key, result);
        /// ─────────────────────────────────────────────────────────────────────
        /// </summary>
        public LicenseInfo LoadAndValidate()
        {
            var key = _storage.LoadKey();

            // ── MOCK VALIDATION ───────────────────────────────────────────────
            // TODO: remove this block and call real API instead.
            _current = new LicenseInfo
            {
                Key        = key ?? string.Empty,
                Status     = LicenseStatus.Valid,   // always valid until API is wired
                ExpiryDate = null,                  // TODO: set from API response
                DeviceId   = string.Empty,          // TODO: generate device fingerprint
                PlanName   = "Development"
            };
            return _current;
            // ─────────────────────────────────────────────────────────────────
        }

        /// <summary>
        /// Saves a new key and "activates" it.
        ///
        /// ── MOCK ─────────────────────────────────────────────────────────────
        /// Any non-empty key is accepted immediately.
        /// TODO: replace MOCK block with:
        ///   var result = await _apiClient.ActivateAsync(key, GetDeviceId());
        ///   if (!result.Success) return new LicenseInfo { Status = LicenseStatus.Invalid };
        /// ─────────────────────────────────────────────────────────────────────
        /// </summary>
        public LicenseInfo Activate(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _current = new LicenseInfo { Status = LicenseStatus.Invalid };
                return _current;
            }

            // ── MOCK ACTIVATION ───────────────────────────────────────────────
            // TODO: validate with API before saving.
            _storage.SaveKey(key.Trim());
            _current = new LicenseInfo
            {
                Key      = key.Trim(),
                Status   = LicenseStatus.Valid,
                PlanName = "Development"
            };
            return _current;
            // ─────────────────────────────────────────────────────────────────
        }

        // TODO: implement device fingerprint generation for device-locked licenses.
        // private static string GetDeviceId() => ...; // WMI disk serial + CPU ID hash
    }
}
