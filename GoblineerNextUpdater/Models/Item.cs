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
    public record Modifier
    {
        [JsonPropertyName("type")]
        public int Type { get; init; }

        [JsonPropertyName("value")]
        public int Value { get; init; }
    }

    public record Item
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("context")]
        public int? Context { get; init; }

        [JsonPropertyName("modifiers")]
        public List<Modifier>? Modifiers { get; init; }

        [JsonPropertyName("pet_breed_id")]
        public int? PetBreedId { get; init; }

        [JsonPropertyName("pet_level")]
        public int? PetLevel { get; init; }
        
        [JsonPropertyName("pet_quality_id")]
        public int? PetQualityId { get; init; }
        
        [JsonPropertyName("pet_species_id")]
        public int? PetSpeciesId { get; init; }
        
        [JsonPropertyName("bonus_lists")]
        public List<int>? BonusLists { get; init; }
    }
}