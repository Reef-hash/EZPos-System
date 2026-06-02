using System;
using System.Net;
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
    /// Primary endpoint: POST {baseUrl}/api/v1/licensing/validate
    /// Legacy fallback : POST {baseUrl}/api/licenses/validate
    ///
    /// During migration we prefer V1 contract and gracefully fall back to legacy.
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
        /// Calls V1 validate endpoint first, then falls back to legacy validate endpoint.
        /// Returns IsOffline = true when the server cannot be reached (no internet, server down, etc.).
        /// Never throws — all exceptions are caught and returned as IsOffline responses.
        /// </summary>
        public async Task<LicenseApiResponse> ValidateAsync(string key, string deviceId)
        {
            try
            {
                var v1 = await ValidateV1Async(key, deviceId);
                if (v1 is not null)
                    return v1;

                var legacy = await ValidateLegacyAsync(key, deviceId);
                return legacy ?? new LicenseApiResponse { IsValid = false, Message = "Validation response unavailable." };
            }
            catch (Exception ex) when (ex is HttpRequestException
                                           or TaskCanceledException
                                           or OperationCanceledException)
            {
                // Network unreachable, DNS failure, timeout, or server refused connection
                return new LicenseApiResponse { IsValid = false, IsOffline = true, Message = ex.Message };
            }
        }

        /// <summary>
        /// Calls V1 activate endpoint first. Falls back to validate flow if V1 activate is unavailable.
        /// </summary>
        public async Task<LicenseApiResponse> ActivateAsync(string key, string deviceId)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(
                    $"{_baseUrl}/api/v1/licensing/activate",
                    new { licenseKey = key, product = "ezpos", deviceId });

                if (response.StatusCode == HttpStatusCode.NotFound ||
                    response.StatusCode == HttpStatusCode.MethodNotAllowed)
                {
                    // Old backend: no activation endpoint yet, use validate as compatibility path.
                    return await ValidateAsync(key, deviceId);
                }

                if (!response.IsSuccessStatusCode)
                    return new LicenseApiResponse { IsValid = false, Message = "Server returned an error." };

                var parsed = await response.Content.ReadFromJsonAsync<LicenseApiResponse>();
                return parsed?.Normalize() ?? new LicenseApiResponse { IsValid = false, Message = "Activation response unavailable." };
            }
            catch (Exception ex) when (ex is HttpRequestException
                                           or TaskCanceledException
                                           or OperationCanceledException)
            {
                return new LicenseApiResponse { IsValid = false, IsOffline = true, Message = ex.Message };
            }
        }

        private async Task<LicenseApiResponse?> ValidateV1Async(string key, string deviceId)
        {
            var response = await _http.PostAsJsonAsync(
                $"{_baseUrl}/api/v1/licensing/validate",
                new { licenseKey = key, product = "ezpos", deviceId });

            if (response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
                return new LicenseApiResponse { IsValid = false, Message = "Server returned an error." };

            var parsed = await response.Content.ReadFromJsonAsync<LicenseApiResponse>();
            return parsed?.Normalize() ?? new LicenseApiResponse { IsValid = false, Message = "Validation response unavailable." };
        }

        private async Task<LicenseApiResponse?> ValidateLegacyAsync(string key, string deviceId)
        {
            var response = await _http.PostAsJsonAsync(
                $"{_baseUrl}/api/licenses/validate",
                new { LicenseKey = key, DeviceId = deviceId, Product = "ezpos" });

            if (!response.IsSuccessStatusCode)
                return new LicenseApiResponse { IsValid = false, Message = "Server returned an error." };

            var parsed = await response.Content.ReadFromJsonAsync<LicenseApiResponse>();
            return parsed?.Normalize() ?? new LicenseApiResponse { IsValid = false, Message = "Legacy validation response unavailable." };
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

        /// <summary>Fallback error field returned by V1 responses.</summary>
        public string Error { get; set; } = string.Empty;

        // V1 contract fields
        public string Decision { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReasonCode { get; set; } = string.Empty;
        public string ClientAction { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }

        public LicenseApiResponse Normalize()
        {
            if (!string.IsNullOrWhiteSpace(Status))
            {
                var s = Status.Trim().ToLowerInvariant();
                IsValid = s == "valid" || Decision.Equals("allow", StringComparison.OrdinalIgnoreCase);
            }

            if (string.IsNullOrWhiteSpace(Message) && !string.IsNullOrWhiteSpace(Error))
            {
                Message = Error;
            }

            if (string.IsNullOrWhiteSpace(Message))
            {
                Message = Status?.Trim().ToLowerInvariant() switch
                {
                    "device_mismatch" => "This key is already bound to another device. Request a transfer from support.",
                    "seat_exceeded" => "Seat/device limit reached. Release another device or upgrade your plan.",
                    "expired" => "This license key has expired. Please renew your subscription.",
                    "revoked" => "This license has been revoked. Please contact support.",
                    "product_mismatch" => "This key is for a different product. Please use an EZPos license key.",
                    "not_found" => "License key not found. Please check your key and try again.",
                    _ => IsValid ? "License is valid." : "License validation failed.",
                };
            }

            return this;
        }
    }
}

