namespace EZPos.Core.Licensing
{
    /// <summary>
    /// Abstraction over the physical storage mechanism for the license key.
    /// Keeping this separate from ILicenseService means you can swap storage
    /// (file, registry, encrypted blob) without touching validation logic.
    ///
    /// Current implementation: FileLicenseStorage (plain-text file in %ProgramData%\EZPos\).
    /// Future implementation: EncryptedRegistryLicenseStorage, CloudSyncStorage, etc.
    /// </summary>
    public interface ILicenseStorage
    {
        /// <summary>
        /// Returns the stored license key, or null if nothing has been saved yet.
        /// </summary>
        string? LoadKey();

        /// <summary>
        /// Persists the license key to the local storage medium.
        /// Called by LicenseService after a successful activation.
        /// </summary>
        void SaveKey(string key);

        /// <summary>
        /// Removes the stored license key (e.g. on deactivation or reinstall).
        /// TODO: trigger a deactivation API call before clearing.
        /// </summary>
        void ClearKey();
    }
}
