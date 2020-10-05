using System.Globalization;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace GoblineerNextUpdater.Models
{
    public class Modifier
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("context")]
        public int? Context { get; set; }

        [JsonPropertyName("modifiers")]
        public List<Modifier>? Modifiers { get; set; }

        [JsonPropertyName("pet_breed_id")]
        public int? PetBreedId { get; set; }

        [JsonPropertyName("pet_level")]
        public int? PetLevel { get; set; }
        
        [JsonPropertyName("pet_quality_id")]
        public int? PetQualityId { get; set; }
        
        [JsonPropertyName("pet_species_id")]
        public int? PetSpeciesId { get; set; }
        
        [JsonPropertyName("bonus_lists")]
        public List<int>? BonusLists { get; set; }
    }
}