// AI: purpose=Physicality filter for Story 4.3; validates drum candidates for limb conflicts and sticking rules.
// AI: invariants=Protected candidates never removed; deterministic pruning (score desc, operatorId asc, candidateId asc).
// AI: deps=GrooveOnsetCandidate, DrumCandidateMapper, LimbConflictDetector, StickingRules, GrooveDiagnosticsCollector.
// AI: change=Story 4.3 full implementation with limb conflicts, sticking validation, strictness modes.

using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Physicality
{
    /// <summary>
    /// Physicality filter for drum candidate validation.
    /// Story 4.3: Full implementation with limb/sticking validation.
    /// </summary>
    /// <remarks>
    /// Behavior by StrictnessLevel:
    /// - Strict: remove all non-protected violating candidates
    /// - Normal: minimal pruning (keep highest scored, remove others)
    /// - Loose: log violations but keep all candidates
    /// 
    /// Protected candidates (DrumCandidateMapper.ProtectedTag) are never removed.
    /// Deterministic tie-break: Score desc → OperatorId asc → CandidateId asc.
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
        /// </summary>
        /// <param name="candidates">Candidates to filter.</param>
        /// <param name="barNumber">Bar number for diagnostics and context.</param>
        /// <returns>Filtered candidates (playable subset).</returns>
        public IReadOnlyList<GrooveOnsetCandidate> Filter(
            IReadOnlyList<GrooveOnsetCandidate> candidates,
            int barNumber)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            if (candidates.Count == 0)
                return candidates;

            // Build DrumCandidate representations for detectors
            var drumCandidates = BuildDrumCandidates(candidates, barNumber);
            var candidateById = BuildCandidateLookup(candidates);

            // Track IDs to remove
            var toRemoveIds = new HashSet<string>(StringComparer.Ordinal);

            // 1) Apply overcrowding prevention first
            var afterOvercrowding = ApplyOvercrowdingPrevention(candidates, barNumber);
            if (afterOvercrowding.Count != candidates.Count)
            {
                // Rebuild for remaining
                drumCandidates = BuildDrumCandidates(afterOvercrowding, barNumber);
                candidateById = BuildCandidateLookup(afterOvercrowding);
                candidates = afterOvercrowding;
            }

            // 2) Detect and resolve limb conflicts
            ResolveConflicts(drumCandidates, candidateById, toRemoveIds);

            // 3) Validate sticking rules
            ValidateSticking(drumCandidates, candidateById, toRemoveIds);

            // Build final result
            return BuildFilteredResult(candidates, toRemoveIds);
        }

        private List<DrumCandidate> BuildDrumCandidates(
            IReadOnlyList<GrooveOnsetCandidate> candidates,
            int barNumber)
        {
            var result = new List<DrumCandidate>();
            foreach (var c in candidates)
            {
                string cid = DrumCandidateMapper.ExtractCandidateId(c) ?? $"gen_{Guid.NewGuid():N}";
                string opId = DrumCandidateMapper.ExtractOperatorId(c) ?? "UnknownOp";

                var dc = new DrumCandidate
                {
                    CandidateId = cid,
                    OperatorId = opId,
                    Role = c.Role,
                    BarNumber = barNumber,
                    Beat = c.OnsetBeat,
                    Strength = c.Strength,
                    VelocityHint = null,
                    TimingHint = null,
                    ArticulationHint = null,
                    FillRole = ExtractFillRole(c),
                    Score = c.ProbabilityBias
                };
                result.Add(dc);
            }
            return result;
        }

        private static Dictionary<string, GrooveOnsetCandidate> BuildCandidateLookup(
            IReadOnlyList<GrooveOnsetCandidate> candidates)
        {
            var lookup = new Dictionary<string, GrooveOnsetCandidate>(StringComparer.Ordinal);
            foreach (var c in candidates)
            {
                string cid = DrumCandidateMapper.ExtractCandidateId(c) ?? $"gen_{Guid.NewGuid():N}";
                lookup.TryAdd(cid, c);
            }
            return lookup;
        }

        private static FillRole ExtractFillRole(GrooveOnsetCandidate c)
        {
            if (c.Tags == null) return FillRole.None;
            if (c.Tags.Contains(nameof(FillRole.FillStart))) return FillRole.FillStart;
            if (c.Tags.Contains(nameof(FillRole.FillBody))) return FillRole.FillBody;
            if (c.Tags.Contains(nameof(FillRole.FillEnd))) return FillRole.FillEnd;
            if (c.Tags.Contains(nameof(FillRole.Setup))) return FillRole.Setup;
            return FillRole.None;
        }

        private void ResolveConflicts(
            List<DrumCandidate> drumCandidates,
            Dictionary<string, GrooveOnsetCandidate> candidateById,
            HashSet<string> toRemoveIds)
        {
            var conflicts = LimbConflictDetector.Default.DetectConflicts(drumCandidates, _rules.LimbModel);

            foreach (var conflict in conflicts)
            {
                var ids = conflict.ConflictingAssignments.Select(a => a.CandidateId).ToList();
                ResolveViolation(ids, $"LimbConflict:{conflict.Limb}", drumCandidates, candidateById, toRemoveIds);
            }
        }

        private void ValidateSticking(
            List<DrumCandidate> drumCandidates,
            Dictionary<string, GrooveOnsetCandidate> candidateById,
            HashSet<string> toRemoveIds)
        {
            var validation = _rules.StickingRules?.ValidatePattern(drumCandidates);
            if (validation == null || !validation.Violations.Any())
                return;

            foreach (var violation in validation.Violations)
            {
                var ids = violation.CandidateIds?.ToList() ?? new List<string>();
                ResolveViolation(ids, $"Sticking:{violation.RuleId}", drumCandidates, candidateById, toRemoveIds);
            }
        }

        private void ResolveViolation(
            List<string> involvedIds,
            string reason,
            List<DrumCandidate> drumCandidates,
            Dictionary<string, GrooveOnsetCandidate> candidateById,
            HashSet<string> toRemoveIds)
        {
            if (involvedIds.Count == 0) return;

            var protectedIds = involvedIds
                .Where(id => candidateById.TryGetValue(id, out var gc) && DrumCandidateMapper.IsProtected(gc))
                .ToHashSet(StringComparer.Ordinal);
            var unprotectedIds = involvedIds.Except(protectedIds).ToList();

            if (_rules.StrictnessLevel == PhysicalityStrictness.Loose)
            {
                // Log only
                foreach (var id in involvedIds)
                    _diagnosticsCollector?.RecordFilter(id, reason);
                return;
            }

            if (_rules.StrictnessLevel == PhysicalityStrictness.Strict)
            {
                // Remove all unprotected
                foreach (var id in unprotectedIds)
                {
                    toRemoveIds.Add(id);
                    bool wasProtected = protectedIds.Contains(id);
                    _diagnosticsCollector?.RecordPrune(id, reason, wasProtected);
                }
                return;
            }

            // Normal: minimal pruning - keep best, remove rest
            if (protectedIds.Any())
            {
                // Protected exists, remove all unprotected
                foreach (var id in unprotectedIds)
                {
                    toRemoveIds.Add(id);
                    _diagnosticsCollector?.RecordPrune(id, reason, false);
                }
            }
            else
            {
                // Keep highest scored, remove others
                var ordered = OrderByScoreForPruning(unprotectedIds, drumCandidates);
                foreach (var id in ordered.Skip(1))
                {
                    toRemoveIds.Add(id);
                    _diagnosticsCollector?.RecordPrune(id, reason, false);
                }
            }
        }

        private static List<string> OrderByScoreForPruning(
            IEnumerable<string> ids,
            List<DrumCandidate> drumCandidates)
        {
            return ids
                .Select(id =>
                {
                    var dc = drumCandidates.FirstOrDefault(c => c.CandidateId == id);
                    return new { Id = id, Score = dc?.Score ?? 0.0, Op = dc?.OperatorId ?? "" };
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Op, StringComparer.Ordinal)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => x.Id)
                .ToList();
        }

        private IReadOnlyList<GrooveOnsetCandidate> BuildFilteredResult(
            IReadOnlyList<GrooveOnsetCandidate> candidates,
            HashSet<string> toRemoveIds)
        {
            var result = new List<GrooveOnsetCandidate>();
            foreach (var c in candidates)
            {
                string id = DrumCandidateMapper.ExtractCandidateId(c) ?? "";
                bool isProtected = DrumCandidateMapper.IsProtected(c);

                if (toRemoveIds.Contains(id) && !isProtected)
                    continue;

                result.Add(c);
            }
            return result;
        }

        /// <summary>
        /// Applies overcrowding prevention by pruning lowest-scored candidates.
        /// Protected candidates are never pruned.
        /// </summary>
        private IReadOnlyList<GrooveOnsetCandidate> ApplyOvercrowdingPrevention(
            IReadOnlyList<GrooveOnsetCandidate> candidates,
            int barNumber)
        {
            int? maxHits = _rules.MaxHitsPerBar;
            if (!maxHits.HasValue || candidates.Count <= maxHits.Value)
                return candidates;

            var protectedCandidates = new List<GrooveOnsetCandidate>();
            var unprotectedCandidates = new List<GrooveOnsetCandidate>();

            foreach (var candidate in candidates)
            {
                if (DrumCandidateMapper.IsProtected(candidate))
                    protectedCandidates.Add(candidate);
                else
                    unprotectedCandidates.Add(candidate);
            }

            if (protectedCandidates.Count >= maxHits.Value)
            {
                foreach (var c in unprotectedCandidates)
                {
                    var id = DrumCandidateMapper.ExtractCandidateId(c) ?? "";
                    _diagnosticsCollector?.RecordPrune(id, "Overcrowding:protectedExceeded", false);
                }
                return protectedCandidates;
            }

            var sorted = unprotectedCandidates
                .OrderByDescending(c => c.ProbabilityBias)
                .ThenBy(c => DrumCandidateMapper.ExtractOperatorId(c) ?? "")
                .ThenBy(c => DrumCandidateMapper.ExtractCandidateId(c) ?? "")
                .ToList();

            int budget = maxHits.Value - protectedCandidates.Count;
            var selected = sorted.Take(budget).ToList();

            foreach (var c in sorted.Skip(budget))
            {
                var id = DrumCandidateMapper.ExtractCandidateId(c) ?? "";
                _diagnosticsCollector?.RecordPrune(id, "Overcrowding:prunedLowestScored", false);
            }

            var result = new List<GrooveOnsetCandidate>(protectedCandidates);
            result.AddRange(selected);
            return result;
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
