// AI: purpose=Weighted random selection of drum onset candidates with deterministic RNG.
// AI: invariants=Same seed+inputs => same output; tie-break by weight desc then StableId asc for determinism.
// AI: deps=Uses DrumCandidateGroup, DrumOnsetCandidate, GrooveRngHelper and Rng helper for deterministic RNG.

using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators;

// AI: record=WeightedCandidate pairs a mapped DrumOnsetCandidate with its Group and computed weight
public sealed record WeightedCandidate(
    DrumOnsetCandidate Candidate,
    DrumCandidateGroup Group,
    double ComputedWeight,
    string StableId);

// AI: selector=Provides deterministic weighted selection utilities for drum onset candidates
public static class Save_DrumWeightedCandidateSelector
{
    // AI: calc=Weight = candidate.ProbabilityBias * group.BaseProbabilityBias; non-positive treated as 0
    public static double ComputeWeight(DrumOnsetCandidate candidate, DrumCandidateGroup group)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(group);

        double candidateBias = candidate.ProbabilityBias;
        double groupBias = group.BaseProbabilityBias;

        if (candidateBias <= 0 || groupBias <= 0)
        {
            return 0.0;
        }

        return candidateBias * groupBias;
    }

    // AI: id=Stable ID format "{GroupId}:{OnsetBeat:F4}" used for deterministic tie-breaking
    public static string CreateStableId(DrumCandidateGroup group, DrumOnsetCandidate candidate)
    {
        return $"{group.GroupId}:{candidate.OnsetBeat:F4}";
    }

    // AI: build=Creates WeightedCandidate list, filtering zero-weight entries; sorts by weight desc then id asc
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
                if (weight <= 0)
                    continue;

                string stableId = CreateStableId(group, candidate);
                weighted.Add(new WeightedCandidate(candidate, group, weight, stableId));
            }
        }

        return weighted
            .OrderByDescending(w => w.ComputedWeight)
            .ThenBy(w => w.StableId, StringComparer.Ordinal)
            .ToList();
    }

    // AI: select=Deterministic weighted random selection using GrooveRngHelper.RngFor(barNumber,role,streamKey)
    public static IReadOnlyList<WeightedCandidate> SelectCandidates(
        IEnumerable<DrumCandidateGroup> groups,
        int targetCount,
        int barNumber,
        string role)
    {
        ArgumentNullException.ThrowIfNull(groups);

        if (targetCount <= 0)
            return Array.Empty<WeightedCandidate>();

        var weightedCandidates = BuildWeightedCandidates(groups);
        if (weightedCandidates.Count == 0)
            return Array.Empty<WeightedCandidate>();

        var rngPurpose = GrooveRngHelper.RngFor(barNumber, role, GrooveRngStreamKey.CandidatePick);

        var selected = new List<WeightedCandidate>();
        var remaining = new List<WeightedCandidate>(weightedCandidates);

        while (selected.Count < targetCount && remaining.Count > 0)
        {
            double totalWeight = remaining.Sum(w => w.ComputedWeight);
            if (totalWeight <= 0)
                break;

            double randomValue = Rng.NextDouble(rngPurpose) * totalWeight;

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

            selectedCandidate ??= remaining[^1];

            selected.Add(selectedCandidate);
            remaining.Remove(selectedCandidate);
        }

        return selected;
    }

    // AI: helper=Convenience overload for selecting from a single group
    public static IReadOnlyList<WeightedCandidate> SelectFromGroup(
        DrumCandidateGroup group,
        int targetCount,
        int barNumber,
        string role)
    {
        ArgumentNullException.ThrowIfNull(group);
        return SelectCandidates(new[] { group }, targetCount, barNumber, role);
    }

    // AI: top=Return top-N by deterministic weight ordering; topN<=0 returns all
    public static IReadOnlyList<WeightedCandidate> GetTopByWeight(
        IEnumerable<DrumCandidateGroup> groups,
        int topN = 0)
    {
        var weighted = BuildWeightedCandidates(groups);

        if (topN <= 0 || topN >= weighted.Count)
            return weighted;

        return weighted.Take(topN).ToList();
    }
}
