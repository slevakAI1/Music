// AI: purpose=JSON serialization for DrumTrackFeatureData with version support (Story 7.2a).
// AI: invariants=Deterministic output; round-trip preserves all data; version field for schema evolution.
// AI: deps=Uses System.Text.Json; outputs/consumes DrumTrackFeatureData; uses DTO for interface types.
// AI: change=Story 7.2a; update version when schema changes; add migration logic as needed.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// JSON serialization for drum feature data with schema versioning.
/// Supports compact format and round-trip preservation.
/// Uses internal DTOs to handle interface types (IReadOnlySet, IReadOnlyDictionary).
/// Story 7.2a: Serialization Support.
/// </summary>
public static class DrumFeatureDataSerializer
{
    /// <summary>
    /// Current schema version.
    /// Increment when breaking changes are made to the format.
    /// </summary>
    public const string CurrentSchemaVersion = "1.0";

    /// <summary>
    /// Shared serializer options (indented, camelCase, null handling).
    /// </summary>
    private static readonly JsonSerializerOptions DefaultOptions = CreateOptions(writeIndented: true);

    /// <summary>
    /// Compact serializer options (no indentation).
    /// </summary>
    private static readonly JsonSerializerOptions CompactOptions = CreateOptions(writeIndented: false);

    private static JsonSerializerOptions CreateOptions(bool writeIndented)
    {
        return new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    /// <summary>
    /// Serializes feature data to JSON string.
    /// </summary>
    /// <param name="data">Feature data to serialize.</param>
    /// <param name="compact">Use compact format (no indentation).</param>
    /// <returns>JSON string representation.</returns>
    public static string Serialize(DrumTrackFeatureData data, bool compact = false)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Convert to serializable DTO
        var dto = ToDto(data);
        var envelope = new SerializationEnvelope
        {
            SchemaVersion = CurrentSchemaVersion,
            Data = dto
        };

        var options = compact ? CompactOptions : DefaultOptions;
        return JsonSerializer.Serialize(envelope, options);
    }

    /// <summary>
    /// Deserializes JSON string to feature data.
    /// </summary>
    /// <param name="json">JSON string to deserialize.</param>
    /// <returns>Deserialized feature data.</returns>
    /// <exception cref="JsonException">If JSON is invalid.</exception>
    /// <exception cref="InvalidOperationException">If schema version is incompatible.</exception>
    public static DrumTrackFeatureData Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

        var envelope = JsonSerializer.Deserialize<SerializationEnvelope>(json, DefaultOptions);

        if (envelope == null)
            throw new JsonException("Failed to deserialize feature data envelope");

        // Version check
        ValidateSchemaVersion(envelope.SchemaVersion);

        if (envelope.Data == null)
            throw new JsonException("Feature data is null in envelope");

