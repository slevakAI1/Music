// AI: purpose=Weighted selection of groove candidates with deterministic RNG and tie-breaking (Story B3).
// AI: invariants=Same seed => identical selections; deterministic tie-break by weight desc, then stable id.
// AI: deps=DrumCandidateGroup, DrumOnsetCandidate, GrooveRngHelper for RNG access.
// AI: change=Story B3 acceptance criteria: weighted selection using RngFor(bar, role, VariationPick).

using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Selection;

/// <summary>
/// Weighted candidate record with computed weight and group context.
/// Used for selection sorting and tracking.
/// </summary>
public sealed record WeightedCandidate(
    DrumOnsetCandidate Candidate,
    DrumCandidateGroup Group,
    double ComputedWeight,
    string StableId);

/// <summary>
/// Selects groove candidates using weighted random selection with deterministic RNG.
/// Story B3: Implements weighted selection with stable tie-breaking.
/// </summary>
public static class DrumWeightedCandidateSelector
{
    /// <summary>
    /// Computes the effective weight for a candidate.
    /// Story B3: Weight = ProbabilityBias * Group.BaseProbabilityBias.
    /// </summary>
    /// <param name="candidate">The candidate to compute weight for.</param>
    /// <param name="group">The group containing this candidate.</param>
    /// <returns>Computed weight (0 or positive). Zero/negative bias treated as 0.</returns>
    public static double ComputeWeight(DrumOnsetCandidate candidate, DrumCandidateGroup group)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(group);

        double candidateBias = candidate.ProbabilityBias;
        double groupBias = group.BaseProbabilityBias;

        // Story B3: Zero/negative weights treated as 0
        if (candidateBias <= 0 || groupBias <= 0)
        {
            return 0.0;
        }

        return candidateBias * groupBias;
    }

    /// <summary>
    /// Creates a stable ID for a candidate for deterministic tie-breaking.
    /// Format: "{GroupId}:{OnsetBeat}" for uniqueness within a group.
    /// </summary>
    public static string CreateStableId(DrumCandidateGroup group, DrumOnsetCandidate candidate)
    {
        return $"{group.GroupId}:{candidate.OnsetBeat:F4}";
    }

    /// <summary>
    /// Builds weighted candidates from groups with computed weights.
    /// Filters out zero-weight candidates.
    /// </summary>
    /// <param name="groups">Candidate groups to process.</param>
    /// <returns>List of weighted candidates sorted for selection (weight desc, stable id asc).</returns>
    public static IReadOnlyList<WeightedCandidate> BuildWeightedCandidates(
        IEnumerable<DrumCandidateGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        var weighted = new List<WeightedCandidate>();

        foreach (var group in groups)
        {
            foreach (var candidate in group.Candidates)
            {
                double weight = ComputeWeight(candidate, group);

                // Story B3: Filter out zero/negative weights
                if (weight <= 0)
                {
                    continue;
                }

                string stableId = CreateStableId(group, candidate);
                weighted.Add(new WeightedCandidate(candidate, group, weight, stableId));
            }
        }

        // Story B3: Deterministic tie-breaking - weight desc, then stable id asc
        return weighted
            .OrderByDescending(w => w.ComputedWeight)
            .ThenBy(w => w.StableId, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Selects candidates using weighted random selection.
    /// Story B3: Uses RngFor(bar, role, VariationPick) for deterministic selection.
    /// </summary>
    /// <param name="groups">Candidate groups to select from.</param>
    /// <param name="targetCount">Maximum number of candidates to select.</param>
    /// <param name="barNumber">Bar number for RNG seed derivation.</param>
    /// <param name="role">Role for RNG seed derivation.</param>
    /// <returns>Selected candidates in selection order.</returns>
    public static IReadOnlyList<WeightedCandidate> SelectCandidates(
        IEnumerable<DrumCandidateGroup> groups,
        int targetCount,
        int barNumber,
        string role)
    {
        ArgumentNullException.ThrowIfNull(groups);

        if (targetCount <= 0)
        {
            return Array.Empty<WeightedCandidate>();
        }

        var weightedCandidates = BuildWeightedCandidates(groups);

        if (weightedCandidates.Count == 0)
        {
            return Array.Empty<WeightedCandidate>();
        }

        // Get RNG purpose for this selection
        var rngPurpose = GrooveRngHelper.RngFor(barNumber, role, GrooveRngStreamKey.CandidatePick);

        var selected = new List<WeightedCandidate>();
        var remaining = new List<WeightedCandidate>(weightedCandidates);

        while (selected.Count < targetCount && remaining.Count > 0)
        {
            // Compute total weight for probability distribution
            double totalWeight = remaining.Sum(w => w.ComputedWeight);

            if (totalWeight <= 0)
            {
                // All remaining have zero weight, stop selection
                break;
            }

            // Generate random value in [0, totalWeight)
            double randomValue = Rng.NextDouble(rngPurpose) * totalWeight;

            // Select candidate based on cumulative weight
            double cumulative = 0;
            WeightedCandidate? selectedCandidate = null;

            foreach (var candidate in remaining)
            {
                cumulative += candidate.ComputedWeight;
                if (randomValue < cumulative)
                {
                    selectedCandidate = candidate;
                    break;
                }
            }

            // Fallback to last candidate if rounding causes no selection
            selectedCandidate ??= remaining[^1];

            selected.Add(selectedCandidate);
            remaining.Remove(selectedCandidate);
        }

        return selected;
    }

    /// <summary>
    /// Selects candidates from a single group.
    /// Convenience overload for single-group scenarios.
    /// </summary>
    public static IReadOnlyList<WeightedCandidate> SelectFromGroup(
        DrumCandidateGroup group,
        int targetCount,
        int barNumber,
        string role)
    {
        ArgumentNullException.ThrowIfNull(group);
        return SelectCandidates(new[] { group }, targetCount, barNumber, role);
    }

    /// <summary>
    /// Gets candidates sorted by weight for deterministic ordering without random selection.
    /// Useful when you want the top N by weight without randomness.
    /// </summary>
    /// <param name="groups">Candidate groups to sort.</param>
    /// <param name="topN">Maximum number of candidates to return (0 = all).</param>
    /// <returns>Candidates sorted by weight desc, then stable id asc.</returns>
    public static IReadOnlyList<WeightedCandidate> GetTopByWeight(
        IEnumerable<DrumCandidateGroup> groups,
        int topN = 0)
    {
        var weighted = BuildWeightedCandidates(groups);

        if (topN <= 0 || topN >= weighted.Count)
        {
            return weighted;
        }

        return weighted.Take(topN).ToList();
    }
}
