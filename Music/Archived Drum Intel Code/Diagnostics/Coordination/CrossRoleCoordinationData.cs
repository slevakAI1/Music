// AI: purpose=Cross-role coordination data for analyzing multi-role timing relationships (Story 7.2b).
// AI: invariants=RolePair keys alphabetically sorted; LockScores in [0,1]; coincidence count is actual overlap.
// AI: deps=Populated by CrossRoleCoordinationExtractor; uses BeatPositionMatrix for efficient lookup.
// AI: change=Story 7.2b; add additional coordination metrics as needed.

namespace Music.Generator.Drums.Diagnostics.Coordination;

/// <summary>
/// Coordination details for a specific pair of roles.
/// Story 7.2b: Cross-Role Coordination Data.
/// </summary>
/// <param name="RoleA">First role (alphabetically first).</param>
/// <param name="RoleB">Second role (alphabetically second).</param>
/// <param name="TotalCoincidences">Number of beat positions where both roles hit.</param>
/// <param name="CoincidenceRatio">Coincidences / min(roleAHits, roleBHits).</param>
/// <param name="CommonPositionMask">Bitmask of grid positions where both hit (across all bars).</param>
public sealed record RolePairCoincidence(
    string RoleA,
    string RoleB,
    int TotalCoincidences,
    double CoincidenceRatio,
    long CommonPositionMask);

/// <summary>
/// Data about cross-role coordination patterns in a drum track.
/// Measures how roles interact temporally (hitting together).
/// Story 7.2b: Cross-Role Coordination Data.
/// </summary>
public sealed record CrossRoleCoordinationData
{
    /// <summary>
    /// Coincidence matrix: how often two roles hit at the same beat position.
    /// Key: "RoleA+RoleB" (alphabetically sorted), Value: count.
    /// </summary>
    public required IReadOnlyDictionary<string, int> CoincidenceCount { get; init; }

    /// <summary>
    /// Detailed coordination data for key role pairs.
    /// </summary>
    public required IReadOnlyList<RolePairCoincidence> RolePairDetails { get; init; }

    /// <summary>
    /// Lock score for role pairs: how tightly two roles follow each other.
    /// Key: "RoleA+RoleB", Value: 0.0-1.0 (higher = more coordinated).
    /// Based on coincidence ratio.
    /// </summary>
    public required IReadOnlyDictionary<string, double> LockScores { get; init; }

    /// <summary>
    /// Gets the lock score for a specific role pair.
    /// Returns 0 if the pair is not found.
    /// </summary>
    public double GetLockScore(string roleA, string roleB)
    {
        var key = CreateKey(roleA, roleB);
        return LockScores.TryGetValue(key, out var score) ? score : 0.0;
    }

    /// <summary>
    /// Creates a standardized key for a role pair (alphabetically sorted).
    /// </summary>
    public static string CreateKey(string roleA, string roleB)
    {
        return string.Compare(roleA, roleB, StringComparison.Ordinal) <= 0
            ? $"{roleA}+{roleB}"
            : $"{roleB}+{roleA}";
    }
}
