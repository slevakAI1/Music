// AI: purpose=Orchestrates Story 7.2b analysis pipeline to build DrumTrackExtendedFeatureData.
// AI: invariants=Requires DrumTrackFeatureData from 7.2a; runs all extractors in deterministic order.
// AI: deps=Uses all 7.2b extractors; outputs DrumTrackExtendedFeatureData.
// AI: change=Story 7.2b; add new extractors as analysis capabilities expand.

using Music.Generator.Drums.Diagnostics.Coordination;
using Music.Generator.Drums.Diagnostics.Dynamics;
using Music.Generator.Drums.Diagnostics.Patterns;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Diagnostics.Features;

/// <summary>
/// Orchestrates the Story 7.2b analysis pipeline.
/// Takes base feature data (7.2a) and runs all pattern/structural/performance analysis.
/// Story 7.2b: Extended Feature Container.
/// </summary>
public static class DrumTrackExtendedFeatureDataBuilder
{
    /// <summary>
    /// Builds extended feature data from base feature data.
    /// Runs all Story 7.2b extractors in deterministic order.
    /// </summary>
    /// <param name="baseData">Base feature data from Story 7.2a.</param>
    /// <param name="includePopRockReference">Include PopRock anchor variance analysis.</param>
    /// <returns>Extended feature data with all analysis results.</returns>
    public static DrumTrackExtendedFeatureData Build(
        DrumTrackFeatureData baseData,
        bool includePopRockReference = true)
    {
        ArgumentNullException.ThrowIfNull(baseData);

        // Pattern Analysis
        var patternRepetition = PatternRepetitionDetector.Detect(baseData.BarPatterns);
        var patternSimilarity = PatternSimilarityAnalyzer.Analyze(baseData.BarPatterns);
        var sequencePatterns = SequencePatternDetector.Detect(baseData.BarPatterns);

        // Cross-Role Analysis
        var crossRoleCoordination = CrossRoleCoordinationExtractor.Extract(baseData.RoleMatrices);

        // Anchor Analysis
        var anchorCandidates = AnchorCandidateExtractor.Extract(
            baseData.RoleMatrices,
            includePopRockReference);

        // Structural Analysis
        var structuralMarkers = StructuralMarkerDetector.Detect(baseData);

        // Performance Analysis
        var velocityDynamics = VelocityDynamicsExtractor.Extract(baseData);
        var timingFeel = TimingFeelExtractor.Extract(baseData);

        return new DrumTrackExtendedFeatureData
        {
            BaseData = baseData,
            PatternRepetition = patternRepetition,
            PatternSimilarity = patternSimilarity,
            SequencePatterns = sequencePatterns,
            CrossRoleCoordination = crossRoleCoordination,
            AnchorCandidates = anchorCandidates,
            StructuralMarkers = structuralMarkers,
            VelocityDynamics = velocityDynamics,
            TimingFeel = timingFeel,
            AnalysisTimestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Builds extended feature data with custom analysis options.
    /// </summary>
    /// <param name="baseData">Base feature data from Story 7.2a.</param>
    /// <param name="options">Analysis options.</param>
    /// <returns>Extended feature data with selected analysis results.</returns>
    public static DrumTrackExtendedFeatureData Build(
        DrumTrackFeatureData baseData,
        ExtendedAnalysisOptions options)
    {
        ArgumentNullException.ThrowIfNull(baseData);
        ArgumentNullException.ThrowIfNull(options);

        // Pattern Analysis (always run - lightweight)
        var patternRepetition = PatternRepetitionDetector.Detect(baseData.BarPatterns);
        var patternSimilarity = PatternSimilarityAnalyzer.Analyze(baseData.BarPatterns);
        var sequencePatterns = SequencePatternDetector.Detect(baseData.BarPatterns);

        // Cross-Role Analysis (conditional)
        var crossRoleCoordination = options.IncludeCrossRoleAnalysis
            ? CrossRoleCoordinationExtractor.Extract(baseData.RoleMatrices)
            : CreateEmptyCrossRoleData();

        // Anchor Analysis (conditional)
        var anchorCandidates = options.IncludeAnchorAnalysis
            ? AnchorCandidateExtractor.Extract(baseData.RoleMatrices, options.IncludePopRockReference)
            : CreateEmptyAnchorData();

        // Structural Analysis (conditional)
        var structuralMarkers = options.IncludeStructuralAnalysis
            ? StructuralMarkerDetector.Detect(baseData)
            : CreateEmptyStructuralData();

        // Performance Analysis (conditional)
        var velocityDynamics = options.IncludePerformanceAnalysis
            ? VelocityDynamicsExtractor.Extract(baseData)
            : CreateEmptyVelocityData();

        var timingFeel = options.IncludePerformanceAnalysis
            ? TimingFeelExtractor.Extract(baseData)
            : CreateEmptyTimingData();

        return new DrumTrackExtendedFeatureData
        {
            BaseData = baseData,
            PatternRepetition = patternRepetition,
            PatternSimilarity = patternSimilarity,
            SequencePatterns = sequencePatterns,
            CrossRoleCoordination = crossRoleCoordination,
            AnchorCandidates = anchorCandidates,
            StructuralMarkers = structuralMarkers,
            VelocityDynamics = velocityDynamics,
            TimingFeel = timingFeel,
            AnalysisTimestamp = DateTimeOffset.UtcNow
        };
    }

    private static CrossRoleCoordinationData CreateEmptyCrossRoleData() =>
        new()
        {
            CoincidenceCount = new Dictionary<string, int>(),
            RolePairDetails = Array.Empty<RolePairCoincidence>(),
            LockScores = new Dictionary<string, double>()
        };

    private static AnchorCandidateData CreateEmptyAnchorData() =>
        new()
        {
            RoleAnchors = new Dictionary<string, IReadOnlyList<PositionConsistency>>(),
            ConsistentPositionMasks = new Dictionary<string, long>(),
            PopRockAnchorVariance = null
        };

    private static StructuralMarkerData CreateEmptyStructuralData() =>
        new()
        {
            HighDensityBars = Array.Empty<DensityAnomaly>(),
            LowDensityBars = Array.Empty<DensityAnomaly>(),
            CrashBars = Array.Empty<int>(),
            PatternChanges = Array.Empty<PatternChangePoint>(),
            PotentialFills = Array.Empty<PotentialFill>()
        };

    private static VelocityDynamicsData CreateEmptyVelocityData() =>
        new()
        {
            RoleDistributions = new Dictionary<string, VelocityDistribution>(),
            RoleVelocityByPosition = new Dictionary<string, IReadOnlyList<double>>(),
            AccentMasks = new Dictionary<string, long>(),
            GhostPositions = Array.Empty<int>()
        };

    private static TimingFeelData CreateEmptyTimingData() =>
        new()
        {
            RoleAverageOffset = new Dictionary<string, double>(),
            RoleTimingDistributions = new Dictionary<string, TimingDistribution>(),
            SwingRatio = 1.0,
            AheadBehindScore = 0,
            TimingConsistency = 1.0
        };
}

/// <summary>
/// Options for controlling which analyses are performed.
/// </summary>
public sealed record ExtendedAnalysisOptions
{
    /// <summary>
    /// Include cross-role coordination analysis.
    /// </summary>
    public bool IncludeCrossRoleAnalysis { get; init; } = true;

    /// <summary>
    /// Include anchor candidate analysis.
    /// </summary>
    public bool IncludeAnchorAnalysis { get; init; } = true;

    /// <summary>
    /// Include PopRock reference anchor comparison.
    /// </summary>
    public bool IncludePopRockReference { get; init; } = true;

    /// <summary>
    /// Include structural marker detection.
    /// </summary>
    public bool IncludeStructuralAnalysis { get; init; } = true;

    /// <summary>
    /// Include velocity and timing feel analysis.
    /// </summary>
    public bool IncludePerformanceAnalysis { get; init; } = true;

    /// <summary>
    /// Default options (all analyses enabled).
    /// </summary>
    public static ExtendedAnalysisOptions Default { get; } = new();
}
