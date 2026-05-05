using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace EZPos.DataAccess.Repositories
{
    public static class ConfigHelper
    {
        private static Dictionary<string, string>? config = null;
        private static readonly string configPath = "Config\\config.ini";

        private static void LoadConfig()
        {
            config = new Dictionary<string, string>();
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
            Save();
        }

        private static void Save()
        {
            if (config == null) return;
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllLines(configPath, config.Select(kv => $"{kv.Key}={kv.Value}"));
        }
    }
}
