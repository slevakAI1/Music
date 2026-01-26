// AI: purpose=Pattern repetition detection data for identifying recurring drum patterns (Story 7.2b).
// AI: invariants=PatternOccurrences keyed by PatternHash; MostCommonPatterns sorted by frequency desc; ConsecutiveRuns length >= 2.
// AI: deps=Populated by PatternRepetitionDetector; uses BarPatternFingerprint hashes for pattern identification.
// AI: change=Story 7.2b; extend with additional repetition metrics as needed.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Frequency information for a specific pattern.
/// Story 7.2b: Pattern Repetition Detection.
/// </summary>
/// <param name="PatternHash">Unique hash identifying the pattern.</param>
/// <param name="OccurrenceCount">Number of bars with this exact pattern.</param>
/// <param name="BarNumbers">1-based bar numbers where this pattern appears.</param>
public sealed record PatternFrequency(
    string PatternHash,
    int OccurrenceCount,
    IReadOnlyList<int> BarNumbers);

/// <summary>
/// A consecutive run of the same pattern across multiple bars.
/// Story 7.2b: Pattern Repetition Detection.
/// </summary>
/// <param name="PatternHash">Hash of the repeating pattern.</param>
/// <param name="StartBar">First bar of the run (1-based).</param>
/// <param name="EndBar">Last bar of the run (1-based, inclusive).</param>
/// <param name="Length">Number of consecutive bars with this pattern.</param>
public sealed record PatternRun(
    string PatternHash,
    int StartBar,
    int EndBar,
    int Length);

/// <summary>
/// Data about pattern repetition across a drum track.
/// Identifies which patterns repeat, how often, and where.
/// Story 7.2b: Pattern Repetition Detection.
/// </summary>
public sealed record PatternRepetitionData
{
    /// <summary>
    /// Pattern hash → list of bar numbers where it appears.
    /// Key: PatternHash, Value: list of 1-based bar numbers.
    /// </summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<int>> PatternOccurrences { get; init; }

    /// <summary>
    /// Count of unique patterns in the track.
    /// </summary>
    public required int UniquePatternCount { get; init; }

    /// <summary>
    /// Most common patterns sorted by occurrence count (descending).
    /// Limited to top 10 patterns.
    /// </summary>
    public required IReadOnlyList<PatternFrequency> MostCommonPatterns { get; init; }

    /// <summary>
    /// Consecutive repetition runs (same pattern for N bars in a row).
    /// Only includes runs of length >= 2.
    /// </summary>
    public required IReadOnlyList<PatternRun> ConsecutiveRuns { get; init; }

    /// <summary>
    /// Total number of bars analyzed.
    /// </summary>
    public required int TotalBars { get; init; }

    /// <summary>
    /// Ratio of unique patterns to total bars.
    /// Lower value = more repetition.
    /// </summary>
    public double UniquePatternRatio => TotalBars > 0 ? (double)UniquePatternCount / TotalBars : 0.0;

    /// <summary>
    /// Total number of bars covered by consecutive runs.
    /// </summary>
    public int BarsInConsecutiveRuns => ConsecutiveRuns.Sum(r => r.Length);
}
