using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoblineerNextUpdater.Models;

namespace GoblineerNextUpdater.Services
{
    public class GoblineerService
    {
        private BlizzardAPIService ApiService { get; set; }
        private DbService Db { get; set; }

        public GoblineerService(BlizzardAPIService apiService, DbService db)
        {
            ApiService = apiService;
            Db = db;
        }

        private async Task<List<(int, int, double)>> CalculateMarketvaluesForServer(int serverId)
        {
            var auctionData = await Db.GetAuctionsForMarketvalues(serverId);
            
            int prevItemId = -1;
            var itemsData = new List<(int, long)>();
            var mvData = new List<(int, int, double)>();
            foreach((int itemId, int quantity, long price) in auctionData)
            {
                if(itemId != prevItemId && prevItemId != -1)
                {
                    (int totalQuantity, double marketvalue) = CalculateMarketValue(itemsData, isOrdered: true);
                    mvData.Add((prevItemId, totalQuantity, marketvalue));
                    itemsData.Clear();
                }

                itemsData.Add((quantity, price));
                prevItemId = itemId;
            }

            return mvData;
        }

        public async Task<bool> TryUpdateAuctions(string region, int connectedRealmId, string locale)
        {
            try {
                var lastAuctionUpdate = await Db.GetLastUpdate(connectedRealmId);
                (var auctions, var lastUpdate) = await ApiService.GetAuctions(region, connectedRealmId, locale, lastAuctionUpdate);

                await Db.UpdateServerLastUpdate(connectedRealmId, lastUpdate); 

                var items = auctions.Select(auc => auc.Item).ToList();

                await Db.InsertItemsBatched(items);
                await Db.InsertAuctionsBatched(auctions, connectedRealmId);

                var marketvalues = await CalculateMarketvaluesForServer(connectedRealmId);

                await Db.InsertMarketvaluesBatched(marketvalues, lastUpdate, connectedRealmId);

                return true;
            }
            catch(NotModifiedException)
            {
                return false;
            }
        }

        public (int, double) CalculateMarketValue(IEnumerable<(int, long)> listings, bool isOrdered = false)
        {
            if(!isOrdered)
            {
                listings = listings.OrderBy(e => e.Item2);
            }

            var prices = new List<long>();
            foreach((var quantity, var price) in listings)
            {
                prices.AddRange(Enumerable.Repeat(price, quantity));
            }

            var overallQuantity = prices.Count();

            // If overallQuantity is < 4 that brakePoint will always be 0, so the first item's price will be the marketvalue
            // The stepping starts at 15% and if from one item to the next the price increase is > 20% that the loop stops
            int brakePoint;
            for(brakePoint = overallQuantity * 15 / 100; brakePoint < overallQuantity * 30 / 100; ++brakePoint)
            {
                if(prices[brakePoint] * 20 / 100 > prices[brakePoint + 1]) break;
            }

            var calculatingPrices = prices.Take(brakePoint + 1).ToList();

            var stdDev = calculatingPrices.StandardDeviation();
            var avg = calculatingPrices.Average();
            var cutoffLow = avg - stdDev * 1.5;
            var cutoffHigh = avg + stdDev * 1.5;

            calculatingPrices.RemoveAll(price => price < cutoffLow || price > cutoffHigh);

            var marketValue = calculatingPrices.Average();

            return (overallQuantity, marketValue);
        }

        public async Task<int> GetConnectedRealmId(string region, string realm, string locale)
        {
            var serverIdFromDb = await Db.GetServerId(region, realm);
            int serverId = serverIdFromDb ?? 0;
            if(serverIdFromDb == null)
            {
                serverId = await ApiService.GetConnectedRealmIdFromSlug(region, realm, locale);
                await Db.InsertServerAsync(serverId, region, realm, DateTimeOffset.UnixEpoch);
            }

            return serverId;
        }
    }
    public static class Extend
    {
        public static double StandardDeviation(this IEnumerable<long> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v=>Math.Pow(v-avg,2)));
        }
    }
}