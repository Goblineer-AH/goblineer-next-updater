using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoblineerNextUpdater.Services
{
    public class ServerManager
    {
        public string Region { get; private set; }
        public string Realm { get; private set; }
        public string Locale { get; private set; }
        private readonly int connectedRealmId;
        private GoblineerService GoblineerService;

        public static async Task<ServerManager> BuildServerManagerAsync(GoblineerService goblineerService, string region, string realm, string locale)
        {
            int connectedRealmId = await goblineerService.GetConnectedRealmId(region, realm, locale);
            return new ServerManager(goblineerService, region, realm, locale, connectedRealmId);
        }

        private ServerManager(GoblineerService goblineerService, string region, string realm, string locale, int connectedRealmId)
        {
            GoblineerService = goblineerService;
            Region = region;
            Realm = realm;
            Locale = locale;
            this.connectedRealmId = connectedRealmId;
        }

        public async Task UpdateAuctionsEndlessLoop()
        {
            while(true)
            {
                var didUpdate = await GoblineerService.TryUpdateAuctions(Region, connectedRealmId, Locale);
                Console.WriteLine($"ServerManager({Region}, {Realm}) did update: {didUpdate}");
                Thread.Sleep(TimeSpan.FromMinutes(0.1));
            }
        }
    }
}