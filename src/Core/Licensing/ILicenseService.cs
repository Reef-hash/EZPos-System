namespace EZPos.Core.Licensing
{
    /// <summary>
    /// Central contract for all license-checking operations.
    /// Consumed by App.xaml.cs on startup and by LicenseRequiredWindow.
    ///
    /// The concrete implementation (LicenseService) currently uses mock/stub logic.
    /// TODO: when backend is ready, inject LicenseApiClient into LicenseService
    ///       and replace the mock paths with real API calls.
    /// </summary>
    public interface ILicenseService
    {
        /// <summary>
        /// Loads the stored key from ILicenseStorage and validates it.
        /// Returns a LicenseInfo describing the current state.
        ///
        /// TODO: this will become async once API validation is introduced:
        ///   Task&lt;LicenseInfo&gt; LoadAndValidateAsync()
        /// </summary>
        LicenseInfo LoadAndValidate();

        /// <summary>
        /// Attempts to activate a new license key.
        /// On success, persists the key via ILicenseStorage and updates Current.
        ///
        /// TODO: call API to verify key before persisting:
        ///   Task&lt;LicenseInfo&gt; ActivateAsync(string key)
        /// </summary>
        LicenseInfo Activate(string key);

        /// <summary>The last LicenseInfo resolved by LoadAndValidate or Activate.</summary>
        LicenseInfo Current { get; }

        /// <summary>Convenience shorthand: true when Current.Status == Valid.</summary>
        bool IsLicensed { get; }
    }
}
