// AI: purpose=JSON serialization for DrumTrackExtendedFeatureData with versioning (Story 7.2b).
// AI: invariants=Deterministic output; round-trip preserves all data; version field for schema evolution.
// AI: deps=Uses System.Text.Json; references DrumFeatureDataSerializer for base data; uses DTOs for interface types.
// AI: change=Story 7.2b; update version when schema changes; add migration logic as needed.

using System.Text.Json;
using System.Text.Json.Serialization;

using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// JSON serialization for extended drum feature data with schema versioning.
/// Supports both base (7.2a) and extended (7.2b) data in a unified format.
/// Story 7.2b: Serialization Support.
/// </summary>
public static class DrumExtendedFeatureDataSerializer
{
    /// <summary>
    /// Current schema version for extended data.
    /// </summary>
    public const string CurrentSchemaVersion = "1.0";

    private static readonly JsonSerializerOptions DefaultOptions = CreateOptions(writeIndented: true);
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
    /// Serializes extended feature data to JSON string.
    /// </summary>
    /// <param name="data">Extended feature data to serialize.</param>
    /// <param name="compact">Use compact format (no indentation).</param>
    /// <returns>JSON string representation.</returns>
    public static string Serialize(DrumTrackExtendedFeatureData data, bool compact = false)
    {
        ArgumentNullException.ThrowIfNull(data);

        var dto = ToDto(data);
        var envelope = new ExtendedSerializationEnvelope
        {
            SchemaVersion = CurrentSchemaVersion,
            DataType = "Extended",
            Data = dto
        };

        var options = compact ? CompactOptions : DefaultOptions;
        return JsonSerializer.Serialize(envelope, options);
    }

    /// <summary>
    /// Deserializes JSON string to extended feature data.
    /// </summary>
    /// <param name="json">JSON string to deserialize.</param>
    /// <returns>Deserialized extended feature data.</returns>
    /// <exception cref="JsonException">If JSON is invalid.</exception>
    /// <exception cref="InvalidOperationException">If schema version is incompatible.</exception>
    public static DrumTrackExtendedFeatureData Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

        var envelope = JsonSerializer.Deserialize<ExtendedSerializationEnvelope>(json, DefaultOptions);

        if (envelope == null)
            throw new JsonException("Failed to deserialize extended feature data envelope");

        ValidateSchemaVersion(envelope.SchemaVersion);

        if (envelope.Data == null)
            throw new JsonException("Extended feature data is null in envelope");

