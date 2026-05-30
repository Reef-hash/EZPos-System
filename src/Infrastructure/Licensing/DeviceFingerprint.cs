using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace EZPos.Infrastructure.Licensing
{
    /// <summary>
    /// Generates a stable, anonymous device identifier used for license key binding.
    ///
    /// PURPOSE:
    ///   Prevents a customer from sharing their license key with other machines.
    ///   The first machine that activates a key "owns" it. Any other machine
    ///   that tries to use the same key will be rejected by the server.
    ///
    /// ALGORITHM:
    ///   1. Enumerate all UP, non-loopback, non-tunnel network interfaces.
    ///   2. Sort by name for deterministic ordering.
    ///   3. Take the MAC address of the first interface.
    ///   4. SHA256(MAC bytes) → take first 16 hex chars.
    ///
    /// PROPERTIES:
    ///   - Stable across reboots and reinstalls (same NIC = same ID).
    ///   - Hashed — actual MAC address is never sent to the server.
    ///   - If the NIC changes (hardware replacement), device ID changes.
    ///     → Admin panel "Reset Device" button allows transfer to new machine.
    ///
    /// EXAMPLE OUTPUT: "A3F9C2B811E40D7F"
    /// </summary>
    public static class DeviceFingerprint
    {
        private static string? _cached;

        public static string GetDeviceId()
        {
            if (_cached is not null) return _cached;

            var mac  = GetPrimaryMacAddress();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(mac));
            _cached  = Convert.ToHexString(hash)[..16];
            return _cached;
        }

        private static string GetPrimaryMacAddress()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                         && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderBy(n => n.Name)
                .Select(n => n.GetPhysicalAddress().ToString())
                .FirstOrDefault(m => m.Length > 0)
                ?? "UNKNOWN-DEVICE";
        }
    }
}
