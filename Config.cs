using System.Runtime.Serialization;
using System.IO;
using System.Text.Json;

namespace GoblineerNextUpdater
{
    public class Config
    {
        public string BlizzardClientId { get; set; } = "";
        public string BlizzardClientSecret { get; set; } = "";
        public string BlizzardOAuthRegion { get; set; } = "";
        public string ConnectionString { get; set; } = "";

        public static Config LoadConfig(string filePath)
        {
            var data = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<Config>(data);

            return config;
        }
    }
}