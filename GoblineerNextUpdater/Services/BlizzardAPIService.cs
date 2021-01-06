using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GoblineerNextUpdater.Models;

namespace GoblineerNextUpdater.Services
{
    public class NotModifiedException : Exception {}

    public class BlizzardAPIService
    {
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string oauthRegion;

        private string OAuthToken { get; set; } = "";

        public BlizzardAPIService(string clientId, string clientSecret, string oauthRegion)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.oauthRegion = oauthRegion;
        }

        public async Task<string> GetOAuthToken(string region)
        {
            string url = $"https://{region}.battle.net/oauth/token";

            using var client = new HttpClient();

            var authToken = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(authToken));
            
            var data = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await client.PostAsync(url, data);

            if(response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(content);
                var accessToken = document.RootElement.GetProperty("access_token").GetString() 
                    ?? throw new Exception("Cannot get access token from json.");

                return accessToken;
            }
            else
            {
                throw new Exception($"Getting OAuth token failed with code: {response.StatusCode}");
            }
        }

        private async Task CheckOAuthToken()
        {
            var url = $"https://{oauthRegion}.battle.net/oauth/check_token?token={OAuthToken}";

            using var client = new HttpClient();

            var response = await client.GetAsync(url);
            if(response.IsSuccessStatusCode)
            {
                return;
            }
            else if(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.BadRequest)
            {
                OAuthToken = await GetOAuthToken(oauthRegion);
            }
            else
            {
                throw new Exception($"GetAuthenticated failed with code: {response.StatusCode}, url: {response.RequestMessage?.RequestUri}");
            }
        }

        private async Task<(Stream, DateTimeOffset)> GetAuthenticated(string url, string region, DateTimeOffset lastUpdate)
        {
            await CheckOAuthToken();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OAuthToken);
            client.DefaultRequestHeaders.IfModifiedSince = lastUpdate;

            var response = await client.GetAsync(url);
            if(response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadAsStreamAsync(), response.Content.Headers.LastModified ?? DateTimeOffset.UnixEpoch);
            }
            else if(response.StatusCode == HttpStatusCode.NotModified)
            {
                throw new NotModifiedException();
            }
            else
            {
                throw new Exception($"GetAuthenticated failed with code: {response.StatusCode}, url: {response.RequestMessage?.RequestUri}");
            }
        }

        private async Task<Stream> GetAuthenticatedContentOnly(string url, string region) =>
            (await GetAuthenticated(url, region, DateTimeOffset.UnixEpoch)).Item1;

        private JsonElement GetJsonProperty(Stream content, params string[] properties)
        {
            using var document = JsonDocument.Parse(content);
            var elem = document.RootElement;

            foreach(var prop in properties)
            {
                elem = elem.GetProperty(prop);
            }

            return elem.Clone();
        }

        public async Task<(int, string)> GetConnectedRealmIdFromSlug(string region, string realmSlug, string locale)
        {
            string url = $"https://{region}.api.blizzard.com/data/wow/realm/{realmSlug}?namespace=dynamic-{region}&locale={locale}";

            var realmStream = await GetAuthenticatedContentOnly(url, region);
            using var realmDocument = JsonDocument.Parse(realmStream);

            var connectedRealmLink = realmDocument.RootElement.GetProperty("connected_realm").GetProperty("href").GetString()
                ?? throw new Exception("Cannot get connected realm link from json");
            string realmName = realmDocument.RootElement.GetProperty("name").GetString()
                ?? throw new Exception("Cannot get realm name from json.");


            var connectedRealmStream = await GetAuthenticatedContentOnly(connectedRealmLink, region);
            using var connectedRealmDocument = JsonDocument.Parse(connectedRealmStream);
            int connectedRealmId = connectedRealmDocument.RootElement.GetProperty("id").GetInt32();

            return (connectedRealmId, realmName);
        }

        public async Task<(List<Auction>, DateTimeOffset)> GetAuctions(string region, int connectedRealmId, string locale, DateTimeOffset lastUpdate)
        {
            string url = $"https://{region}.api.blizzard.com/data/wow/connected-realm/{connectedRealmId}/auctions?namespace=dynamic-{region}&locale={locale}";
            (var auctionContent, var updateTime) = await GetAuthenticated(url, region, lastUpdate);

            var auctionsDeserialized = await JsonSerializer.DeserializeAsync<AuctionResponse>(auctionContent)
                ?? throw new Exception("Deserialised auctions is null.");
            var auctions = auctionsDeserialized.Auctions;

            return (auctions, updateTime);
        }
    }
}