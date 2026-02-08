using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Selection;

// AI: purpose=Select candidates up to a target with deterministic RNG, anchor preservation and caps
// AI: invariants=Deterministic given same inputs; never exceed targetCount; respects group/candidate caps
// AI: deps=Uses DrumWeightedCandidateSelector_Save, GrooveRngHelper, and GrooveDiagnosticsCollector_Save (optional)
public static class OperatorSelector_Save
{
    // AI: entry=Select until targetCount or pool exhausted; diagnostics optional for decision tracing
    public static IReadOnlyList<DrumOnsetCandidate> SelectUntilTargetReached(
        Bar bar,
        string role,
        IReadOnlyList<DrumCandidateGroup> groups,
        int targetCount,
        IReadOnlyList<GrooveOnset> existingAnchors,
        GrooveDiagnosticsCollector_Save? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(bar);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(existingAnchors);

        // Story C2: Return empty if target <= 0
        if (targetCount <= 0)
        {
            return Array.Empty<DrumOnsetCandidate>();
        }

        // AI: anchors=Collect beats for existing anchors of this role to avoid duplicate onset beats
        var anchorBeats = new HashSet<decimal>(
            existingAnchors
                .Where(a => string.Equals(a.Role, role, StringComparison.Ordinal))
                .Select(a => a.Beat));

        // AI: pool=Working pool excludes candidates conflicting with anchors; diagnostics record excluded ones
        var workingPool = BuildWorkingPool(groups, anchorBeats, diagnostics);

        // Story G1: Record candidate pool statistics
        diagnostics?.RecordCandidatePool(groups.Count, workingPool.Count);

        if (workingPool.Count == 0)
        {
            return Array.Empty<DrumOnsetCandidate>();
        }

        // Track remaining allowances for groups and candidates
        var groupAllowances = new Dictionary<string, int>(StringComparer.Ordinal);
        var candidateAllowances = new Dictionary<string, int>(StringComparer.Ordinal);

        InitializeAllowances(workingPool, groupAllowances, candidateAllowances);

        // Selection loop
        var selected = new List<DrumOnsetCandidate>();
        var remaining = new List<(DrumOnsetCandidate Candidate, DrumCandidateGroup Group)>(workingPool);

        while (selected.Count < targetCount && remaining.Count > 0)
        {
            // Filter remaining by current allowances
            var selectable = FilterByAllowances(remaining, groupAllowances, candidateAllowances);

            if (selectable.Count == 0)
            {
                // Pool exhausted due to caps
                break;
            }

            // Build groups for weighted selection
            var selectableGroups = BuildGroupsForSelection(selectable);



            // THIS IS WHERE IT's RANDOMLY SELECTING



            // Use weighted selector to pick one candidate
            var selectedCandidates = DrumWeightedCandidateSelector_Save.SelectCandidates(
                selectableGroups,
                targetCount: 1,
                barNumber: bar.BarNumber,
                role: role);

            if (selectedCandidates.Count == 0)
            {
                // No candidates selected (should not happen, but safety check)
                break;
            }

            var picked = selectedCandidates[0];
            selected.Add(picked.Candidate);

            // Story G1: Record selection decision
            if (diagnostics != null)
            {
                double weight = picked.Candidate.ProbabilityBias * picked.Group.BaseProbabilityBias;
                string candidateId = GrooveDiagnosticsCollector_Save.MakeCandidateId(picked.Group.GroupId, picked.Candidate.OnsetBeat);
                diagnostics.RecordSelection(candidateId, weight, RandomPurpose.GrooveCandidatePick);
            }

            // Update allowances
            UpdateAllowances(picked, groupAllowances, candidateAllowances);

            // Remove picked candidate from remaining pool
            remaining.RemoveAll(r =>
                r.Group.GroupId == picked.Group.GroupId &&
                r.Candidate.OnsetBeat == picked.Candidate.OnsetBeat);
        }

        return selected;
    }

