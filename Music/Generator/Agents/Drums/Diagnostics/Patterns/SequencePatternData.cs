// AI: purpose=Multi-bar sequence detection data for identifying recurring patterns (Story 7.2b).
// AI: invariants=Sequences are consecutive bars; EvolvingSequence tracks gradual pattern drift.
// AI: deps=Populated by SequencePatternDetector; uses BarPatternFingerprint hashes.
// AI: change=Story 7.2b; extend with longer sequences as needed.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// A multi-bar sequence that repeats across the track.
/// Story 7.2b: Multi-Bar Sequence Detection.
/// </summary>
/// <param name="PatternHashes">One hash per bar in the sequence.</param>
/// <param name="Occurrences">Start bars where this sequence appears.</param>
/// <param name="SequenceLength">Number of bars in the sequence.</param>
public sealed record MultiBarSequence(
    IReadOnlyList<string> PatternHashes,
    IReadOnlyList<int> Occurrences,
    int SequenceLength);

/// <summary>
/// A step in an evolving sequence showing gradual pattern change.
/// Story 7.2b: Multi-Bar Sequence Detection.
/// </summary>
/// <param name="BarNumber">Bar number (1-based).</param>
/// <param name="PatternHash">Pattern hash at this bar.</param>
/// <param name="SimilarityToBase">How similar to the base pattern (0.0-1.0).</param>
public sealed record EvolutionStep(
    int BarNumber,
    string PatternHash,
    double SimilarityToBase);

/// <summary>
/// A sequence showing gradual evolution from a base pattern.
/// Story 7.2b: Multi-Bar Sequence Detection.
/// </summary>
/// <param name="BasePatternHash">The starting pattern.</param>
/// <param name="Steps">Evolution steps showing gradual change.</param>
/// <param name="TotalBarsSpanned">Total bars from first to last step.</param>
public sealed record EvolvingSequence(
    string BasePatternHash,
    IReadOnlyList<EvolutionStep> Steps,
    int TotalBarsSpanned);

/// <summary>
/// Data about multi-bar sequences in a drum track.
/// Identifies repeating 2-bar and 4-bar sequences and evolving patterns.
/// Story 7.2b: Multi-Bar Sequence Detection.
/// </summary>
public sealed record SequencePatternData
{
    /// <summary>
    /// Recurring 2-bar sequences found in the track.
    /// </summary>
    public required IReadOnlyList<MultiBarSequence> TwoBarSequences { get; init; }

    /// <summary>
    /// Recurring 4-bar sequences found in the track.
    /// </summary>
    public required IReadOnlyList<MultiBarSequence> FourBarSequences { get; init; }

    /// <summary>
    /// Patterns that evolve gradually over multiple bars.
    /// </summary>
    public required IReadOnlyList<EvolvingSequence> EvolvingSequences { get; init; }

    /// <summary>
    /// Total number of detected sequences.
    /// </summary>
    public int TotalSequences =>
        TwoBarSequences.Count + FourBarSequences.Count + EvolvingSequences.Count;
}
