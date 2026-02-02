// AI: purpose=Extracts cross-role coordination data from beat position matrices (Story 7.2b).
// AI: invariants=Deterministic output; computes pairwise coordination for all role combinations.
// AI: deps=Uses BeatPositionMatrix for efficient positional lookup; outputs CrossRoleCoordinationData.
// AI: change=Story 7.2b; optimize for large track analysis if needed.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Extracts cross-role coordination data from beat position matrices.
/// Computes coincidence counts, ratios, and lock scores between role pairs.
/// Story 7.2b: Cross-Role Coordination Data.
/// </summary>
public static class CrossRoleCoordinationExtractor
{
    /// <summary>
    /// Key role pairs to analyze in detail (in addition to all pairs).
    /// </summary>
    private static readonly (string, string)[] KeyRolePairs =
    {
        ("Kick", "Snare"),
        ("Kick", "ClosedHat"),
        ("Snare", "ClosedHat"),
        ("Kick", "Bass"),
        ("ClosedHat", "Crash")
    };

    /// <summary>
    /// Extracts cross-role coordination data from role matrices.
    /// </summary>
    /// <param name="roleMatrices">Beat position matrices per role.</param>
    /// <returns>Cross-role coordination data.</returns>
    public static CrossRoleCoordinationData Extract(
        IReadOnlyDictionary<string, BeatPositionMatrix> roleMatrices)
    {
        ArgumentNullException.ThrowIfNull(roleMatrices);

        if (roleMatrices.Count < 2)
        {
            return CreateEmpty();
        }

        var roles = roleMatrices.Keys.OrderBy(r => r, StringComparer.Ordinal).ToList();
        var coincidenceCount = new Dictionary<string, int>();
        var lockScores = new Dictionary<string, double>();
        var pairDetails = new List<RolePairCoincidence>();

        // Process all unique role pairs
        for (int i = 0; i < roles.Count; i++)
        {
            for (int j = i + 1; j < roles.Count; j++)
            {
                var roleA = roles[i];
                var roleB = roles[j];
                var key = CrossRoleCoordinationData.CreateKey(roleA, roleB);

                var matrixA = roleMatrices[roleA];
                var matrixB = roleMatrices[roleB];

                var (coincidences, commonMask, hitsA, hitsB) = ComputeCoincidence(matrixA, matrixB);

                coincidenceCount[key] = coincidences;

                // Compute coincidence ratio and lock score
                var minHits = Math.Min(hitsA, hitsB);
                var ratio = minHits > 0 ? (double)coincidences / minHits : 0.0;
                lockScores[key] = ratio;

                // Add detailed pair data for key pairs or any with significant coincidence
                if (IsKeyPair(roleA, roleB) || coincidences > 0)
                {
                    pairDetails.Add(new RolePairCoincidence(
                        roleA,
                        roleB,
                        coincidences,
                        ratio,
                        commonMask));
                }
            }
        }

        // Sort pair details by coincidence count descending
        var sortedDetails = pairDetails
            .OrderByDescending(p => p.TotalCoincidences)
            .ThenBy(p => p.RoleA, StringComparer.Ordinal)
            .ThenBy(p => p.RoleB, StringComparer.Ordinal)
            .ToList();

        return new CrossRoleCoordinationData
        {
            CoincidenceCount = coincidenceCount,
            RolePairDetails = sortedDetails,
            LockScores = lockScores
        };
    }

    /// <summary>
    /// Computes coincidence between two role matrices.
    /// </summary>
    private static (int coincidences, long commonMask, int hitsA, int hitsB) ComputeCoincidence(
        BeatPositionMatrix matrixA,
        BeatPositionMatrix matrixB)
    {
        int coincidences = 0;
        long commonMask = 0;
        int hitsA = 0;
        int hitsB = 0;

        var barCount = Math.Min(matrixA.TotalBars, matrixB.TotalBars);
        var gridRes = Math.Min(matrixA.GridResolution, matrixB.GridResolution);

        for (int barIndex = 0; barIndex < barCount; barIndex++)
        {
            for (int pos = 0; pos < gridRes; pos++)
            {
                var slotA = matrixA.GetSlot(barIndex, pos);
                var slotB = matrixB.GetSlot(barIndex, pos);

                if (slotA != null) hitsA++;
                if (slotB != null) hitsB++;

                if (slotA != null && slotB != null)
                {
                    coincidences++;

                    // Track position in common mask (accumulates across bars)
                    if (pos < 64)
                        commonMask |= 1L << pos;
                }
            }
        }

        return (coincidences, commonMask, hitsA, hitsB);
    }

    /// <summary>
    /// Checks if a role pair is one of the key pairs to analyze.
    /// </summary>
    private static bool IsKeyPair(string roleA, string roleB)
    {
        foreach (var (a, b) in KeyRolePairs)
        {
            if ((roleA == a && roleB == b) || (roleA == b && roleB == a))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Creates empty cross-role coordination data.
    /// </summary>
    private static CrossRoleCoordinationData CreateEmpty()
    {
        return new CrossRoleCoordinationData
        {
            CoincidenceCount = new Dictionary<string, int>(),
            RolePairDetails = Array.Empty<RolePairCoincidence>(),
            LockScores = new Dictionary<string, double>()
        };
    }
}
