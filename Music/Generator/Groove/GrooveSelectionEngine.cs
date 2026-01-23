// AI: purpose=Select groove candidates until target count reached with pool exhaustion safety (Story C2).
// AI: invariants=Deterministic; same seed => same selections; never exceeds target; respects anchors and caps.
// AI: deps=GrooveWeightedCandidateSelector, GrooveRngHelper for RNG; GrooveOnset for anchors; GrooveDiagnosticsCollector for G1.
// AI: change=Story G1: Added optional diagnostics collection via GrooveDiagnosticsCollector.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Selection engine for groove candidates with target count and pool exhaustion safety.
    /// Story C2: Implements safe candidate selection with anchor preservation and cap enforcement.
    /// Story G1: Supports optional diagnostics collection.
    /// </summary>
    public static class GrooveSelectionEngine
    {
        /// <summary>
        /// Selects candidates until target count is reached or pool is exhausted.
        /// Story C2: Deterministic selection with RNG, respects anchors and per-group/per-candidate caps.
        /// Story G1: Optional diagnostics collection for decision tracing.
        /// </summary>
        /// <param name="barContext">Bar context for RNG seed derivation.</param>
        /// <param name="role">Role name for selection.</param>
        /// <param name="groups">Candidate groups to select from (should be merged and filtered by caller).</param>
        /// <param name="targetCount">Target number of candidates to select.</param>
        /// <param name="existingAnchors">Existing anchors for this role (to avoid duplicates).</param>
        /// <param name="diagnostics">Optional diagnostics collector for decision tracing (Story G1).</param>
        /// <returns>List of selected candidates in selection order.</returns>
        public static IReadOnlyList<GrooveOnsetCandidate> SelectUntilTargetReached(
            GrooveBarContext barContext,
            string role,
            IReadOnlyList<GrooveCandidateGroup> groups,
            int targetCount,
            IReadOnlyList<GrooveOnset> existingAnchors,
            GrooveDiagnosticsCollector? diagnostics = null)
        {
            ArgumentNullException.ThrowIfNull(barContext);
            ArgumentException.ThrowIfNullOrWhiteSpace(role);
            ArgumentNullException.ThrowIfNull(groups);
            ArgumentNullException.ThrowIfNull(existingAnchors);

            // Story C2: Return empty if target <= 0
            if (targetCount <= 0)
            {
                return Array.Empty<GrooveOnsetCandidate>();
            }

            // Build set of anchor beats to detect conflicts
            var anchorBeats = new HashSet<decimal>(
                existingAnchors
                    .Where(a => string.Equals(a.Role, role, StringComparison.Ordinal))
                    .Select(a => a.Beat));

            // Build working pool: candidates that don't conflict with anchors
            var workingPool = BuildWorkingPool(groups, anchorBeats, diagnostics);

            // Story G1: Record candidate pool statistics
            diagnostics?.RecordCandidatePool(groups.Count, workingPool.Count);

            if (workingPool.Count == 0)
            {
                return Array.Empty<GrooveOnsetCandidate>();
            }

            // Track remaining allowances for groups and candidates
            var groupAllowances = new Dictionary<string, int>(StringComparer.Ordinal);
            var candidateAllowances = new Dictionary<string, int>(StringComparer.Ordinal);

            InitializeAllowances(workingPool, groupAllowances, candidateAllowances);

            // Selection loop
            var selected = new List<GrooveOnsetCandidate>();
            var remaining = new List<(GrooveOnsetCandidate Candidate, GrooveCandidateGroup Group)>(workingPool);

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

                // Use weighted selector to pick one candidate
                var selectedCandidates = GrooveWeightedCandidateSelector.SelectCandidates(
                    selectableGroups,
                    targetCount: 1,
                    barNumber: barContext.BarNumber,
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
                    string candidateId = GrooveDiagnosticsCollector.MakeCandidateId(picked.Group.GroupId, picked.Candidate.OnsetBeat);
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

        /// <summary>
        /// Builds the working pool of candidates excluding those that conflict with anchors.
        /// Story G1: Records filter decisions for excluded candidates.
        /// </summary>
        private static List<(GrooveOnsetCandidate Candidate, GrooveCandidateGroup Group)> BuildWorkingPool(
            IReadOnlyList<GrooveCandidateGroup> groups,
            HashSet<decimal> anchorBeats,
            GrooveDiagnosticsCollector? diagnostics = null)
        {
            var pool = new List<(GrooveOnsetCandidate, GrooveCandidateGroup)>();

            foreach (var group in groups)
            {
                foreach (var candidate in group.Candidates)
                {
                    // Story C2: Exclude candidates that conflict with anchors (same beat)
                    if (anchorBeats.Contains(candidate.OnsetBeat))
                    {
                        // Story G1: Record filter decision
                        if (diagnostics != null)
                        {
                            string candidateId = GrooveDiagnosticsCollector.MakeCandidateId(group.GroupId, candidate.OnsetBeat);
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

        /// <summary>
        /// Initializes allowance tracking for groups and candidates.
        /// </summary>
        private static void InitializeAllowances(
            List<(GrooveOnsetCandidate Candidate, GrooveCandidateGroup Group)> pool,
            Dictionary<string, int> groupAllowances,
            Dictionary<string, int> candidateAllowances)
        {
            foreach (var (candidate, group) in pool)
            {
                // Initialize group allowance (use int.MaxValue for unlimited)
                if (!groupAllowances.ContainsKey(group.GroupId))
                {
                    groupAllowances[group.GroupId] = group.MaxAddsPerBar > 0
                        ? group.MaxAddsPerBar
                        : int.MaxValue;
                }

                // Initialize candidate allowance (use unique key: groupId:beat)
                string candidateKey = $"{group.GroupId}:{candidate.OnsetBeat:F4}";
                if (!candidateAllowances.ContainsKey(candidateKey))
                {
                    candidateAllowances[candidateKey] = candidate.MaxAddsPerBar > 0
                        ? candidate.MaxAddsPerBar
                        : int.MaxValue;
                }
            }
        }

        /// <summary>
        /// Filters remaining candidates by current allowances.
        /// </summary>
        private static List<(GrooveOnsetCandidate Candidate, GrooveCandidateGroup Group)> FilterByAllowances(
            List<(GrooveOnsetCandidate Candidate, GrooveCandidateGroup Group)> remaining,
            Dictionary<string, int> groupAllowances,
            Dictionary<string, int> candidateAllowances)
        {
            var selectable = new List<(GrooveOnsetCandidate, GrooveCandidateGroup)>();

            foreach (var (candidate, group) in remaining)
            {
                // Check group allowance
                if (groupAllowances.TryGetValue(group.GroupId, out int groupRemaining) && groupRemaining <= 0)
                {
                    continue;
                }

                // Check candidate allowance
                string candidateKey = $"{group.GroupId}:{candidate.OnsetBeat:F4}";
                if (candidateAllowances.TryGetValue(candidateKey, out int candidateRemaining) && candidateRemaining <= 0)
                {
                    continue;
                }

                selectable.Add((candidate, group));
            }

            return selectable;
        }

        /// <summary>
        /// Builds groups for weighted selection from selectable candidates.
        /// </summary>
        private static List<GrooveCandidateGroup> BuildGroupsForSelection(
            List<(GrooveOnsetCandidate Candidate, GrooveCandidateGroup Group)> selectable)
        {
            // Group candidates by group ID
            var groupedByGroupId = selectable
                .GroupBy(s => s.Group.GroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var groups = new List<GrooveCandidateGroup>();

            foreach (var kvp in groupedByGroupId)
            {
                var firstEntry = kvp.Value[0];
                var group = new GrooveCandidateGroup
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

        /// <summary>
        /// Updates allowances after a candidate is selected.
        /// </summary>
        private static void UpdateAllowances(
            WeightedCandidate picked,
            Dictionary<string, int> groupAllowances,
            Dictionary<string, int> candidateAllowances)
        {
            // Decrement group allowance
            if (groupAllowances.ContainsKey(picked.Group.GroupId))
            {
                if (groupAllowances[picked.Group.GroupId] != int.MaxValue)
                {
                    groupAllowances[picked.Group.GroupId]--;
                }
            }

            // Decrement candidate allowance
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
}
