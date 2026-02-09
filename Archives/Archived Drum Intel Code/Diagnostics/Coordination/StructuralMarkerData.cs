// AI: purpose=Structural marker data for identifying fills, crashes, section changes (Story 7.2b).
// AI: invariants=DensityAnomaly deviation in std devs; PotentialFill confidence in [0,1]; bars 1-based.
// AI: deps=Populated by StructuralMarkerDetector; uses BarOnsetStats and BarPatternFingerprint.
// AI: change=Story 7.2b; tune detection thresholds based on analysis feedback.

namespace Music.Generator.Drums.Diagnostics.Coordination;

/// <summary>
/// A bar with significantly different density than average.
/// Story 7.2b: Structural Marker Detection.
/// </summary>
/// <param name="BarNumber">Bar number (1-based).</param>
/// <param name="EventCount">Number of events in this bar.</param>
/// <param name="DeviationFromMean">Standard deviations from mean density.</param>
public sealed record DensityAnomaly(
    int BarNumber,
    int EventCount,
    double DeviationFromMean);

/// <summary>
/// A point where the pattern changes significantly.
/// Story 7.2b: Structural Marker Detection.
/// </summary>
/// <param name="BarNumber">Bar where the change occurs (1-based).</param>
/// <param name="PreviousPatternHash">Hash of the previous bar's pattern.</param>
/// <param name="NewPatternHash">Hash of this bar's pattern.</param>
/// <param name="Similarity">How similar the patterns are (0.0 = completely different).</param>
public sealed record PatternChangePoint(
    int BarNumber,
    string PreviousPatternHash,
    string NewPatternHash,
    double Similarity);

/// <summary>
/// A potential fill location identified by heuristics.
/// Story 7.2b: Structural Marker Detection.
/// </summary>
/// <param name="StartBar">First bar of the potential fill (1-based).</param>
/// <param name="EndBar">Last bar of the potential fill (1-based, often same as StartBar).</param>
/// <param name="Confidence">Confidence score (0.0-1.0) based on indicators.</param>
/// <param name="IndicatorReasons">Reasons why this is flagged as a potential fill.</param>
public sealed record PotentialFill(
    int StartBar,
    int EndBar,
    double Confidence,
    IReadOnlyList<string> IndicatorReasons);

/// <summary>
/// Data about structural markers in a drum track.
/// Identifies fills, density anomalies, crashes, and pattern changes.
/// Story 7.2b: Structural Marker Detection.
/// </summary>
public sealed record StructuralMarkerData
{
    /// <summary>
    /// Bars with significantly higher density than average.
    /// </summary>
    public required IReadOnlyList<DensityAnomaly> HighDensityBars { get; init; }

    /// <summary>
    /// Bars with significantly lower density than average.
    /// </summary>
    public required IReadOnlyList<DensityAnomaly> LowDensityBars { get; init; }

    /// <summary>
    /// Bars containing crash cymbal hits (potential section starts).
    /// </summary>
    public required IReadOnlyList<int> CrashBars { get; init; }

    /// <summary>
    /// Points where the pattern changes significantly from previous bar.
    /// </summary>
    public required IReadOnlyList<PatternChangePoint> PatternChanges { get; init; }

    /// <summary>
    /// Potential fill locations identified by multiple indicators.
    /// </summary>
    public required IReadOnlyList<PotentialFill> PotentialFills { get; init; }

    /// <summary>
    /// Total number of structural markers detected.
    /// </summary>
    public int TotalMarkers =>
        HighDensityBars.Count + LowDensityBars.Count + CrashBars.Count +
        PatternChanges.Count + PotentialFills.Count;
}
