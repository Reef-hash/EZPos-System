using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EZPos.Models.Domain;

namespace EZPos.Business.Services
{
    /// <summary>
    /// Service for checking and downloading updates.
    /// 
    /// Update Flow:
    /// 1. CheckForUpdatesAsync() fetches manifest from hosted URL
    /// 2. Compares remote version with local AppVersion
    /// 3. Returns manifest if update available
    /// 4. User clicks "Update Now" (in UpdateAvailableDialog)
    /// 5. DownloadInstallerAsync() downloads .exe and verifies checksum
    /// 6. App backs up DB and exits
    /// 7. Installer runs silently (via InnoSetup)
    /// 8. App restarts with updated binaries + preserved DB
    /// </summary>
    public class UpdaterService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        /// <summary>
        /// Hosted manifest URL. Set via config or environment.
        /// Default: null (disables updates)
        /// Example: https://updates.ezpos.my/latest.json
        /// </summary>
        public string? ManifestUrl { get; set; }
        
        /// <summary>
        /// Current app version. Read from assembly version or hardcoded.
        /// Example: "1.0.0"
        /// </summary>
        public string AppVersion { get; set; }

        public UpdaterService(string appVersion, string? manifestUrl = null)
        {
            AppVersion = appVersion;
            ManifestUrl = manifestUrl;
            
            // Set reasonable HTTP client defaults
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Check if an update is available by fetching manifest from remote endpoint.
        /// </summary>
        /// <returns>
        /// UpdateManifest if newer version exists, null if up-to-date or error occurs.
        /// </returns>
        public async Task<UpdateManifest?> CheckForUpdatesAsync()
        {
            try
            {
                // Manifest URL is required
                if (string.IsNullOrWhiteSpace(ManifestUrl))
                {
                    return null;
                }

                // Fetch manifest JSON
                var response = await _httpClient.GetAsync(ManifestUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, options);
                if (manifest?.Version == null)
                {
                    return null;
                }

                // Check if remote version is newer than local
                if (!IsVersionNewer(manifest.Version, AppVersion))
                {
                    return null;
                }

                // Check if local version is below minimum required version
                if (!string.IsNullOrEmpty(manifest.MinimumVersion))
                {
                    if (IsVersionOlder(AppVersion, manifest.MinimumVersion))
                    {
                        // Local version is too old; mark as mandatory
                        manifest.Mandatory = true;
                    }
                }

                return manifest;
            }
            catch (Exception ex)
            {
                // Log but don't throw; update check failures should not crash the app
                System.Diagnostics.Debug.WriteLine($"UpdaterService.CheckForUpdatesAsync error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Download installer from URL and verify checksum.
        /// </summary>
        /// <param name="downloadUrl">Full URL to installer .exe</param>
        /// <param name="checksumAlgorithm">e.g., "sha256"</param>
        /// <param name="checksumValue">Expected checksum hex string</param>
        /// <param name="destinationPath">Where to save .exe</param>
        /// <returns>True if download and checksum verify, false otherwise</returns>
        public async Task<bool> DownloadInstallerAsync(
            string downloadUrl, 
            string? checksumAlgorithm, 
            string? checksumValue,
            string destinationPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(downloadUrl))
                {
                    return false;
                }

                // Download to destination
                var response = await _httpClient.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var content = await response.Content.ReadAsByteArrayAsync();
                
                // Verify checksum if provided
                if (!string.IsNullOrEmpty(checksumValue))
                {
                    if (!VerifyChecksum(content, checksumAlgorithm ?? "sha256", checksumValue))
                    {
                        return false;
                    }
                }

                // Write to destination
                File.WriteAllBytes(destinationPath, content);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdaterService.DownloadInstallerAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Compare two semantic versions.
        /// Returns true if remoteVersion &gt; localVersion.
        /// </summary>
        private static bool IsVersionNewer(string remoteVersion, string localVersion)
        {
            if (!System.Version.TryParse(remoteVersion, out var remote) ||
                !System.Version.TryParse(localVersion, out var local))
            {
                return false;
            }

            return remote.CompareTo(local) > 0;
        }

        /// <summary>
        /// Returns true if testVersion is older than minimumVersion.
        /// </summary>
        private static bool IsVersionOlder(string testVersion, string minimumVersion)
        {
            if (!System.Version.TryParse(testVersion, out var test) ||
                !System.Version.TryParse(minimumVersion, out var minimum))
            {
                return false;
            }

            return test.CompareTo(minimum) < 0;
        }

        /// <summary>
        /// Verify downloaded file checksum.
        /// </summary>
        private static bool VerifyChecksum(byte[] data, string algorithm, string expectedHex)
        {
            try
            {
                string actualHex;
                
                switch (algorithm.ToLower())
                {
                    case "sha256":
                        using (var sha256 = SHA256.Create())
                        {
                            var hash = sha256.ComputeHash(data);
                            actualHex = BitConverter.ToString(hash).Replace("-", "").ToLower();
                        }
                        break;

                    case "sha1":
                        using (var sha1 = SHA1.Create())
                        {
                            var hash = sha1.ComputeHash(data);
                            actualHex = BitConverter.ToString(hash).Replace("-", "").ToLower();
                        }
                        break;

                    case "md5":
                        using (var md5 = MD5.Create())
                        {
                            var hash = md5.ComputeHash(data);
                            actualHex = BitConverter.ToString(hash).Replace("-", "").ToLower();
                        }
                        break;

                    default:
                        return false; // Unsupported algorithm
                }

                return actualHex.Equals(expectedHex.ToLower(), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
