using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GoblineerNextUpdater.Models
{
    public class Link
    {
        [JsonPropertyName("href")]
        public string Href { get; set; } = "";
    }

    public class Links
    {
        [JsonPropertyName("self")]
        public Link Self { get; set; } = new Link();
    }
    public class AuctionResponse
    {
        [JsonPropertyName("_links")]
        public Links Links { get; set; } = new Links();

        [JsonPropertyName("connected_realm")]
        public Link ConnectedRealm { get; set; } = new Link();

        [JsonPropertyName("auctions")]
        public List<Auction> Auctions { get; set; } = new List<Auction>();
    }
    public class Auction
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("item")]
        public Item Item { get; set; } = new Item();

        [JsonPropertyName("buyout")]
        public long Buyout { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("time_left")]
        public string TimeLeft { get; set; } = "";

        [JsonPropertyName("bid")]
        public long Bid { get; set; }

        [JsonPropertyName("unit_price")]
        public long UnitPrice { get; set; }


        public (int, long) QuantityAndPrice 
        {
            get => UnitPrice switch {
                0 => (Quantity, Buyout),
                _ => (Quantity, UnitPrice),
            };
        }
    }
}