using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoblineerNextUpdater.Services;

namespace GoblineerNextUpdater
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var config = Config.LoadConfig("secrets.json");

            var blizzardAPIService = new BlizzardAPIService(config.BlizzardClientId, config.BlizzardClientSecret, config.BlizzardOAuthRegion);
            var dbService = new DbService(config.ConnectionString);
            var goblineerService = new GoblineerService(blizzardAPIService, dbService);

            // await TestMain3(blizzardAPIService, dbService, goblineerService);
            await TestMain4(blizzardAPIService, dbService, goblineerService);
        }

        public static async Task TestMain4(BlizzardAPIService blizzardAPIService, DbService dbService, GoblineerService goblineerService)
        {
            // await dbService.DropTables();
            await dbService.InitialseDatabase();
            await dbService.TruncateTables();

            var servers = new List<(string,string)>
            {
                // ("eu", "tarren-mill"),
                // ("eu", "howling-fjord"),
                ("eu", "ragnaros"),
                // ("eu", "kazzak"),
                // ("eu", "draenor"),
                ("eu", "argent-dawn"),
                ("eu", "twisting-nether"),
                ("eu", "aegwynn"),
                ("eu", "aerie-peak"),
                ("eu", "agamaggan"),
                ("eu", "aggramar"),
                // ("eu", "alexstrasza"),
                // ("eu", "alleria"),
                // ("eu", "alonsus"),
                // ("eu", "ambossar"),
                // ("eu", "anachronos"),
                // ("eu", "anetheron"),
                // ("eu", "antonidas"),
                // ("eu", "arathi"),
                // ("eu", "arathor"),
                // ("eu", "archimonde"),
            };

            var serversWithId = servers.Select(((string region, string realm) x) => 
                    (x.region, goblineerService.GetConnectedRealmId(x.region, x.realm, "en_GB").Result)
                )
                .Distinct()
                .ToList();

            while(true)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                var tasks = new List<Task>();
                foreach((string region, int connectedRealmId) in serversWithId)
                {
                    tasks.Add(UpdateRealm(goblineerService, region, connectedRealmId));
                }
                Task.WaitAll(tasks.ToArray());

                stopwatch.Stop();
                var timeout = TimeSpan.FromMinutes(1) - stopwatch.Elapsed;
                timeout = timeout.Ticks < 0 ? TimeSpan.FromTicks(0) : timeout;

                Console.WriteLine($"Sleeping from {timeout}");
                Thread.Sleep(timeout);
            }
        }

        public static async Task UpdateRealm(GoblineerService goblineerService, string region, int connectedRealmId)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Console.WriteLine($"Started updating: {connectedRealmId}");
            var didUpdate = await goblineerService.TryUpdateAuctions(region, connectedRealmId, "en_GB");

            stopwatch.Stop();
            Console.WriteLine($"Finished updating: {connectedRealmId} - Did update: {didUpdate} - Time: {stopwatch.Elapsed}");

        }
    }
}
