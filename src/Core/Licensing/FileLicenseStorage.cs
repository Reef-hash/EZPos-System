using System;
using System.IO;

namespace EZPos.Core.Licensing
{
    /// <summary>
    /// Stores the license key as a plain-text file under:
    ///   %ProgramData%\EZPos\license.dat
    ///
    /// Why ProgramData?
    ///   - Survives per-user profile changes and roaming.
    ///   - Accessible to all Windows user accounts on the machine.
    ///   - Standard location for machine-scoped app data.
    ///
    /// Extension point: replace with EncryptedRegistryStorage or signed-blob storage
    /// once tamper-resistance becomes a requirement.
    /// </summary>
    public class FileLicenseStorage : ILicenseStorage
    {
        private readonly string _filePath;

        /// <summary>Default constructor — uses %ProgramData%\EZPos\license.dat.</summary>
        public FileLicenseStorage()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EZPos");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "license.dat");
        }

        /// <summary>
        /// Testability constructor — inject any file path.
        /// Use in unit tests to avoid touching real ProgramData.
        /// </summary>
        public FileLicenseStorage(string filePath)
        {
            _filePath = filePath;
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        }

        /// <inheritdoc/>
        public string? LoadKey()
        {
            if (!File.Exists(_filePath)) return null;
            var content = File.ReadAllText(_filePath).Trim();
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }

        /// <inheritdoc/>
        public void SaveKey(string key)
        {
            File.WriteAllText(_filePath, key.Trim());
        }

        /// <inheritdoc/>
        public void ClearKey()
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
    }
}
