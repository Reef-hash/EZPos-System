using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Windows.Input;

namespace EZPos.DataAccess.Repositories
{
    public static class ConfigHelper
    {
        private static Dictionary<string, string>? config = null;
        
        /// <summary>
        /// Config file path: %ProgramData%\EZPos\config.ini
        /// Initialized on first access and creates folder if needed.
        /// </summary>
        private static string GetConfigPath()
        {
            var programDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EZPos");
            if (!Directory.Exists(programDataDir))
                Directory.CreateDirectory(programDataDir);
            return Path.Combine(programDataDir, "config.ini");
        }

        private static void LoadConfig()
        {
            config = new Dictionary<string, string>();
            var configPath = GetConfigPath();
            if (!File.Exists(configPath)) return;
            foreach (var line in File.ReadAllLines(configPath))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("=")) continue;
                var idx = line.IndexOf('=');
                var key = line[..idx].Trim();
                var val = line[(idx + 1)..].Trim();
                config[key] = val;
            }
        }

        public static string Get(string key, string defaultValue = "")
        {
            if (config == null) LoadConfig();
            if (config != null && config.ContainsKey(key))
                return config[key];
            return defaultValue;
        }

        public static void Set(string key, string value)
        {
            if (config == null) LoadConfig();
            config ??= new Dictionary<string, string>();
            config[key] = value;
            SaveConfig();
        }

        public static Key GetKey(string key, string defaultValue)
        {
            var configured = Get(key, defaultValue);
            var fallback = ParseKey(defaultValue, Key.None);
            return ParseKey(configured, fallback);
        }

        public static void SetKey(string key, Key value)
        {
            Set(key, value.ToString());
        }

        private static Key ParseKey(string raw, Key fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            var normalized = raw.Trim();

            if (normalized.Equals("PgUp", StringComparison.OrdinalIgnoreCase))
                return Key.PageUp;
            if (normalized.Equals("PgDn", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("PgDown", StringComparison.OrdinalIgnoreCase))
                return Key.PageDown;

            if (Enum.TryParse<Key>(normalized, true, out var parsed))
                return parsed;

            var converter = new KeyConverter();
            var converted = converter.ConvertFromString(normalized);
            if (converted is Key key)
                return key;

            return fallback;
        }

        private static void SaveConfig()
        {
            if (config == null) return;
            var configPath = GetConfigPath();
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllLines(configPath, config.Select(kv => $"{kv.Key}={kv.Value}"));
        }
    }
}
