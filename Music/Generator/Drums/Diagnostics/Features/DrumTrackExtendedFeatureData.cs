// AI: purpose=Extended feature container combining 7.2a base data with 7.2b pattern analysis (Story 7.2b).
// AI: invariants=BaseData is required; all analysis fields populated by builder; schema version tracks format changes.
// AI: deps=Contains DrumTrackFeatureData (7.2a) + all analysis records from 7.2b.
// AI: change=Story 7.2b; add new analysis sections as needed.

using Music.Generator.Drums.Diagnostics.Coordination;
using Music.Generator.Drums.Diagnostics.Dynamics;
using Music.Generator.Drums.Diagnostics.Patterns;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Diagnostics.Features;

/// <summary>
/// Extended feature data container combining Story 7.2a base data with 7.2b pattern analysis.
/// Provides comprehensive drum track analysis for benchmark and tuning workflows.
/// Story 7.2b: Extended Feature Container.
/// </summary>
public sealed record DrumTrackExtendedFeatureData
{
    /// <summary>
    /// Current schema version for extended data.
    /// </summary>
    public const string CurrentSchemaVersion = "1.0";

    // --- Base Data from Story 7.2a ---

    /// <summary>
    /// Base feature data including raw events, patterns, and statistics.
    /// </summary>
    public required DrumTrackFeatureData BaseData { get; init; }

    // --- Pattern Analysis (Story 7.2b) ---

    /// <summary>
    /// Pattern repetition analysis (unique patterns, runs, frequency).
    /// </summary>
    public required PatternRepetitionData PatternRepetition { get; init; }

    /// <summary>
    /// Pattern similarity analysis (similar pairs, families).
    /// </summary>
    public required PatternSimilarityData PatternSimilarity { get; init; }

    /// <summary>
    /// Multi-bar sequence detection (2-bar, 4-bar, evolving).
    /// </summary>
    public required SequencePatternData SequencePatterns { get; init; }

    // --- Cross-Role Analysis ---

    /// <summary>
    /// Cross-role coordination (coincidence, lock scores).
    /// </summary>
    public required CrossRoleCoordinationData CrossRoleCoordination { get; init; }

    // --- Anchor Analysis ---

    /// <summary>
    /// Anchor candidate detection (consistent positions, variance from reference).
    /// </summary>
    public required AnchorCandidateData AnchorCandidates { get; init; }

    // --- Structural Analysis ---

    /// <summary>
    /// Structural markers (fills, crashes, density anomalies, pattern changes).
    /// </summary>
    public required StructuralMarkerData StructuralMarkers { get; init; }

    // --- Performance Analysis ---

    /// <summary>
    /// Velocity dynamics (distributions, accents, ghosts).
    /// </summary>
    public required VelocityDynamicsData VelocityDynamics { get; init; }

    /// <summary>
    /// Timing feel analysis (swing, pocket, consistency).
    /// </summary>
    public required TimingFeelData TimingFeel { get; init; }

    // --- Metadata ---

    /// <summary>
    /// Schema version for this extended data format.
    /// </summary>
    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Timestamp when this analysis was performed.
    /// </summary>
    public DateTimeOffset AnalysisTimestamp { get; init; } = DateTimeOffset.UtcNow;

    // --- Derived Summary Properties ---

    /// <summary>
    /// Track ID (from base data).
    /// </summary>
    public string TrackId => BaseData.TrackId;

    /// <summary>
    /// Total bars analyzed (from base data).
    /// </summary>
    public int TotalBars => BaseData.TotalBars;

    /// <summary>
    /// Total unique pattern count.
    /// </summary>
    public int UniquePatternCount => PatternRepetition.UniquePatternCount;

    /// <summary>
    /// Total structural markers detected.
    /// </summary>
    public int TotalStructuralMarkers => StructuralMarkers.TotalMarkers;

    /// <summary>
    /// Whether the track has detectable swing feel.
    /// </summary>
    public bool HasSwing => TimingFeel.HasSwing;

    /// <summary>
    /// Overall variance from PopRock reference anchor.
    /// Null if not computed.
    /// </summary>
    public double? PopRockAnchorVariance => AnchorCandidates.PopRockAnchorVariance?.OverallVarianceScore;
}
