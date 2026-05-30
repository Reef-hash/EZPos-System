using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EZPos.Core.Licensing;

namespace EZPos.Infrastructure.Licensing
{
    /// <summary>
    /// HTTP client that calls the EZPos Web API to validate license keys.
    ///
    /// Base URL is read from config.ini: App:LicenseApiUrl
    ///   Development : http://localhost:5122
    ///   Production  : https://your-deployed-site.com  (update config.ini before release)
    ///
    /// Endpoint called: POST {baseUrl}/api/licenses/validate
    /// Request body   : { "licenseKey": "EZPOS-XXXX-XXXX-XXXX" }
    /// Response       : { "isValid": true|false, "message": "..." }
    /// </summary>
    public class LicenseApiClient
    {
        // Single shared HttpClient — reused across all calls (recommended practice)
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        private readonly string _baseUrl;

        public LicenseApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        /// <summary>
        /// Calls POST /api/licenses/validate.
        /// Returns IsOffline = true when the server cannot be reached (no internet, server down, etc.).
        /// Never throws — all exceptions are caught and returned as IsOffline responses.
        /// </summary>
        public async Task<LicenseApiResponse> ValidateAsync(string key, string deviceId)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(
                    $"{_baseUrl}/api/licenses/validate",
                    new { LicenseKey = key, DeviceId = deviceId });

                if (!response.IsSuccessStatusCode)
                    return new LicenseApiResponse { IsValid = false, Message = "Server returned an error." };

                return await response.Content.ReadFromJsonAsync<LicenseApiResponse>()
                       ?? new LicenseApiResponse { IsValid = false };
            }
            catch (Exception ex) when (ex is HttpRequestException
                                           or TaskCanceledException
                                           or OperationCanceledException)
            {
                // Network unreachable, DNS failure, timeout, or server refused connection
                return new LicenseApiResponse { IsValid = false, IsOffline = true, Message = ex.Message };
            }
        }
    }

    /// <summary>Deserialized JSON response from POST /api/licenses/validate.</summary>
    public sealed class LicenseApiResponse
    {
        /// <summary>True when the key exists in the database and IsActive = true.</summary>
        public bool IsValid { get; set; }

        /// <summary>True when the request failed due to a network error — NOT an invalid key.</summary>
        public bool IsOffline { get; set; }

        /// <summary>Human-readable message from the server (or error description if offline).</summary>
        public string Message { get; set; } = string.Empty;
    }
}

