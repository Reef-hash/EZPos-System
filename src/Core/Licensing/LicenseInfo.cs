using System;

namespace EZPos.Core.Licensing
{
    /// <summary>
    /// Holds the resolved licensing state for the current installation.
    /// Populated by LicenseService after loading and (mock) validating.
    ///
    /// Extension points:
    ///   - DeviceId   → bind to hardware fingerprint for device-locked licenses.
    ///   - ExpiryDate → drive subscription-expiry UI warnings / grace period.
    ///   - PlanName   → expose feature flags per plan (e.g. "Starter" vs "Pro").
    /// </summary>
    public class LicenseInfo
    {
        /// <summary>The raw license key as entered or stored.</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Resolved status after the last LoadAndValidate / Activate call.</summary>
        public LicenseStatus Status { get; set; } = LicenseStatus.Missing;

        /// <summary>
        /// UTC expiry date of the subscription.
        /// Null = perpetual license or not yet set by the API.
        /// TODO: populate from API response when backend is ready.
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Hardware / OS device fingerprint for device-locked licensing.
        /// TODO: generate via WMI / registry on first activation.
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>UTC timestamp of first activation, stored locally for UI display.</summary>
        public DateTime? ActivatedAt { get; set; }

        /// <summary>
        /// Human-readable plan name returned by the API (e.g. "Starter", "Pro", "Enterprise").
        /// TODO: set from API response; drive feature-flag checks via this field.
        /// </summary>
        public string PlanName { get; set; } = string.Empty;

        /// <summary>True when the status allows the app to operate normally.</summary>
        public bool IsLicensed => Status == LicenseStatus.Valid;
    }
}
