using System;
using System.IO;
using System.Text.Json;

namespace GoblineerNextUpdater
{
    public record Config(
        string BlizzardClientId,
        string BlizzardClientSecret,
        string BlizzardOAuthRegion,
        string ConnectionString
    )
    {
        public static Config LoadConfig(string filePath)
        {
            var data = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<Config>(data);
            if(config is null)
                throw new Exception("Config file is null.");

            return config;
        }
    }
}