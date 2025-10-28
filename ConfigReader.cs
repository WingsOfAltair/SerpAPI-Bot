using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerpAPI_Bot
{
    public static class ConfigReader
    {
        private static Dictionary<string, string> _config = new Dictionary<string, string>();

        public static void Load(string filePath = "config.txt")
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Config file not found: {filePath}");

            foreach (var line in File.ReadAllLines(filePath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                var parts = trimmed.Split('=', 2);
                if (parts.Length != 2) continue;
                _config[parts[0].Trim()] = parts[1].Trim();
            }
        }

        public static string Get(string key, string defaultValue = "")
        {
            return _config.ContainsKey(key) ? _config[key] : defaultValue;
        }
    }
}
