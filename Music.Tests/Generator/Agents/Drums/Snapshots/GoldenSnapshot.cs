// AI: purpose=Snapshot data model for golden test serialization; captures per-bar drum events with provenance.
// AI: deps=System.Text.Json for serialization; used by DrummerGoldenTests.
// AI: change=Story 10.8.3: End-to-end regression snapshot (golden test).

using System.Text.Json.Serialization;

namespace Music.Tests.Generator.Agents.Drums.Snapshots
{
    public sealed record GoldenSnapshot
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; init; } = 1;

        [JsonPropertyName("seed")]
        public int Seed { get; init; }

        [JsonPropertyName("styleId")]
        public string StyleId { get; init; } = string.Empty;

        [JsonPropertyName("totalBars")]
        public int TotalBars { get; init; }

        [JsonPropertyName("bars")]
        public List<BarSnapshot> Bars { get; init; } = new();
    }

    public sealed record BarSnapshot
    {
        [JsonPropertyName("barNumber")]
        public int BarNumber { get; init; }

        [JsonPropertyName("sectionType")]
        public string SectionType { get; init; } = string.Empty;

        [JsonPropertyName("events")]
        public List<EventSnapshot> Events { get; init; } = new();

        [JsonPropertyName("operatorsUsed")]
        public List<string> OperatorsUsed { get; init; } = new();
    }

    public sealed record EventSnapshot
    {
        [JsonPropertyName("beat")]
        public decimal Beat { get; init; }

        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("velocity")]
        public int Velocity { get; init; }

        [JsonPropertyName("timingOffset")]
        public int TimingOffset { get; init; }

        [JsonPropertyName("provenance")]
        public string Provenance { get; init; } = "Anchor";
    }
}

