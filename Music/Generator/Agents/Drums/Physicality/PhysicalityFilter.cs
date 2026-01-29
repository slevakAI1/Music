// AI: purpose=Physicality filter for Story 4.3/4.4; validates drum candidates for limb conflicts, sticking rules, and overcrowding.
// AI: invariants=Protected candidates never removed; deterministic pruning (score desc, operatorId asc, candidateId asc).
// AI: deps=DrumOnsetCandidate, DrumCandidateMapper, LimbConflictDetector, StickingRules, GrooveDiagnosticsCollector.
// AI: change=Story 4.4 adds full overcrowding prevention: MaxHitsPerBeat, MaxHitsPerBar, MaxHitsPerRolePerBar.

using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Physicality
{
    /// <summary>
    /// Physicality filter for drum candidate validation.
    /// Story 4.3: Limb/sticking validation. Story 4.4: Overcrowding prevention.
    /// </summary>
    /// <remarks>
    /// <para>Overcrowding prevention order: Role caps → Beat caps → Bar caps.</para>
    /// <para>Behavior by StrictnessLevel (for limb/sticking only; overcrowding always enforced):</para>
    /// <list type="bullet">
    ///   <item>Strict: remove all non-protected violating candidates</item>
    ///   <item>Normal: minimal pruning (keep highest scored, remove others)</item>
    ///   <item>Loose: log violations but keep all candidates</item>
    /// </list>
    /// <para>Protected candidates (DrumCandidateMapper.ProtectedTag) are never removed.</para>
    /// <para>Deterministic tie-break: Score desc → OperatorId asc → CandidateId asc.</para>
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
        public IReadOnlyList<DrumOnsetCandidate> Filter(
            IReadOnlyList<DrumOnsetCandidate> candidates,
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
            IReadOnlyList<DrumOnsetCandidate> candidates,
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

        private static Dictionary<string, DrumOnsetCandidate> BuildCandidateLookup(
            IReadOnlyList<DrumOnsetCandidate> candidates)
        {
            var lookup = new Dictionary<string, DrumOnsetCandidate>(StringComparer.Ordinal);
            foreach (var c in candidates)
            {
                string cid = DrumCandidateMapper.ExtractCandidateId(c) ?? $"gen_{Guid.NewGuid():N}";
                lookup.TryAdd(cid, c);
            }
            return lookup;
        }

        private static FillRole ExtractFillRole(DrumOnsetCandidate c)
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
            Dictionary<string, DrumOnsetCandidate> candidateById,
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
            Dictionary<string, DrumOnsetCandidate> candidateById,
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
            Dictionary<string, DrumOnsetCandidate> candidateById,
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

        private IReadOnlyList<DrumOnsetCandidate> BuildFilteredResult(
            IReadOnlyList<DrumOnsetCandidate> candidates,
            HashSet<string> toRemoveIds)
        {
            var result = new List<DrumOnsetCandidate>();
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
        /// Order: per-role caps → per-beat caps → per-bar caps.
        /// </summary>
        private IReadOnlyList<DrumOnsetCandidate> ApplyOvercrowdingPrevention(
            IReadOnlyList<DrumOnsetCandidate> candidates,
            int barNumber)
        {
            if (candidates.Count == 0)
                return candidates;

            // Apply caps in order: role → beat → bar (most specific to least specific)
            var result = ApplyRoleCaps(candidates);
            result = ApplyBeatCaps(result);
            result = ApplyBarCap(result);

            return result;
        }

        /// <summary>
        /// Applies per-role-per-bar caps.
        /// </summary>
        private IReadOnlyList<DrumOnsetCandidate> ApplyRoleCaps(
            IReadOnlyList<DrumOnsetCandidate> candidates)
        {
            if (_rules.MaxHitsPerRolePerBar == null || _rules.MaxHitsPerRolePerBar.Count == 0)
                return candidates;

            var result = new List<DrumOnsetCandidate>();
            var protectedByRole = new Dictionary<string, List<DrumOnsetCandidate>>(StringComparer.Ordinal);
            var unprotectedByRole = new Dictionary<string, List<DrumOnsetCandidate>>(StringComparer.Ordinal);

            // Group candidates by role
            foreach (var c in candidates)
            {
                string role = c.Role;
                if (DrumCandidateMapper.IsProtected(c))
                {
                    if (!protectedByRole.TryGetValue(role, out var protList))
                    {
                        protList = new List<DrumOnsetCandidate>();
                        protectedByRole[role] = protList;
                    }
                    protList.Add(c);
                }
                else
                {
                    if (!unprotectedByRole.TryGetValue(role, out var unprotList))
                    {
                        unprotList = new List<DrumOnsetCandidate>();
                        unprotectedByRole[role] = unprotList;
                    }
                    unprotList.Add(c);
                }
            }

            // Process each role
            var allRoles = protectedByRole.Keys.Union(unprotectedByRole.Keys).Distinct(StringComparer.Ordinal);
            foreach (var role in allRoles)
            {
                var cap = _rules.GetRoleCap(role);
                var protectedList = protectedByRole.GetValueOrDefault(role) ?? new List<DrumOnsetCandidate>();
                var unprotectedList = unprotectedByRole.GetValueOrDefault(role) ?? new List<DrumOnsetCandidate>();

                if (!cap.HasValue)
                {
                    // No cap for this role - keep all
                    result.AddRange(protectedList);
                    result.AddRange(unprotectedList);
                    continue;
                }

                int totalForRole = protectedList.Count + unprotectedList.Count;
                if (totalForRole <= cap.Value)
                {
                    // Under cap - keep all
                    result.AddRange(protectedList);
                    result.AddRange(unprotectedList);
                    continue;
                }

                // Over cap - prune unprotected
                result.AddRange(protectedList);

                if (protectedList.Count >= cap.Value)
                {
                    // Protected alone exceeds cap - prune all unprotected
                    foreach (var c in unprotectedList)
                    {
                        var id = DrumCandidateMapper.ExtractCandidateId(c) ?? "";
                        _diagnosticsCollector?.RecordPrune(id, $"Overcrowding:MaxHitsPerRole:{role}", false);
                    }
                    continue;
                }

                // Keep highest scored unprotected up to budget
                int budget = cap.Value - protectedList.Count;
                var sorted = SortByScoreDescending(unprotectedList);
                result.AddRange(sorted.Take(budget));

                foreach (var c in sorted.Skip(budget))
                {
                    var id = DrumCandidateMapper.ExtractCandidateId(c) ?? "";
                    _diagnosticsCollector?.RecordPrune(id, $"Overcrowding:MaxHitsPerRole:{role}", false);
                }
            }

            return result;
        }

        /// <summary>
        /// Applies per-beat caps.
        /// </summary>
        private IReadOnlyList<DrumOnsetCandidate> ApplyBeatCaps(
            IReadOnlyList<DrumOnsetCandidate> candidates)
        {
            if (!_rules.MaxHitsPerBeat.HasValue)
                return candidates;

            int maxPerBeat = _rules.MaxHitsPerBeat.Value;
            var result = new List<DrumOnsetCandidate>();
            var protectedByBeat = new Dictionary<decimal, List<DrumOnsetCandidate>>();
            var unprotectedByBeat = new Dictionary<decimal, List<DrumOnsetCandidate>>();

            // Group candidates by beat
            foreach (var c in candidates)
            {
                decimal beat = c.OnsetBeat;
                if (DrumCandidateMapper.IsProtected(c))
                {
                    if (!protectedByBeat.TryGetValue(beat, out var protList))
                    {
                        protList = new List<DrumOnsetCandidate>();
                        protectedByBeat[beat] = protList;
                    }
                    protList.Add(c);
                }
                else
                {
                    if (!unprotectedByBeat.TryGetValue(beat, out var unprotList))
                    {
                        unprotList = new List<DrumOnsetCandidate>();
                        unprotectedByBeat[beat] = unprotList;
                    }
                    unprotList.Add(c);
                }
            }

            // Process each beat
            var allBeats = protectedByBeat.Keys.Union(unprotectedByBeat.Keys).Distinct().OrderBy(b => b);
            foreach (var beat in allBeats)
            {
                var protectedList = protectedByBeat.GetValueOrDefault(beat) ?? new List<DrumOnsetCandidate>();
                var unprotectedList = unprotectedByBeat.GetValueOrDefault(beat) ?? new List<DrumOnsetCandidate>();

                int totalForBeat = protectedList.Count + unprotectedList.Count;
                if (totalForBeat <= maxPerBeat)
                {
                    // Under cap - keep all
                    result.AddRange(protectedList);
                    result.AddRange(unprotectedList);
                    continue;
                }

                // Over cap - prune unprotected
                result.AddRange(protectedList);

                if (protectedList.Count >= maxPerBeat)
                {
                    // Protected alone exceeds cap - prune all unprotected
                    foreach (var c in unprotectedList)
                    {
                        var id = DrumCandidateMapper.ExtractCandidateId(c) ?? "";
                        _diagnosticsCollector?.RecordPrune(id, "Overcrowding:MaxHitsPerBeat", false);
                    }
                    continue;
                }

                // Keep highest scored unprotected up to budget
                int budget = maxPerBeat - protectedList.Count;
                var sorted = SortByScoreDescending(unprotectedList);
                result.AddRange(sorted.Take(budget));

                foreach (var c in sorted.Skip(budget))
                {
                    var id = DrumCandidateMapper.ExtractCandidateId(c) ?? "";
                    _diagnosticsCollector?.RecordPrune(id, "Overcrowding:MaxHitsPerBeat", false);
                }
            }

            return result;
        }

        /// <summary>
        /// Applies per-bar cap.
        /// </summary>
        private IReadOnlyList<DrumOnsetCandidate> ApplyBarCap(
            IReadOnlyList<DrumOnsetCandidate> candidates)
        {
            int? maxHits = _rules.MaxHitsPerBar;
            if (!maxHits.HasValue || candidates.Count <= maxHits.Value)
                return candidates;

            var protectedCandidates = new List<DrumOnsetCandidate>();
            var unprotectedCandidates = new List<DrumOnsetCandidate>();

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
                    _diagnosticsCollector?.RecordPrune(id, "Overcrowding:MaxHitsPerBar", false);
                }
                return protectedCandidates;
            }

            var sorted = SortByScoreDescending(unprotectedCandidates);

            int budget = maxHits.Value - protectedCandidates.Count;
            var selected = sorted.Take(budget).ToList();

            foreach (var c in sorted.Skip(budget))
            {
                var id = DrumCandidateMapper.ExtractCandidateId(c) ?? "";
                _diagnosticsCollector?.RecordPrune(id, "Overcrowding:MaxHitsPerBar", false);
            }

            var result = new List<DrumOnsetCandidate>(protectedCandidates);
            result.AddRange(selected);
            return result;
        }

        /// <summary>
        /// Sorts candidates by score descending with deterministic tie-break.
        /// </summary>
        private static List<DrumOnsetCandidate> SortByScoreDescending(
            IEnumerable<DrumOnsetCandidate> candidates)
        {
            return candidates
                .OrderByDescending(c => c.ProbabilityBias)
                .ThenBy(c => DrumCandidateMapper.ExtractOperatorId(c) ?? "", StringComparer.Ordinal)
                .ThenBy(c => DrumCandidateMapper.ExtractCandidateId(c) ?? "", StringComparer.Ordinal)
                .ToList();
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

