using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

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
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("secrets.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var blizzardClientId = configuration.GetValue<string>("BLIZZARD_CLIENT_ID");
            var blizzardClientSecret = configuration.GetValue<string>("BLIZZARD_CLIENT_SECRET");
            var blizzardOAuthRegion = configuration.GetValue<string>("BLIZZARD_OAUTH_REGION");
            var connectionString = ReadConnectionString(configuration);

            return new(blizzardClientId, blizzardClientSecret, blizzardOAuthRegion, connectionString);
        }

        private static string ReadConnectionString(IConfiguration configuration)
        {
            var host     = configuration.GetValue<string>("DB_HOST");
            var username = configuration.GetValue<string>("DB_USERNAME");
            var password = configuration.GetValue<string>("DB_PASSWORD");
            var database = configuration.GetValue<string>("DB_DATABASE");

            var connString = $"Host={host};Username={username};Password={password};Database={database};Pooling=true;";

            return connString;
        }
    }
}