        return FromDto(envelope.Data);
    }

    /// <summary>
    /// Attempts to deserialize JSON string to feature data.
    /// </summary>
    /// <param name="json">JSON string to deserialize.</param>
    /// <param name="data">Deserialized data if successful.</param>
    /// <param name="error">Error message if failed.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool TryDeserialize(
        string json,
        out DrumTrackFeatureData? data,
        out string? error)
    {
        data = null;
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "JSON string is null or empty";
            return false;
        }

        try
        {
            data = Deserialize(json);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Validates schema version compatibility.
    /// </summary>
    private static void ValidateSchemaVersion(string? version)
    {
        if (string.IsNullOrEmpty(version))
            throw new InvalidOperationException("Schema version is missing from serialized data");

        // Extract major version for compatibility check
        var currentMajor = GetMajorVersion(CurrentSchemaVersion);
        var dataMajor = GetMajorVersion(version);

        if (dataMajor > currentMajor)
        {
            throw new InvalidOperationException(
                $"Schema version {version} is newer than supported version {CurrentSchemaVersion}. " +
                "Please update the application.");
        }

        // Allow older minor versions (backward compatible)
    }

    /// <summary>
    /// Extracts major version number from version string.
    /// </summary>
    private static int GetMajorVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length > 0 && int.TryParse(parts[0], out var major))
            return major;

        return 0;
    }

    /// <summary>
    /// Serializes feature data to a file.
    /// </summary>
    /// <param name="data">Feature data to serialize.</param>
    /// <param name="filePath">Output file path.</param>
    /// <param name="compact">Use compact format.</param>
    public static async Task SerializeToFileAsync(
        DrumTrackFeatureData data,
        string filePath,
        bool compact = false)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = Serialize(data, compact);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Deserializes feature data from a file.
    /// </summary>
    /// <param name="filePath">Input file path.</param>
    /// <returns>Deserialized feature data.</returns>
    public static async Task<DrumTrackFeatureData> DeserializeFromFileAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = await File.ReadAllTextAsync(filePath);
        return Deserialize(json);
    }

    #region DTO Conversion

    /// <summary>
    /// Converts domain model to serializable DTO.
    /// </summary>
    private static DrumTrackFeatureDataDto ToDto(DrumTrackFeatureData data)
    {
        return new DrumTrackFeatureDataDto
        {
            TrackId = data.TrackId,
            GenreHint = data.GenreHint,
            ArtistHint = data.ArtistHint,
            TotalBars = data.TotalBars,
            DefaultBeatsPerBar = data.DefaultBeatsPerBar,
            TempoEstimateBpm = data.TempoEstimateBpm,
            SchemaVersion = data.SchemaVersion,
            ExtractionTimestamp = data.ExtractionTimestamp,
            Events = data.Events.ToList(),
            BarPatterns = data.BarPatterns.Select(ToDto).ToList(),
            BarStats = data.BarStats.Select(ToDto).ToList(),
            RoleMatrices = data.RoleMatrices.ToDictionary(kvp => kvp.Key, kvp => ToDto(kvp.Value)),
            ActiveRoles = data.ActiveRoles.ToHashSet()
        };
    }

    /// <summary>
    /// Converts DTO back to domain model.
    /// </summary>
    private static DrumTrackFeatureData FromDto(DrumTrackFeatureDataDto dto)
    {
        return new DrumTrackFeatureData
        {
            TrackId = dto.TrackId ?? "",
            GenreHint = dto.GenreHint,
            ArtistHint = dto.ArtistHint,
            TotalBars = dto.TotalBars,
            DefaultBeatsPerBar = dto.DefaultBeatsPerBar,
            TempoEstimateBpm = dto.TempoEstimateBpm,
            SchemaVersion = dto.SchemaVersion ?? "1.0",
            ExtractionTimestamp = dto.ExtractionTimestamp,
            Events = dto.Events ?? new List<DrumMidiEvent>(),
            BarPatterns = dto.BarPatterns?.Select(FromDto).ToList() ?? new List<BarPatternFingerprint>(),
            BarStats = dto.BarStats?.Select(FromDto).ToList() ?? new List<BarOnsetStats>(),
            RoleMatrices = dto.RoleMatrices?.ToDictionary(kvp => kvp.Key, kvp => FromDto(kvp.Value))
                ?? new Dictionary<string, BeatPositionMatrix>(),
            ActiveRoles = dto.ActiveRoles ?? new HashSet<string>()
        };
    }

    private static BarPatternFingerprintDto ToDto(BarPatternFingerprint fp) => new()
    {
        BarNumber = fp.BarNumber,
        BeatsPerBar = fp.BeatsPerBar,
        RoleBitmasks = fp.RoleBitmasks.ToDictionary(k => k.Key, v => v.Value),
        RoleVelocities = fp.RoleVelocities.ToDictionary(k => k.Key, v => v.Value.ToList()),
        PatternHash = fp.PatternHash,
        RoleEventCounts = fp.RoleEventCounts.ToDictionary(k => k.Key, v => v.Value),
        GridResolution = fp.GridResolution
    };

    private static BarPatternFingerprint FromDto(BarPatternFingerprintDto dto) => new()
    {
        BarNumber = dto.BarNumber,
        BeatsPerBar = dto.BeatsPerBar,
        RoleBitmasks = dto.RoleBitmasks ?? new Dictionary<string, long>(),
        RoleVelocities = dto.RoleVelocities?.ToDictionary(
            k => k.Key, v => (IReadOnlyList<int>)v.Value) ?? new Dictionary<string, IReadOnlyList<int>>(),
        PatternHash = dto.PatternHash ?? "",
        RoleEventCounts = dto.RoleEventCounts ?? new Dictionary<string, int>(),
        GridResolution = dto.GridResolution
    };

    private static BarOnsetStatsDto ToDto(BarOnsetStats stats) => new()
    {
        BarNumber = stats.BarNumber,
        TotalHits = stats.TotalHits,
        HitsPerRole = stats.HitsPerRole.ToDictionary(k => k.Key, v => v.Value),
        AverageVelocity = stats.AverageVelocity,
        MinVelocity = stats.MinVelocity,
        MaxVelocity = stats.MaxVelocity,
        AverageVelocityPerRole = stats.AverageVelocityPerRole.ToDictionary(k => k.Key, v => v.Value),
        AverageTimingOffset = stats.AverageTimingOffset,
        MinTimingOffset = stats.MinTimingOffset,
        MaxTimingOffset = stats.MaxTimingOffset,
        HitsPerBeat = stats.HitsPerBeat.ToList(),
        OffbeatRatio = stats.OffbeatRatio
    };

    private static BarOnsetStats FromDto(BarOnsetStatsDto dto) => new()
    {
        BarNumber = dto.BarNumber,
        TotalHits = dto.TotalHits,
        HitsPerRole = dto.HitsPerRole ?? new Dictionary<string, int>(),
        AverageVelocity = dto.AverageVelocity,
        MinVelocity = dto.MinVelocity,
        MaxVelocity = dto.MaxVelocity,
        AverageVelocityPerRole = dto.AverageVelocityPerRole ?? new Dictionary<string, double>(),
        AverageTimingOffset = dto.AverageTimingOffset,
        MinTimingOffset = dto.MinTimingOffset,
        MaxTimingOffset = dto.MaxTimingOffset,
        HitsPerBeat = dto.HitsPerBeat ?? new List<int>(),
        OffbeatRatio = dto.OffbeatRatio
    };

    private static BeatPositionMatrixDto ToDto(BeatPositionMatrix matrix) => new()
    {
        Role = matrix.Role,
        TotalBars = matrix.TotalBars,
        GridResolution = matrix.GridResolution,
        BarSlots = matrix.BarSlots.Select(bar => bar.ToList()).ToList()
    };

    private static BeatPositionMatrix FromDto(BeatPositionMatrixDto dto) => new()
    {
        Role = dto.Role ?? "",
        TotalBars = dto.TotalBars,
        GridResolution = dto.GridResolution,
        BarSlots = dto.BarSlots?.Select(bar => bar.ToArray()).ToList()
            ?? new List<BeatPositionSlot?[]>()
    };

    #endregion

    /// <summary>
    /// Envelope for versioned serialization.
    /// </summary>
    private sealed record SerializationEnvelope
    {
        public string? SchemaVersion { get; init; }
        public DrumTrackFeatureDataDto? Data { get; init; }
    }

    #region Serialization DTOs (internal, JSON-friendly)

    private sealed record DrumTrackFeatureDataDto
    {
        public string? TrackId { get; init; }
        public string? GenreHint { get; init; }
        public string? ArtistHint { get; init; }
        public int TotalBars { get; init; }
        public int DefaultBeatsPerBar { get; init; }
        public int TempoEstimateBpm { get; init; }
        public string? SchemaVersion { get; init; }
        public DateTimeOffset ExtractionTimestamp { get; init; }
        public List<DrumMidiEvent>? Events { get; init; }
        public List<BarPatternFingerprintDto>? BarPatterns { get; init; }
        public List<BarOnsetStatsDto>? BarStats { get; init; }
        public Dictionary<string, BeatPositionMatrixDto>? RoleMatrices { get; init; }
        public HashSet<string>? ActiveRoles { get; init; }
    }

    private sealed record BarPatternFingerprintDto
    {
        public int BarNumber { get; init; }
        public int BeatsPerBar { get; init; }
        public Dictionary<string, long>? RoleBitmasks { get; init; }
        public Dictionary<string, List<int>>? RoleVelocities { get; init; }
        public string? PatternHash { get; init; }
        public Dictionary<string, int>? RoleEventCounts { get; init; }
        public int GridResolution { get; init; }
    }

    private sealed record BarOnsetStatsDto
    {
        public int BarNumber { get; init; }
        public int TotalHits { get; init; }
        public Dictionary<string, int>? HitsPerRole { get; init; }
        public double AverageVelocity { get; init; }
        public int MinVelocity { get; init; }
        public int MaxVelocity { get; init; }
        public Dictionary<string, double>? AverageVelocityPerRole { get; init; }
        public double AverageTimingOffset { get; init; }
        public int MinTimingOffset { get; init; }
        public int MaxTimingOffset { get; init; }
        public List<int>? HitsPerBeat { get; init; }
        public double OffbeatRatio { get; init; }
    }

    private sealed record BeatPositionMatrixDto
    {
        public string? Role { get; init; }
        public int TotalBars { get; init; }
        public int GridResolution { get; init; }
        public List<List<BeatPositionSlot?>>? BarSlots { get; init; }
    }

    #endregion
}
