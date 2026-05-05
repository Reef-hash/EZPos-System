namespace EZPos.Core.Licensing
{
    /// <summary>
    /// Represents every possible state a license can be in.
    /// Used by ILicenseService to communicate the result of a license check back to the UI.
    ///
    /// Extension point: add Suspended, GracePeriod, etc. when the API backend supports them.
    /// </summary>
    public enum LicenseStatus
    {
        /// <summary>License key is present, not expired, and verified as genuine.</summary>
        Valid,

        /// <summary>A key was provided but failed verification (tampered / unknown / wrong device).</summary>
        Invalid,

        /// <summary>Key was valid but the subscription period has ended.</summary>
        Expired,

        /// <summary>No license key has been stored on this machine yet.</summary>
        Missing,

        /// <summary>Key is stored but has not been activated against the API yet.</summary>
        NotActivated
    }
}
