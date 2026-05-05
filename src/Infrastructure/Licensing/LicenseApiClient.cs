// ═══════════════════════════════════════════════════════════════════════════
// PLACEHOLDER — Infrastructure/Licensing/LicenseApiClient.cs
// ═══════════════════════════════════════════════════════════════════════════
//
// PURPOSE:
//   This file is a structural placeholder for the future HTTP client that will
//   communicate with the EZPos licensing backend (Stripe + license key API).
//
// WHEN BACKEND IS READY:
//   1. Uncomment the class below.
//   2. Add System.Net.Http and (optionally) System.Text.Json NuGet packages.
//   3. Implement ValidateAsync and ActivateAsync against the real API endpoints.
//   4. Inject LicenseApiClient into LicenseService (Core/Licensing/LicenseService.cs).
//   5. Replace the MOCK blocks in LicenseService with real API calls.
//
// PLANNED API ENDPOINTS:
//   POST /api/v1/licenses/validate    — check if a key is active + not expired
//   POST /api/v1/licenses/activate    — bind a key to a device
//   POST /api/v1/licenses/deactivate  — release a device slot
//
// AUTHENTICATION: Bearer token or HMAC-signed request (TBD by backend team).
//
// ═══════════════════════════════════════════════════════════════════════════

namespace EZPos.Infrastructure.Licensing
{
    // TODO: uncomment and implement when Stripe + license key backend is ready.

    /*
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using EZPos.Core.Licensing;

    public class LicenseApiClient
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "https://api.ezpos.io/v1";   // TODO: move to config

        public LicenseApiClient(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Validates an existing key against the backend.
        /// Returns LicenseStatus.Valid, Expired, Invalid, or NotActivated.
        /// </summary>
        public async Task<LicenseApiResponse> ValidateAsync(string key, string deviceId)
        {
            var payload = new { Key = key, DeviceId = deviceId };
            var response = await _http.PostAsJsonAsync($"{BaseUrl}/licenses/validate", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LicenseApiResponse>()
                   ?? new LicenseApiResponse { Status = "invalid" };
        }

        /// <summary>
        /// Activates a key for this device. Call once on first use.
        /// </summary>
        public async Task<LicenseApiResponse> ActivateAsync(string key, string deviceId)
        {
            var payload = new { Key = key, DeviceId = deviceId };
            var response = await _http.PostAsJsonAsync($"{BaseUrl}/licenses/activate", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LicenseApiResponse>()
                   ?? new LicenseApiResponse { Status = "invalid" };
        }

        /// <summary>
        /// Releases this device's activation slot (useful on uninstall).
        /// </summary>
        public async Task DeactivateAsync(string key, string deviceId)
        {
            var payload = new { Key = key, DeviceId = deviceId };
            await _http.PostAsJsonAsync($"{BaseUrl}/licenses/deactivate", payload);
        }
    }

    /// <summary>Deserialized shape of the API JSON response.</summary>
    public class LicenseApiResponse
    {
        public string    Status      { get; set; } = string.Empty;  // "valid"|"invalid"|"expired"|"not_activated"
        public string?   PlanName    { get; set; }
        public string?   ExpiryDate  { get; set; }                  // ISO 8601
        public bool      Success     { get; set; }
        public string?   Message     { get; set; }
    }
    */
}
