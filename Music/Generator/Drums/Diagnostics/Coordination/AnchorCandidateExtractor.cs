// AI: purpose=Extracts anchor candidates by analyzing position consistency across bars (Story 7.2b).
// AI: invariants=Anchor threshold >= 0.8; reference comparison is optional; deterministic output.
// AI: deps=Uses BeatPositionMatrix for efficient lookup; outputs AnchorCandidateData.
// AI: change=Story 7.2b; add custom reference anchors as needed.

using Music.Generator.Drums.Diagnostics.BarAnalysis;

namespace Music.Generator.Drums.Diagnostics.Coordination;

/// <summary>
/// Extracts anchor candidates by analyzing position consistency across bars.
/// Positions hit in >= 80% of bars are considered anchors.
/// Story 7.2b: Anchor Candidate Detection.
/// </summary>
public static class AnchorCandidateExtractor
{
    /// <summary>
    /// Minimum consistency ratio for a position to be considered an anchor.
    /// </summary>
    public const double AnchorThreshold = 0.8;

    /// <summary>
    /// Reference anchor for PopRock: expected positions per role.
    /// Grid positions are 0-based (16th grid in 4/4):
    /// - Position 0 = beat 1, Position 4 = beat 2, Position 8 = beat 3, Position 12 = beat 4
    /// - Position 2 = beat 1.5, Position 6 = beat 2.5, etc.
    /// </summary>
    private static readonly Dictionary<string, long> PopRockReferenceAnchors = new()
    {
        // Kick on beats 1 and 3 (positions 0, 8)
        ["Kick"] = (1L << 0) | (1L << 8),

        // Snare on beats 2 and 4 (positions 4, 12)
        ["Snare"] = (1L << 4) | (1L << 12),

        // ClosedHat on all 8th notes (positions 0, 2, 4, 6, 8, 10, 12, 14)
        ["ClosedHat"] = (1L << 0) | (1L << 2) | (1L << 4) | (1L << 6) |
                        (1L << 8) | (1L << 10) | (1L << 12) | (1L << 14)
    };

    /// <summary>
    /// Extracts anchor candidates from role matrices.
    /// </summary>
    /// <param name="roleMatrices">Beat position matrices per role.</param>
    /// <param name="includePopRockReference">Include comparison to PopRock reference anchors.</param>
    /// <returns>Anchor candidate data.</returns>
    public static AnchorCandidateData Extract(
        IReadOnlyDictionary<string, BeatPositionMatrix> roleMatrices,
        bool includePopRockReference = true)
    {
        ArgumentNullException.ThrowIfNull(roleMatrices);

        if (roleMatrices.Count == 0)
        {
            return CreateEmpty();
        }

        var roleAnchors = new Dictionary<string, IReadOnlyList<PositionConsistency>>();
        var consistentMasks = new Dictionary<string, long>();

        foreach (var (role, matrix) in roleMatrices.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            var (anchors, mask) = ExtractRoleAnchors(matrix);
            roleAnchors[role] = anchors;
            consistentMasks[role] = mask;
        }

        AnchorVarianceFromReference? popRockVariance = null;
        if (includePopRockReference)
        {
            popRockVariance = ComputePopRockVariance(consistentMasks);
        }

        return new AnchorCandidateData
        {
            RoleAnchors = roleAnchors,
            ConsistentPositionMasks = consistentMasks,
            PopRockAnchorVariance = popRockVariance
        };
    }

