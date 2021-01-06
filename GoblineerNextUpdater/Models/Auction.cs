using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GoblineerNextUpdater.Models
{
    public record Link
    {
        [JsonPropertyName("href")]
        public string Href { get; } = "";
    }

    public record Links
    {
        [JsonPropertyName("self")]
        public Link Self { get; } = new Link();
    }
    public record AuctionResponse
    {
        [JsonPropertyName("_links")]
        public Links Links { get; init; } = new Links();

        [JsonPropertyName("connected_realm")]
        public Link ConnectedRealm { get; init; } = new Link();

        [JsonPropertyName("auctions")]
        public List<Auction> Auctions { get; init; } = new List<Auction>();
    }
    public record Auction
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("item")]
        public Item Item { get; init; } = new Item();

        [JsonPropertyName("buyout")]
        public long Buyout { get; init; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; init; }

        [JsonPropertyName("time_left")]
        public string TimeLeft { get; init; } = "";

        [JsonPropertyName("bid")]
        public long Bid { get; init; }

        [JsonPropertyName("unit_price")]
        public long UnitPrice { get; init; }


        public (int, long) QuantityAndPrice 
        {
            get => UnitPrice switch {
                0 => (Quantity, Buyout),
                _ => (Quantity, UnitPrice),
            };
        }
    }
}