    // AI: build=Compose pool of (candidate,group) excluding anchor conflicts; records filter events to diagnostics
    private static List<(DrumOnsetCandidate Candidate, DrumCandidateGroup Group)> BuildWorkingPool(
        IReadOnlyList<DrumCandidateGroup> groups,
        HashSet<decimal> anchorBeats,
        GrooveDiagnosticsCollector_Save? diagnostics = null)
    {
        var pool = new List<(DrumOnsetCandidate, DrumCandidateGroup)>();

        foreach (var group in groups)
        {
            foreach (var candidate in group.Candidates)
            {
                if (anchorBeats.Contains(candidate.OnsetBeat))
                {
                    if (diagnostics != null)
                    {
                        string candidateId = GrooveDiagnosticsCollector_Save.MakeCandidateId(group.GroupId, candidate.OnsetBeat);
                        diagnostics.RecordFilter(candidateId, "anchor conflict");
                    }
                }
                else
                {
                    pool.Add((candidate, group));
                }
            }
        }

        return pool;
    }

    // AI: allowances=Initialize group and candidate allowances; use int.MaxValue to represent unlimited
    private static void InitializeAllowances(
        List<(DrumOnsetCandidate Candidate, DrumCandidateGroup Group)> pool,
        Dictionary<string, int> groupAllowances,
        Dictionary<string, int> candidateAllowances)
    {
        foreach (var (candidate, group) in pool)
        {
            if (!groupAllowances.ContainsKey(group.GroupId))
            {
                groupAllowances[group.GroupId] = group.MaxAddsPerBar > 0
                    ? group.MaxAddsPerBar
                    : int.MaxValue;
            }

            string candidateKey = $"{group.GroupId}:{candidate.OnsetBeat:F4}";
            if (!candidateAllowances.ContainsKey(candidateKey))
            {
                candidateAllowances[candidateKey] = candidate.MaxAddsPerBar > 0
                    ? candidate.MaxAddsPerBar
                    : int.MaxValue;
            }
        }
    }

    // AI: filter=Return only remaining entries with positive group and candidate allowances
    private static List<(DrumOnsetCandidate Candidate, DrumCandidateGroup Group)> FilterByAllowances(
        List<(DrumOnsetCandidate Candidate, DrumCandidateGroup Group)> remaining,
        Dictionary<string, int> groupAllowances,
        Dictionary<string, int> candidateAllowances)
    {
        var selectable = new List<(DrumOnsetCandidate, DrumCandidateGroup)>();

        foreach (var (candidate, group) in remaining)
        {
            if (groupAllowances.TryGetValue(group.GroupId, out int groupRemaining) && groupRemaining <= 0)
                continue;

            string candidateKey = $"{group.GroupId}:{candidate.OnsetBeat:F4}";
            if (candidateAllowances.TryGetValue(candidateKey, out int candidateRemaining) && candidateRemaining <= 0)
                continue;

            selectable.Add((candidate, group));
        }

        return selectable;
    }

    // AI: buildGroups=Aggregate selectable tuples into DrumCandidateGroup instances for weighted selection
    private static List<DrumCandidateGroup> BuildGroupsForSelection(
        List<(DrumOnsetCandidate Candidate, DrumCandidateGroup Group)> selectable)
    {
        var groupedByGroupId = selectable
            .GroupBy(s => s.Group.GroupId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groups = new List<DrumCandidateGroup>();

        foreach (var kvp in groupedByGroupId)
        {
            var firstEntry = kvp.Value[0];
            var group = new DrumCandidateGroup
            {
                GroupId = firstEntry.Group.GroupId,
                GroupTags = firstEntry.Group.GroupTags,
                BaseProbabilityBias = firstEntry.Group.BaseProbabilityBias,
                MaxAddsPerBar = firstEntry.Group.MaxAddsPerBar,
                Candidates = kvp.Value.Select(v => v.Candidate).ToList()
            };
            groups.Add(group);
        }

        return groups;
    }

    // AI: update=Decrement group and candidate allowances unless they are int.MaxValue (representing unlimited)
    private static void UpdateAllowances(
        WeightedCandidate picked,
        Dictionary<string, int> groupAllowances,
        Dictionary<string, int> candidateAllowances)
    {
        if (groupAllowances.ContainsKey(picked.Group.GroupId))
        {
            if (groupAllowances[picked.Group.GroupId] != int.MaxValue)
            {
                groupAllowances[picked.Group.GroupId]--;
            }
        }

        string candidateKey = $"{picked.Group.GroupId}:{picked.Candidate.OnsetBeat:F4}";
        if (candidateAllowances.ContainsKey(candidateKey))
        {
            if (candidateAllowances[candidateKey] != int.MaxValue)
            {
                candidateAllowances[candidateKey]--;
            }
        }
    }
}