    /// <summary>
    /// Extracts anchor positions for a single role.
    /// </summary>
    private static (IReadOnlyList<PositionConsistency> anchors, long mask) ExtractRoleAnchors(
        BeatPositionMatrix matrix)
    {
        var anchors = new List<PositionConsistency>();
        long mask = 0;

        if (matrix.TotalBars == 0)
            return (anchors, mask);

        // Count hits at each grid position across all bars
        var positionCounts = new int[matrix.GridResolution];

        for (int barIndex = 0; barIndex < matrix.TotalBars; barIndex++)
        {
            for (int pos = 0; pos < matrix.GridResolution; pos++)
            {
                if (matrix.GetSlot(barIndex, pos) != null)
                    positionCounts[pos]++;
            }
        }

        // Identify positions that meet anchor threshold
        for (int pos = 0; pos < matrix.GridResolution; pos++)
        {
            var hitCount = positionCounts[pos];
            var ratio = (double)hitCount / matrix.TotalBars;

            if (ratio >= AnchorThreshold)
            {
                anchors.Add(new PositionConsistency(pos, hitCount, matrix.TotalBars, ratio));

                if (pos < 64)
                    mask |= 1L << pos;
            }
        }

        return (anchors, mask);
    }

    /// <summary>
    /// Computes variance from PopRock reference anchor.
    /// </summary>
    private static AnchorVarianceFromReference ComputePopRockVariance(
        IReadOnlyDictionary<string, long> detectedMasks)
    {
        var perRoleVariance = new Dictionary<string, double>();
        var missingAnchors = new List<string>();
        var extraAnchors = new List<string>();

        double totalVariance = 0;
        int roleCount = 0;

        foreach (var (role, referenceMask) in PopRockReferenceAnchors)
        {
            var detectedMask = detectedMasks.TryGetValue(role, out var m) ? m : 0L;

            // Calculate Jaccard distance (1 - similarity)
            var intersection = referenceMask & detectedMask;
            var union = referenceMask | detectedMask;

            var intersectionCount = CountBits(intersection);
            var unionCount = CountBits(union);

            var similarity = unionCount > 0 ? (double)intersectionCount / unionCount : 1.0;
            var variance = 1.0 - similarity;

            perRoleVariance[role] = variance;
            totalVariance += variance;
            roleCount++;

            // Track missing anchors (in reference but not detected)
            var missing = referenceMask & ~detectedMask;
            if (missing != 0)
            {
                for (int pos = 0; pos < 64; pos++)
                {
                    if ((missing & (1L << pos)) != 0)
                        missingAnchors.Add($"{role}@{pos}");
                }
            }

            // Track extra anchors (detected but not in reference)
            var extra = detectedMask & ~referenceMask;
            if (extra != 0)
            {
                for (int pos = 0; pos < 64; pos++)
                {
                    if ((extra & (1L << pos)) != 0)
                        extraAnchors.Add($"{role}@{pos}");
                }
            }
        }

        // Check for roles in detected but not in reference (all are "extra")
        foreach (var (role, mask) in detectedMasks)
        {
            if (!PopRockReferenceAnchors.ContainsKey(role) && mask != 0)
            {
                for (int pos = 0; pos < 64; pos++)
                {
                    if ((mask & (1L << pos)) != 0)
                        extraAnchors.Add($"{role}@{pos}");
                }
            }
        }

        var overallVariance = roleCount > 0 ? totalVariance / roleCount : 0.0;

        return new AnchorVarianceFromReference(
            "PopRockBasic",
            overallVariance,
            perRoleVariance,
            missingAnchors.OrderBy(s => s, StringComparer.Ordinal).ToList(),
            extraAnchors.OrderBy(s => s, StringComparer.Ordinal).ToList());
    }

    /// <summary>
    /// Counts set bits in a long value.
    /// </summary>
    private static int CountBits(long value)
    {
        return System.Numerics.BitOperations.PopCount((ulong)value);
    }

    /// <summary>
    /// Creates empty anchor candidate data.
    /// </summary>
    private static AnchorCandidateData CreateEmpty()
    {
        return new AnchorCandidateData
        {
            RoleAnchors = new Dictionary<string, IReadOnlyList<PositionConsistency>>(),
            ConsistentPositionMasks = new Dictionary<string, long>(),
            PopRockAnchorVariance = null
        };
    }
}
