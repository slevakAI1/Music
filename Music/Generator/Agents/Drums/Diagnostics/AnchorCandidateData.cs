// AI: purpose=Anchor candidate detection data for identifying consistent beat positions (Story 7.2b).
// AI: invariants=ConsistencyRatio in [0,1]; positions with >= 80% consistency are anchors.
// AI: deps=Populated by AnchorCandidateExtractor; enables groove anchor comparison.
// AI: change=Story 7.2b; add reference anchor comparison as needed.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Consistency information for a specific grid position.
/// Story 7.2b: Anchor Candidate Detection.
/// </summary>
/// <param name="GridPosition">Grid position (0-based, e.g., 0-15 for 16th grid in 4/4).</param>
/// <param name="HitCount">Number of bars with a hit at this position.</param>
/// <param name="TotalBars">Total bars analyzed.</param>
/// <param name="ConsistencyRatio">HitCount / TotalBars (0.0-1.0).</param>
public sealed record PositionConsistency(
    int GridPosition,
    int HitCount,
    int TotalBars,
    double ConsistencyRatio);

/// <summary>
/// Variance from a reference anchor pattern.
/// Story 7.2b: Anchor Candidate Detection.
/// </summary>
/// <param name="ReferenceName">Name of the reference anchor (e.g., "PopRockBasic").</param>
/// <param name="OverallVarianceScore">Overall variance (0.0 = perfect match, 1.0 = no match).</param>
/// <param name="PerRoleVariance">Variance per role (0.0-1.0).</param>
/// <param name="MissingAnchors">Expected anchor positions not found in the track.</param>
/// <param name="ExtraAnchors">Anchor positions found but not in reference.</param>
public sealed record AnchorVarianceFromReference(
    string ReferenceName,
    double OverallVarianceScore,
    IReadOnlyDictionary<string, double> PerRoleVariance,
    IReadOnlyList<string> MissingAnchors,
    IReadOnlyList<string> ExtraAnchors);

/// <summary>
/// Data about anchor candidates in a drum track.
/// Identifies consistently hit positions that could serve as groove anchors.
/// Story 7.2b: Anchor Candidate Detection.
/// </summary>
public sealed record AnchorCandidateData
{
    /// <summary>
    /// Per-role anchor positions (consistently hit).
    /// Key: role name, Value: list of position consistencies.
    /// </summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<PositionConsistency>> RoleAnchors { get; init; }

    /// <summary>
    /// Bitmask of consistently hit positions per role.
    /// Key: role, Value: bitmask where bit N = anchor at grid position N.
    /// </summary>
    public required IReadOnlyDictionary<string, long> ConsistentPositionMasks { get; init; }

    /// <summary>
    /// Variance from reference anchor (e.g., PopRockBasic).
    /// Null if no reference was provided.
    /// </summary>
    public AnchorVarianceFromReference? PopRockAnchorVariance { get; init; }

    /// <summary>
    /// Total number of anchor positions across all roles.
    /// </summary>
    public int TotalAnchorPositions => RoleAnchors.Values.Sum(l => l.Count);

    /// <summary>
    /// Average consistency ratio across all detected anchors.
    /// </summary>
    public double AverageAnchorConsistency
    {
        get
        {
            var allAnchors = RoleAnchors.Values.SelectMany(l => l).ToList();
            return allAnchors.Count > 0 ? allAnchors.Average(a => a.ConsistencyRatio) : 0.0;
        }
    }
}
