using System.IO;
using System.Collections.Generic;

namespace EZPos.DataAccess.Repositories
{
    public static class ConfigHelper
    {
        private static Dictionary<string, string> config = null;
        private static string configPath = "config.ini";

        private static void LoadConfig()
        {
            config = new Dictionary<string, string>();
            if (!File.Exists(configPath)) return;
            foreach (var line in File.ReadAllLines(configPath))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("=")) continue;
                var parts = line.Split('=');
                config[parts[0].Trim()] = parts[1].Trim();
            }
        }

        public static string Get(string key, string defaultValue = "")
        {
            if (config == null) LoadConfig();
            if (config != null && config.ContainsKey(key))
                return config[key];
            return defaultValue;
        }
    }
}