        return FromDto(envelope.Data);
    }

    /// <summary>
    /// Attempts to deserialize JSON string to extended feature data.
    /// </summary>
    public static bool TryDeserialize(
        string json,
        out DrumTrackExtendedFeatureData? data,
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
    /// Serializes extended feature data to a file.
    /// </summary>
    public static async Task SerializeToFileAsync(
        DrumTrackExtendedFeatureData data,
        string filePath,
        bool compact = false)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = Serialize(data, compact);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Deserializes extended feature data from a file.
    /// </summary>
    public static async Task<DrumTrackExtendedFeatureData> DeserializeFromFileAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = await File.ReadAllTextAsync(filePath);
        return Deserialize(json);
    }

    private static void ValidateSchemaVersion(string? version)
    {
        if (string.IsNullOrEmpty(version))
            throw new InvalidOperationException("Schema version is missing from serialized data");

        var currentMajor = GetMajorVersion(CurrentSchemaVersion);
        var dataMajor = GetMajorVersion(version);

        if (dataMajor > currentMajor)
        {
            throw new InvalidOperationException(
                $"Schema version {version} is newer than supported version {CurrentSchemaVersion}. " +
                "Please update the application.");
        }
    }

    private static int GetMajorVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length > 0 && int.TryParse(parts[0], out var major))
            return major;
        return 0;
    }

    #region DTO Conversion

    private static ExtendedFeatureDataDto ToDto(DrumTrackExtendedFeatureData data)
    {
        return new ExtendedFeatureDataDto
        {
            BaseData = ToBaseDataDto(data.BaseData),
            PatternRepetition = ToDto(data.PatternRepetition),
            PatternSimilarity = ToDto(data.PatternSimilarity),
            SequencePatterns = ToDto(data.SequencePatterns),
            CrossRoleCoordination = ToDto(data.CrossRoleCoordination),
            AnchorCandidates = ToDto(data.AnchorCandidates),
            StructuralMarkers = ToDto(data.StructuralMarkers),
            VelocityDynamics = ToDto(data.VelocityDynamics),
            TimingFeel = ToDto(data.TimingFeel),
            SchemaVersion = data.SchemaVersion,
            AnalysisTimestamp = data.AnalysisTimestamp
        };
    }

    private static DrumTrackExtendedFeatureData FromDto(ExtendedFeatureDataDto dto)
    {
        return new DrumTrackExtendedFeatureData
        {
            BaseData = FromBaseDataDto(dto.BaseData!),
            PatternRepetition = FromDto(dto.PatternRepetition!),
            PatternSimilarity = FromDto(dto.PatternSimilarity!),
            SequencePatterns = FromDto(dto.SequencePatterns!),
            CrossRoleCoordination = FromDto(dto.CrossRoleCoordination!),
            AnchorCandidates = FromDto(dto.AnchorCandidates!),
            StructuralMarkers = FromDto(dto.StructuralMarkers!),
            VelocityDynamics = FromDto(dto.VelocityDynamics!),
            TimingFeel = FromDto(dto.TimingFeel!),
            SchemaVersion = dto.SchemaVersion ?? CurrentSchemaVersion,
            AnalysisTimestamp = dto.AnalysisTimestamp
        };
    }

    // Base data DTO conversion (simplified - just store JSON for now)
    private static BaseDataDto ToBaseDataDto(DrumTrackFeatureData data)
    {
        return new BaseDataDto
        {
            TrackId = data.TrackId,
            GenreHint = data.GenreHint,
            ArtistHint = data.ArtistHint,
            TotalBars = data.TotalBars,
            DefaultBeatsPerBar = data.DefaultBeatsPerBar,
            TempoEstimateBpm = data.TempoEstimateBpm,
            Events = data.Events.ToList(),
            BarPatterns = data.BarPatterns.ToList(),
            BarStats = data.BarStats.ToList(),
            RoleMatrices = data.RoleMatrices.ToDictionary(k => k.Key, v => v.Value),
            ActiveRoles = data.ActiveRoles.ToHashSet()
        };
    }

    private static DrumTrackFeatureData FromBaseDataDto(BaseDataDto dto)
    {
        return new DrumTrackFeatureData
        {
            TrackId = dto.TrackId ?? "",
            GenreHint = dto.GenreHint,
            ArtistHint = dto.ArtistHint,
            TotalBars = dto.TotalBars,
            DefaultBeatsPerBar = dto.DefaultBeatsPerBar,
            TempoEstimateBpm = dto.TempoEstimateBpm,
            Events = dto.Events ?? new List<DrumMidiEvent>(),
            BarPatterns = dto.BarPatterns ?? new List<BarPatternFingerprint>(),
            BarStats = dto.BarStats ?? new List<BarOnsetStats>(),
            RoleMatrices = dto.RoleMatrices ?? new Dictionary<string, BeatPositionMatrix>(),
            ActiveRoles = dto.ActiveRoles ?? new HashSet<string>()
        };
    }

    // Pattern Repetition
    private static PatternRepetitionDto ToDto(PatternRepetitionData data) => new()
    {
        PatternOccurrences = data.PatternOccurrences.ToDictionary(k => k.Key, v => v.Value.ToList()),
        UniquePatternCount = data.UniquePatternCount,
        MostCommonPatterns = data.MostCommonPatterns.Select(ToDto).ToList(),
        ConsecutiveRuns = data.ConsecutiveRuns.Select(ToDto).ToList(),
        TotalBars = data.TotalBars
    };

    private static PatternRepetitionData FromDto(PatternRepetitionDto dto) => new()
    {
        PatternOccurrences = dto.PatternOccurrences?.ToDictionary(
            k => k.Key, v => (IReadOnlyList<int>)v.Value) ?? new Dictionary<string, IReadOnlyList<int>>(),
        UniquePatternCount = dto.UniquePatternCount,
        MostCommonPatterns = dto.MostCommonPatterns?.Select(FromDto).ToList() ?? new List<PatternFrequency>(),
        ConsecutiveRuns = dto.ConsecutiveRuns?.Select(FromDto).ToList() ?? new List<PatternRun>(),
        TotalBars = dto.TotalBars
    };

    private static PatternFrequencyDto ToDto(PatternFrequency f) =>
        new(f.PatternHash, f.OccurrenceCount, f.BarNumbers.ToList());

    private static PatternFrequency FromDto(PatternFrequencyDto dto) =>
        new(dto.PatternHash ?? "", dto.OccurrenceCount, dto.BarNumbers ?? new List<int>());

    private static PatternRunDto ToDto(PatternRun r) =>
        new(r.PatternHash, r.StartBar, r.EndBar, r.Length);

    private static PatternRun FromDto(PatternRunDto dto) =>
        new(dto.PatternHash ?? "", dto.StartBar, dto.EndBar, dto.Length);

    // Pattern Similarity
    private static PatternSimilarityDto ToDto(PatternSimilarityData data) => new()
    {
        SimilarPairs = data.SimilarPairs.Select(ToDto).ToList(),
        PatternFamilies = data.PatternFamilies.Select(ToDto).ToList()
    };

    private static PatternSimilarityData FromDto(PatternSimilarityDto dto) => new()
    {
        SimilarPairs = dto.SimilarPairs?.Select(FromDto).ToList() ?? new List<SimilarPatternPair>(),
        PatternFamilies = dto.PatternFamilies?.Select(FromDto).ToList() ?? new List<PatternFamily>()
    };

    private static SimilarPatternPairDto ToDto(SimilarPatternPair p) =>
        new(p.PatternHashA, p.PatternHashB, p.Similarity);

    private static SimilarPatternPair FromDto(SimilarPatternPairDto dto) =>
        new(dto.PatternHashA ?? "", dto.PatternHashB ?? "", dto.Similarity);

    private static PatternFamilyDto ToDto(PatternFamily f) =>
        new(f.BasePatternHash, f.VariantHashes.ToList(), f.AllBarNumbers.ToList());

    private static PatternFamily FromDto(PatternFamilyDto dto) =>
        new(dto.BasePatternHash ?? "", dto.VariantHashes ?? new List<string>(), dto.AllBarNumbers ?? new List<int>());

    // Sequence Patterns
    private static SequencePatternDto ToDto(SequencePatternData data) => new()
    {
        TwoBarSequences = data.TwoBarSequences.Select(ToDto).ToList(),
        FourBarSequences = data.FourBarSequences.Select(ToDto).ToList(),
        EvolvingSequences = data.EvolvingSequences.Select(ToDto).ToList()
    };

    private static SequencePatternData FromDto(SequencePatternDto dto) => new()
    {
        TwoBarSequences = dto.TwoBarSequences?.Select(FromDto).ToList() ?? new List<MultiBarSequence>(),
        FourBarSequences = dto.FourBarSequences?.Select(FromDto).ToList() ?? new List<MultiBarSequence>(),
        EvolvingSequences = dto.EvolvingSequences?.Select(FromDto).ToList() ?? new List<EvolvingSequence>()
    };

    private static MultiBarSequenceDto ToDto(MultiBarSequence s) =>
        new(s.PatternHashes.ToList(), s.Occurrences.ToList(), s.SequenceLength);

    private static MultiBarSequence FromDto(MultiBarSequenceDto dto) =>
        new(dto.PatternHashes ?? new List<string>(), dto.Occurrences ?? new List<int>(), dto.SequenceLength);

    private static EvolvingSequenceDto ToDto(EvolvingSequence s) =>
        new(s.BasePatternHash, s.Steps.Select(ToDto).ToList(), s.TotalBarsSpanned);

    private static EvolvingSequence FromDto(EvolvingSequenceDto dto) =>
        new(dto.BasePatternHash ?? "", dto.Steps?.Select(FromDto).ToList() ?? new List<EvolutionStep>(), dto.TotalBarsSpanned);

    private static EvolutionStepDto ToDto(EvolutionStep s) =>
        new(s.BarNumber, s.PatternHash, s.SimilarityToBase);

    private static EvolutionStep FromDto(EvolutionStepDto dto) =>
        new(dto.BarNumber, dto.PatternHash ?? "", dto.SimilarityToBase);

    // Cross Role Coordination
    private static CrossRoleCoordinationDto ToDto(CrossRoleCoordinationData data) => new()
    {
        CoincidenceCount = data.CoincidenceCount.ToDictionary(k => k.Key, v => v.Value),
        RolePairDetails = data.RolePairDetails.Select(ToDto).ToList(),
        LockScores = data.LockScores.ToDictionary(k => k.Key, v => v.Value)
    };

    private static CrossRoleCoordinationData FromDto(CrossRoleCoordinationDto dto) => new()
    {
        CoincidenceCount = dto.CoincidenceCount ?? new Dictionary<string, int>(),
        RolePairDetails = dto.RolePairDetails?.Select(FromDto).ToList() ?? new List<RolePairCoincidence>(),
        LockScores = dto.LockScores ?? new Dictionary<string, double>()
    };

    private static RolePairCoincidenceDto ToDto(RolePairCoincidence r) =>
        new(r.RoleA, r.RoleB, r.TotalCoincidences, r.CoincidenceRatio, r.CommonPositionMask);

    private static RolePairCoincidence FromDto(RolePairCoincidenceDto dto) =>
        new(dto.RoleA ?? "", dto.RoleB ?? "", dto.TotalCoincidences, dto.CoincidenceRatio, dto.CommonPositionMask);

    // Anchor Candidates
    private static AnchorCandidateDto ToDto(AnchorCandidateData data) => new()
    {
        RoleAnchors = data.RoleAnchors.ToDictionary(k => k.Key, v => v.Value.Select(ToDto).ToList()),
        ConsistentPositionMasks = data.ConsistentPositionMasks.ToDictionary(k => k.Key, v => v.Value),
        PopRockAnchorVariance = data.PopRockAnchorVariance != null ? ToDto(data.PopRockAnchorVariance) : null
    };

    private static AnchorCandidateData FromDto(AnchorCandidateDto dto) => new()
    {
        RoleAnchors = dto.RoleAnchors?.ToDictionary(
            k => k.Key,
            v => (IReadOnlyList<PositionConsistency>)v.Value.Select(FromDto).ToList())
            ?? new Dictionary<string, IReadOnlyList<PositionConsistency>>(),
        ConsistentPositionMasks = dto.ConsistentPositionMasks ?? new Dictionary<string, long>(),
        PopRockAnchorVariance = dto.PopRockAnchorVariance != null ? FromDto(dto.PopRockAnchorVariance) : null
    };

    private static PositionConsistencyDto ToDto(PositionConsistency p) =>
        new(p.GridPosition, p.HitCount, p.TotalBars, p.ConsistencyRatio);

    private static PositionConsistency FromDto(PositionConsistencyDto dto) =>
        new(dto.GridPosition, dto.HitCount, dto.TotalBars, dto.ConsistencyRatio);

    private static AnchorVarianceDto ToDto(AnchorVarianceFromReference a) => new()
    {
        ReferenceName = a.ReferenceName,
        OverallVarianceScore = a.OverallVarianceScore,
        PerRoleVariance = a.PerRoleVariance.ToDictionary(k => k.Key, v => v.Value),
        MissingAnchors = a.MissingAnchors.ToList(),
        ExtraAnchors = a.ExtraAnchors.ToList()
    };

    private static AnchorVarianceFromReference FromDto(AnchorVarianceDto dto) =>
        new(
            dto.ReferenceName ?? "",
            dto.OverallVarianceScore,
            dto.PerRoleVariance ?? new Dictionary<string, double>(),
            dto.MissingAnchors ?? new List<string>(),
            dto.ExtraAnchors ?? new List<string>());

    // Structural Markers
    private static StructuralMarkerDto ToDto(StructuralMarkerData data) => new()
    {
        HighDensityBars = data.HighDensityBars.Select(ToDto).ToList(),
        LowDensityBars = data.LowDensityBars.Select(ToDto).ToList(),
        CrashBars = data.CrashBars.ToList(),
        PatternChanges = data.PatternChanges.Select(ToDto).ToList(),
        PotentialFills = data.PotentialFills.Select(ToDto).ToList()
    };

    private static StructuralMarkerData FromDto(StructuralMarkerDto dto) => new()
    {
        HighDensityBars = dto.HighDensityBars?.Select(FromDto).ToList() ?? new List<DensityAnomaly>(),
        LowDensityBars = dto.LowDensityBars?.Select(FromDto).ToList() ?? new List<DensityAnomaly>(),
        CrashBars = dto.CrashBars ?? new List<int>(),
        PatternChanges = dto.PatternChanges?.Select(FromDto).ToList() ?? new List<PatternChangePoint>(),
        PotentialFills = dto.PotentialFills?.Select(FromDto).ToList() ?? new List<PotentialFill>()
    };

    private static DensityAnomalyDto ToDto(DensityAnomaly d) =>
        new(d.BarNumber, d.EventCount, d.DeviationFromMean);

    private static DensityAnomaly FromDto(DensityAnomalyDto dto) =>
        new(dto.BarNumber, dto.EventCount, dto.DeviationFromMean);

    private static PatternChangePointDto ToDto(PatternChangePoint p) =>
        new(p.BarNumber, p.PreviousPatternHash, p.NewPatternHash, p.Similarity);

    private static PatternChangePoint FromDto(PatternChangePointDto dto) =>
        new(dto.BarNumber, dto.PreviousPatternHash ?? "", dto.NewPatternHash ?? "", dto.Similarity);

    private static PotentialFillDto ToDto(PotentialFill f) =>
        new(f.StartBar, f.EndBar, f.Confidence, f.IndicatorReasons.ToList());

    private static PotentialFill FromDto(PotentialFillDto dto) =>
        new(dto.StartBar, dto.EndBar, dto.Confidence, dto.IndicatorReasons ?? new List<string>());

    // Velocity Dynamics
    private static VelocityDynamicsDto ToDto(VelocityDynamicsData data) => new()
    {
        RoleDistributions = data.RoleDistributions.ToDictionary(k => k.Key, v => ToDto(v.Value)),
        RoleVelocityByPosition = data.RoleVelocityByPosition.ToDictionary(k => k.Key, v => v.Value.ToList()),
        AccentMasks = data.AccentMasks.ToDictionary(k => k.Key, v => v.Value),
        GhostPositions = data.GhostPositions.ToList()
    };

    private static VelocityDynamicsData FromDto(VelocityDynamicsDto dto) => new()
    {
        RoleDistributions = dto.RoleDistributions?.ToDictionary(k => k.Key, v => FromDto(v.Value))
            ?? new Dictionary<string, VelocityDistribution>(),
        RoleVelocityByPosition = dto.RoleVelocityByPosition?.ToDictionary(
            k => k.Key, v => (IReadOnlyList<double>)v.Value)
            ?? new Dictionary<string, IReadOnlyList<double>>(),
        AccentMasks = dto.AccentMasks ?? new Dictionary<string, long>(),
        GhostPositions = dto.GhostPositions ?? new List<int>()
    };

    private static VelocityDistributionDto ToDto(VelocityDistribution v) =>
        new(v.Mean, v.StdDev, v.Min, v.Max, v.Histogram.ToList());

    private static VelocityDistribution FromDto(VelocityDistributionDto dto) =>
        new(dto.Mean, dto.StdDev, dto.Min, dto.Max, dto.Histogram ?? new List<int>());

    // Timing Feel
    private static TimingFeelDto ToDto(TimingFeelData data) => new()
    {
        RoleAverageOffset = data.RoleAverageOffset.ToDictionary(k => k.Key, v => v.Value),
        RoleTimingDistributions = data.RoleTimingDistributions.ToDictionary(k => k.Key, v => ToDto(v.Value)),
        SwingRatio = data.SwingRatio,
        AheadBehindScore = data.AheadBehindScore,
        TimingConsistency = data.TimingConsistency
    };

    private static TimingFeelData FromDto(TimingFeelDto dto) => new()
    {
        RoleAverageOffset = dto.RoleAverageOffset ?? new Dictionary<string, double>(),
        RoleTimingDistributions = dto.RoleTimingDistributions?.ToDictionary(k => k.Key, v => FromDto(v.Value))
            ?? new Dictionary<string, TimingDistribution>(),
        SwingRatio = dto.SwingRatio,
        AheadBehindScore = dto.AheadBehindScore,
        TimingConsistency = dto.TimingConsistency
    };

    private static TimingDistributionDto ToDto(TimingDistribution t) =>
        new(t.Mean, t.StdDev, t.MinOffset, t.MaxOffset, t.Histogram.ToList());

    private static TimingDistribution FromDto(TimingDistributionDto dto) =>
        new(dto.Mean, dto.StdDev, dto.MinOffset, dto.MaxOffset, dto.Histogram ?? new List<int>());

    #endregion

    #region Serialization DTOs

    private sealed record ExtendedSerializationEnvelope
    {
        public string? SchemaVersion { get; init; }
        public string? DataType { get; init; }
        public ExtendedFeatureDataDto? Data { get; init; }
    }

    private sealed record ExtendedFeatureDataDto
    {
        public BaseDataDto? BaseData { get; init; }
        public PatternRepetitionDto? PatternRepetition { get; init; }
        public PatternSimilarityDto? PatternSimilarity { get; init; }
        public SequencePatternDto? SequencePatterns { get; init; }
        public CrossRoleCoordinationDto? CrossRoleCoordination { get; init; }
        public AnchorCandidateDto? AnchorCandidates { get; init; }
        public StructuralMarkerDto? StructuralMarkers { get; init; }
        public VelocityDynamicsDto? VelocityDynamics { get; init; }
        public TimingFeelDto? TimingFeel { get; init; }
        public string? SchemaVersion { get; init; }
        public DateTimeOffset AnalysisTimestamp { get; init; }
    }

    private sealed record BaseDataDto
    {
        public string? TrackId { get; init; }
        public string? GenreHint { get; init; }
        public string? ArtistHint { get; init; }
        public int TotalBars { get; init; }
        public int DefaultBeatsPerBar { get; init; }
        public int TempoEstimateBpm { get; init; }
        public List<DrumMidiEvent>? Events { get; init; }
        public List<BarPatternFingerprint>? BarPatterns { get; init; }
        public List<BarOnsetStats>? BarStats { get; init; }
        public Dictionary<string, BeatPositionMatrix>? RoleMatrices { get; init; }
        public HashSet<string>? ActiveRoles { get; init; }
    }

    private sealed record PatternRepetitionDto
    {
        public Dictionary<string, List<int>>? PatternOccurrences { get; init; }
        public int UniquePatternCount { get; init; }
        public List<PatternFrequencyDto>? MostCommonPatterns { get; init; }
        public List<PatternRunDto>? ConsecutiveRuns { get; init; }
        public int TotalBars { get; init; }
    }

    private sealed record PatternFrequencyDto(string? PatternHash, int OccurrenceCount, List<int>? BarNumbers);
    private sealed record PatternRunDto(string? PatternHash, int StartBar, int EndBar, int Length);

    private sealed record PatternSimilarityDto
    {
        public List<SimilarPatternPairDto>? SimilarPairs { get; init; }
        public List<PatternFamilyDto>? PatternFamilies { get; init; }
    }

    private sealed record SimilarPatternPairDto(string? PatternHashA, string? PatternHashB, double Similarity);
    private sealed record PatternFamilyDto(string? BasePatternHash, List<string>? VariantHashes, List<int>? AllBarNumbers);

    private sealed record SequencePatternDto
    {
        public List<MultiBarSequenceDto>? TwoBarSequences { get; init; }
        public List<MultiBarSequenceDto>? FourBarSequences { get; init; }
        public List<EvolvingSequenceDto>? EvolvingSequences { get; init; }
    }

    private sealed record MultiBarSequenceDto(List<string>? PatternHashes, List<int>? Occurrences, int SequenceLength);
    private sealed record EvolvingSequenceDto(string? BasePatternHash, List<EvolutionStepDto>? Steps, int TotalBarsSpanned);
    private sealed record EvolutionStepDto(int BarNumber, string? PatternHash, double SimilarityToBase);

    private sealed record CrossRoleCoordinationDto
    {
        public Dictionary<string, int>? CoincidenceCount { get; init; }
        public List<RolePairCoincidenceDto>? RolePairDetails { get; init; }
        public Dictionary<string, double>? LockScores { get; init; }
    }

    private sealed record RolePairCoincidenceDto(string? RoleA, string? RoleB, int TotalCoincidences, double CoincidenceRatio, long CommonPositionMask);

    private sealed record AnchorCandidateDto
    {
        public Dictionary<string, List<PositionConsistencyDto>>? RoleAnchors { get; init; }
        public Dictionary<string, long>? ConsistentPositionMasks { get; init; }
        public AnchorVarianceDto? PopRockAnchorVariance { get; init; }
    }

    private sealed record PositionConsistencyDto(int GridPosition, int HitCount, int TotalBars, double ConsistencyRatio);

    private sealed record AnchorVarianceDto
    {
        public string? ReferenceName { get; init; }
        public double OverallVarianceScore { get; init; }
        public Dictionary<string, double>? PerRoleVariance { get; init; }
        public List<string>? MissingAnchors { get; init; }
        public List<string>? ExtraAnchors { get; init; }
    }

    private sealed record StructuralMarkerDto
    {
        public List<DensityAnomalyDto>? HighDensityBars { get; init; }
        public List<DensityAnomalyDto>? LowDensityBars { get; init; }
        public List<int>? CrashBars { get; init; }
        public List<PatternChangePointDto>? PatternChanges { get; init; }
        public List<PotentialFillDto>? PotentialFills { get; init; }
    }

    private sealed record DensityAnomalyDto(int BarNumber, int EventCount, double DeviationFromMean);
    private sealed record PatternChangePointDto(int BarNumber, string? PreviousPatternHash, string? NewPatternHash, double Similarity);
    private sealed record PotentialFillDto(int StartBar, int EndBar, double Confidence, List<string>? IndicatorReasons);

    private sealed record VelocityDynamicsDto
    {
        public Dictionary<string, VelocityDistributionDto>? RoleDistributions { get; init; }
        public Dictionary<string, List<double>>? RoleVelocityByPosition { get; init; }
        public Dictionary<string, long>? AccentMasks { get; init; }
        public List<int>? GhostPositions { get; init; }
    }

    private sealed record VelocityDistributionDto(double Mean, double StdDev, int Min, int Max, List<int>? Histogram);

    private sealed record TimingFeelDto
    {
        public Dictionary<string, double>? RoleAverageOffset { get; init; }
        public Dictionary<string, TimingDistributionDto>? RoleTimingDistributions { get; init; }
        public double SwingRatio { get; init; }
        public double AheadBehindScore { get; init; }
        public double TimingConsistency { get; init; }
    }

    private sealed record TimingDistributionDto(double Mean, double StdDev, int MinOffset, int MaxOffset, List<int>? Histogram);

    #endregion
}
