// AI: purpose=Stub physicality filter for Story 2.4; validates drum candidates for playability constraints.
// AI: invariants=Protected candidates never removed; deterministic filtering order; null-safe.
// AI: deps=GrooveOnsetCandidate, DrumCandidateMapper.IsProtected; full implementation in Story 4.3.
// AI: change=Story 2.4 stub; Story 4.3 adds LimbModel, StickingRules, overcrowding prevention.

using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Physicality
{
    /// <summary>
    /// Stub physicality filter for drum candidate validation.
    /// Story 2.4: Placeholder implementation; Story 4.3 provides full limb/sticking validation.
    /// </summary>
    /// <remarks>
    /// Current stub behavior:
    /// - Passes all candidates through (no validation)
    /// - Records filter diagnostics when collector is provided
    /// 
    /// Story 4.3 will add:
    /// - LimbConflictDetector integration
    /// - StickingRules validation
    /// - Overcrowding prevention with deterministic pruning
    /// </remarks>
    public sealed class PhysicalityFilter
    {
        private readonly PhysicalityRules _rules;
        private readonly GrooveDiagnosticsCollector? _diagnosticsCollector;

        /// <summary>
        /// Creates a physicality filter with the specified rules.
        /// </summary>
        /// <param name="rules">Physicality rules configuration.</param>
        /// <param name="diagnosticsCollector">Optional diagnostics collector.</param>
        public PhysicalityFilter(
            PhysicalityRules rules,
            GrooveDiagnosticsCollector? diagnosticsCollector = null)
        {
            ArgumentNullException.ThrowIfNull(rules);
            _rules = rules;
            _diagnosticsCollector = diagnosticsCollector;
        }

        /// <summary>
        /// Filters candidates by physicality constraints.
        /// Stub implementation: passes all candidates through.
        /// </summary>
        /// <param name="candidates">Candidates to filter.</param>
        /// <param name="barNumber">Bar number for diagnostics.</param>
        /// <returns>Filtered candidates (currently same as input).</returns>
        public IReadOnlyList<GrooveOnsetCandidate> Filter(
            IReadOnlyList<GrooveOnsetCandidate> candidates,
            int barNumber)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            if (candidates.Count == 0)
                return candidates;

            // Stub: Apply only basic overcrowding prevention if caps are set
            if (_rules.MaxHitsPerBar.HasValue && candidates.Count > _rules.MaxHitsPerBar.Value)
            {
                return ApplyOvercrowdingPrevention(candidates, barNumber);
            }

            // TODO: Story 4.3 - Add limb conflict detection
            // TODO: Story 4.3 - Add sticking rules validation

            return candidates;
        }

        /// <summary>
        /// Applies overcrowding prevention by pruning lowest-scored candidates.
        /// Protected candidates are never pruned.
        /// </summary>
        private IReadOnlyList<GrooveOnsetCandidate> ApplyOvercrowdingPrevention(
            IReadOnlyList<GrooveOnsetCandidate> candidates,
            int barNumber)
        {
            int maxHits = _rules.MaxHitsPerBar ?? int.MaxValue;

            // Separate protected and unprotected candidates
            var protectedCandidates = new List<GrooveOnsetCandidate>();
            var unprotectedCandidates = new List<GrooveOnsetCandidate>();

            foreach (var candidate in candidates)
            {
                if (DrumCandidateMapper.IsProtected(candidate))
                {
                    protectedCandidates.Add(candidate);
                }
                else
                {
                    unprotectedCandidates.Add(candidate);
                }
            }

            // If protected alone exceed cap, return all protected
            if (protectedCandidates.Count >= maxHits)
            {
                return protectedCandidates;
            }

            // Sort unprotected by score descending, then by tags for determinism
            var sorted = unprotectedCandidates
                .OrderByDescending(c => c.ProbabilityBias)
                .ThenBy(c => GetCandidateId(c) ?? "")
                .ToList();

            // Take top candidates up to remaining budget
            int budget = maxHits - protectedCandidates.Count;
            var selected = sorted.Take(budget).ToList();

            // Combine protected + selected unprotected
            var result = new List<GrooveOnsetCandidate>(protectedCandidates);
            result.AddRange(selected);

            return result;
        }

        /// <summary>
        /// Extracts candidate ID for deterministic ordering.
        /// </summary>
        private static string? GetCandidateId(GrooveOnsetCandidate candidate)
        {
            return DrumCandidateMapper.ExtractCandidateId(candidate);
        }

        /// <summary>
        /// Creates a default filter with default rules.
        /// </summary>
        public static PhysicalityFilter CreateDefault()
        {
            return new PhysicalityFilter(PhysicalityRules.Default);
        }
    }
}
