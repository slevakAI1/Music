// AI: purpose=Pattern similarity analysis data for identifying related drum patterns (Story 7.2b).
// AI: invariants=SimilarPairs have Similarity >= threshold (0.7); PatternFamilies group similar patterns.
// AI: deps=Populated by PatternSimilarityAnalyzer; uses Jaccard similarity from BarPatternExtractor.
// AI: change=Story 7.2b; tune similarity threshold based on analysis feedback.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// A pair of patterns that are similar but not identical.
/// Story 7.2b: Pattern Similarity Analysis.
/// </summary>
/// <param name="PatternHashA">First pattern hash.</param>
/// <param name="PatternHashB">Second pattern hash.</param>
/// <param name="Similarity">Jaccard similarity score (0.0-1.0).</param>
public sealed record SimilarPatternPair(
    string PatternHashA,
    string PatternHashB,
    double Similarity);

/// <summary>
/// A family of patterns that are variations of each other.
/// Story 7.2b: Pattern Similarity Analysis.
/// </summary>
/// <param name="BasePatternHash">The most common pattern in the family (anchor).</param>
/// <param name="VariantHashes">Other patterns in this family (similar to base).</param>
/// <param name="AllBarNumbers">All bar numbers where any family member appears.</param>
public sealed record PatternFamily(
    string BasePatternHash,
    IReadOnlyList<string> VariantHashes,
    IReadOnlyList<int> AllBarNumbers);

/// <summary>
/// Data about pattern similarity relationships in a drum track.
/// Identifies similar patterns and groups them into families.
/// Story 7.2b: Pattern Similarity Analysis.
/// </summary>
public sealed record PatternSimilarityData
{
    /// <summary>
    /// Pairs of patterns that are similar (above threshold).
    /// Excludes identical patterns and self-pairs.
    /// </summary>
    public required IReadOnlyList<SimilarPatternPair> SimilarPairs { get; init; }

    /// <summary>
    /// Groups of patterns that are variations of each other.
    /// Each family has a base pattern and related variants.
    /// </summary>
    public required IReadOnlyList<PatternFamily> PatternFamilies { get; init; }

    /// <summary>
    /// Average similarity across all similar pairs.
    /// </summary>
    public double AverageSimilarity => SimilarPairs.Count > 0
        ? SimilarPairs.Average(p => p.Similarity)
        : 0.0;

    /// <summary>
    /// Total number of patterns in families (base + variants).
    /// </summary>
    public int PatternsInFamilies => PatternFamilies.Sum(f => 1 + f.VariantHashes.Count);
}
