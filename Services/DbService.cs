using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoblineerNextUpdater.Models;
using Npgsql;
using System.Text;

namespace GoblineerNextUpdater.Services
{
    public class ServerNotFoundException : Exception
    {
        public ServerNotFoundException(string msg) : base(msg) {}
    }
    public static class Extensions
    {
        public static NpgsqlParameter AddWithNullableValue(
            this NpgsqlParameterCollection collection,
            string parameterName,
            object? value)
        {
            if(value == null)
                return collection.AddWithValue(parameterName, DBNull.Value);
            else
                return collection.AddWithValue(parameterName, value);
        }

        public static int? GetNullableOrdinalInt(this NpgsqlDataReader reader, string columnName) =>
            reader.IsDBNull(reader.GetOrdinal(columnName)) ? (int?)null : reader.GetInt32(reader.GetOrdinal(columnName));
    }
    public class DbService
    {
        private readonly string connString;
        private static readonly int defaultIntValue = -1;
        private static readonly List<int> defaultIntArrayValue = new List<int>(0);

        public DbService(string connString)
        {
            this.connString = connString;
        }

        private async Task<NpgsqlConnection> OpenNewConnection()
        {
            var connection = new NpgsqlConnection(connString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task InsertServerAsync(int connectedrealmid, string region, string realm, string realmName, DateTimeOffset lastUpdate)
        {
            var query = @"
                INSERT INTO servers (connectedrealmid, region, realm, realmname, lastupdate)
                VALUES (@connectedrealmid, @region, @realm, @realmname, @lastupdate);
            ";

            await using var connection = await OpenNewConnection();
            await using var cmd = new NpgsqlCommand(query, connection);

            cmd.Parameters.AddWithValue("connectedrealmid", connectedrealmid);
            cmd.Parameters.AddWithValue("region", region);
            cmd.Parameters.AddWithValue("realm", realm);
            cmd.Parameters.AddWithValue("realmname", realmName);
            cmd.Parameters.AddWithValue("lastupdate", lastUpdate);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int?> GetServerId(string region, string realm)
        {
            var query = @"
                SELECT connectedrealmid FROM servers
                WHERE region = @region AND realm = @realm;
            ";

            await using var connection = await OpenNewConnection();
            await using var cmd = new NpgsqlCommand(query, connection);

            cmd.Parameters.AddWithValue("region", region);
            cmd.Parameters.AddWithValue("realm", realm);

            await using var reader = await cmd.ExecuteReaderAsync();

            int? id = null;
            if(await reader.ReadAsync())
            {
                id = reader.GetInt32(0);
            }
            
            return id;
        }

        public async Task<DateTimeOffset> GetLastUpdate(int connectedRealmId)
        {
            var query = @"
                SELECT lastupdate FROM servers
                WHERE connectedrealmid = @connectedrealmid;
            ";

            await using var connection = await OpenNewConnection();
            await using var cmd = new NpgsqlCommand(query, connection);

            cmd.Parameters.AddWithValue("connectedrealmid", connectedRealmId);

            await using var reader = await cmd.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                var lastUpdate = reader.GetDateTime(0);
                DateTimeOffset lastUpdateUTC = lastUpdate.ToUniversalTime();
                return lastUpdateUTC;
            }
            
            throw new ServerNotFoundException($"GetLastUpdate: Server with id={connectedRealmId} not found");
        }

        public async Task<List<(int, int, long)>> GetAuctionsForMarketvalues(int serverId)
        {
            var query = @"
                SELECT itemid, quantity, price FROM auctions
                WHERE serverId = @serverId
                ORDER BY itemid asc, price asc;
            ";

            await using var connection = await OpenNewConnection();
            await using var cmd = new NpgsqlCommand(query, connection);

            cmd.Parameters.AddWithValue("serverId", serverId);

            await using var reader = await cmd.ExecuteReaderAsync();

            var result = new List<(int, int, long)>();
            while(await reader.ReadAsync())
            {
                var itemId = reader.GetInt32(0);
                var quantity = reader.GetInt32(1);
                var price = reader.GetInt64(2);
                result.Add((itemId, quantity, price));
            }

            return result;
        }

        private async Task InsertItems(List<Item> items)
        {

            var queryStringBuilder = new StringBuilder();
            for(int i = 0; i < items.Count; ++i)
            {
                var insertItemQuery = $@"
                    INSERT INTO items (originalitemid, context, modifiers, bonuses, petbreedid, petlevel, petqualityid, petspeciesid)
                    VALUES (@itemid{i}, @context{i}, @modifiers{i}, @bonuses{i}, @petbreedid{i}, @petlevel{i}, @petqualityid{i}, @petspeciesid{i})
                    ON CONFLICT DO NOTHING;
                ";

                queryStringBuilder.Append(insertItemQuery);
            }

            await using var connection = await OpenNewConnection();
            await using var cmd = new NpgsqlCommand(queryStringBuilder.ToString(), connection);

            for(int i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                List<int>? modifiers = item.Modifiers?.SelectMany(mod => new int[] { mod.Type, mod.Value }).ToList();

                cmd.Parameters.AddWithValue($"@itemid{i}", item.Id);
                cmd.Parameters.AddWithValue($"@context{i}", item.Context ?? defaultIntValue);
                cmd.Parameters.AddWithValue($"@modifiers{i}", modifiers ?? defaultIntArrayValue);
                cmd.Parameters.AddWithValue($"@bonuses{i}", item.BonusLists ?? defaultIntArrayValue);
                cmd.Parameters.AddWithValue($"@petbreedid{i}", item.PetBreedId ?? defaultIntValue);
                cmd.Parameters.AddWithValue($"@petlevel{i}", item.PetLevel ?? defaultIntValue);
                cmd.Parameters.AddWithValue($"@petqualityid{i}", item.PetQualityId ?? defaultIntValue);
                cmd.Parameters.AddWithValue($"@petspeciesid{i}", item.PetSpeciesId ?? defaultIntValue);
            }

            cmd.Prepare();

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertAuctions(List<Auction> auctions, int serverId)
        {
            var queryStringBuilder = new StringBuilder();
            for(int i = 0; i < auctions.Count; ++i)
            {
                var selectItemIdQuery = $@"
                    SELECT id FROM items 
                    WHERE originalItemId = @originalitemid{i}
                      AND context = @context{i}
                      AND modifiers = @modifiers{i}
                      AND bonuses = @bonuses{i}
                      AND petbreedid = @petbreedid{i}
                      AND petlevel = @petlevel{i}
                      AND petqualityid = @petqualityid{i}
                      AND petspeciesid = @petspeciesid{i}
                ";

                var insertAuctionQuery = $@"
                    INSERT INTO auctions (serverId, auctionId, itemId, bid, price, quantity, timeLeft)
                    VALUES (@serverId, @auctionId{i}, ({selectItemIdQuery}), @bid{i}, @price{i}, @quantity{i}, @timeLeft{i});
                ";

                queryStringBuilder.Append(insertAuctionQuery);
            }

            await using var connection = await OpenNewConnection();
            await using var cmd = new NpgsqlCommand(queryStringBuilder.ToString(), connection);
            cmd.Parameters.AddWithValue("@serverId", serverId);

            for(int i = 0; i < auctions.Count; ++i)
            {
                var auc = auctions[i];
                var item = auc.Item;
                List<int>? modifiers = item.Modifiers?.SelectMany(mod => new int[] { mod.Type, mod.Value }).ToList();

                cmd.Parameters.AddWithValue($"@auctionId{i}", auc.Id);
                cmd.Parameters.AddWithValue($"@bid{i}", auc.Bid);
                cmd.Parameters.AddWithValue($"@price{i}", auc.QuantityAndPrice.Item2);
                cmd.Parameters.AddWithValue($"@quantity{i}", auc.Quantity);
                cmd.Parameters.AddWithValue($"@timeLeft{i}", auc.TimeLeft);

                cmd.Parameters.AddWithValue($"@originalitemid{i}", item.Id);
                cmd.Parameters.AddWithValue($"@context{i}", item.Context ?? defaultIntValue);
                cmd.Parameters.AddWithValue($"@modifiers{i}", modifiers ?? defaultIntArrayValue);
                cmd.Parameters.AddWithValue($"@bonuses{i}", item.BonusLists ?? defaultIntArrayValue);
                cmd.Parameters.AddWithValue($"@petbreedid{i}", item.PetBreedId ?? defaultIntValue);
                cmd.Parameters.AddWithValue($"@petlevel{i}", item.PetLevel ?? defaultIntValue);
                cmd.Parameters.AddWithValue($"@petqualityid{i}", item.PetQualityId ?? defaultIntValue);
                cmd.Parameters.AddWithValue($"@petspeciesid{i}", item.PetSpeciesId ?? defaultIntValue);
            }

            cmd.Prepare();

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertMarketvalues(List<(int, int, double)> marketvalues, DateTimeOffset time, int serverId)
        {
            var queryStringBuilder = new StringBuilder();
            for(int i = 0; i < marketvalues.Count; ++i)
            {
                var insertMarketvalueQuery = $@"
                    INSERT INTO marketvalues (serverId, itemId, marketvalue, quantity, time)
                    VALUES (@serverId, @itemId{i}, @marketvalue{i}, @quantity{i}, @time);
                ";

                queryStringBuilder.Append(insertMarketvalueQuery);
            }

            await using var connection = await OpenNewConnection();
            await using var cmd = new NpgsqlCommand(queryStringBuilder.ToString(), connection);
            cmd.Parameters.AddWithValue("serverId", serverId);
            cmd.Parameters.AddWithValue("time", time);

            for(int i = 0; i < marketvalues.Count; ++i)
            {
                (int itemId, int quantity, double marketvalue) = marketvalues[i];

                cmd.Parameters.AddWithValue($"itemId{i}", itemId);
                cmd.Parameters.AddWithValue($"marketvalue{i}", marketvalue);
                cmd.Parameters.AddWithValue($"quantity{i}", quantity);
            }

            cmd.Prepare();

            await cmd.ExecuteNonQueryAsync();

            await connection.CloseAsync();
        }

        private async Task InsertBatches<T>(Func<List<T>,Task> dbInsertFunction, List<T> items, int chunkSize)
        {
            for(int i = 0; i <= items.Count / chunkSize; ++i)
            {
                var itemChunk = items.Skip(i * chunkSize).Take(chunkSize).ToList();
                await dbInsertFunction(itemChunk);
            }
        }

        public async Task InsertItemsBatched(List<Item> items) =>
            await InsertBatches<Item>(InsertItems, items, 5000);

        public async Task InsertAuctionsBatched(List<Auction> auctions, int serverId) =>
            await InsertBatches<Auction>(auction => InsertAuctions(auction, serverId), auctions, 5000);

        public async Task InsertMarketvaluesBatched(List<(int, int, double)> marketvalues, DateTimeOffset time, int serverId) =>
            await InsertBatches<(int, int, double)>(x => InsertMarketvalues(x, time, serverId), marketvalues, 5000);

        public async Task UpdateServerLastUpdate(int connectedRealmId, DateTimeOffset lastUpdate)
        {
            var query = @"
                UPDATE servers SET lastupdate = @lastUpdate WHERE connectedrealmid = @connectedRealmId
            ";

            await using var connection = await OpenNewConnection();
            await using var cmd = new NpgsqlCommand(query, connection);

            cmd.Parameters.AddWithValue("lastUpdate", lastUpdate);
            cmd.Parameters.AddWithValue("connectedRealmId", connectedRealmId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteServersAuctionsAndmarketvalues(int connectedRealmId)
        {
            var query = @"
                DELETE FROM auctions WHERE serverid = @serverid;
                DELETE FROM marketvalues WHERE serverid= @serverid;
            ";

            await using var connection = await OpenNewConnection();
            await using var cmd = new NpgsqlCommand(query, connection);

            cmd.Parameters.AddWithValue("serverid", connectedRealmId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InitialseDatabase()
        {
            var query = @"
                CREATE TABLE IF NOT EXISTS servers (
                    id serial PRIMARY KEY,
                    connectedRealmId INTEGER NOT NULL,
                    region TEXT NOT NULL,
                    realm TEXT NOT NULL,
                    realmName TEXT NOT NULL,
                    lastUpdate TIMESTAMPTZ NOT NULL
                );

                CREATE TABLE IF NOT EXISTS items (
                    id serial PRIMARY KEY,
                    originalItemId int NOT NULL,
                    context int NOT NULL DEFAULT -1,
                    modifiers int[] NOT NULL DEFAULT array[]::int[],
                    bonuses int[] NOT NULL DEFAULT array[]::int[],
                    petBreedId int NOT NULL DEFAULT -1,
                    petLevel int NOT NULL DEFAULT -1,
                    petQualityId int NOT NULL DEFAULT -1,
                    petSpeciesId int NOT NULL DEFAULT -1,
                    CONSTRAINT unique_item UNIQUE(originalItemId, context, modifiers,
                        bonuses, petBreedId, petLevel, petQualityId, petSpeciesId)
                );

                CREATE TABLE IF NOT EXISTS auctions (
                    id serial PRIMARY KEY,
                    serverId int REFERENCES servers(id) ON DELETE CASCADE,
                    auctionId int NOT NULL,
                    itemId int REFERENCES items(id) ON DELETE CASCADE,
                    bid bigint NOT NULL,
                    price bigint NOT NULL,
                    quantity int NOT NULL,
                    timeLeft TEXT
                );

                CREATE TABLE IF NOT EXISTS marketvalues (
                    serverId int REFERENCES servers(id) ON DELETE CASCADE,
                    itemId int REFERENCES items(id) ON DELETE CASCADE,
                    marketvalue double precision NOT NULL,
                    quantity int NOT NULL,
                    time TIMESTAMPTZ NOT NULL
                );


                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_items_itemId ON items (originalItemId);
                CREATE INDEX IF NOT EXISTS ix_auctions_serverId ON auctions (serverId);
                CREATE INDEX IF NOT EXISTS ix_auctions_itemId ON auctions (itemId);
            ";
            
            await using var connection = await OpenNewConnection();
            await using var cmd = new NpgsqlCommand(query, connection);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task TruncateTables()
        {
            var query = @"
                -- TRUNCATE TABLE items CASCADE;
                TRUNCATE TABLE auctions;
                TRUNCATE TABLE servers;
            ";

            await using var connection = await OpenNewConnection();
            
            await using var cmd = new NpgsqlCommand(query, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}