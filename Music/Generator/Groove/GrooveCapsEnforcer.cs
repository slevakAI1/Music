// AI: purpose=Enforce hard caps on groove onsets with policy-aware protection (Story C3, F1, G1).
// AI: invariants=IsMustHit/IsNeverRemove never pruned; IsProtected respects OverrideCanRemoveProtectedOnsets policy.
// AI: deps=GrooveOverrideMergePolicy, OverrideMergePolicyEnforcer, GrooveDiagnosticsCollector for decision tracing.
// AI: change=Story G1: Uses structured GrooveDiagnosticsCollector instead of string diagnostics.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Music.Generator
{
    /// <summary>
    /// Enforces hard caps on groove onsets per bar, per beat, per role, per group, and per candidate.
    /// Uses deterministic scoring and pruning to ensure constraints are satisfied while preserving protected onsets.
    /// </summary>
    /// <remarks>
    /// Story C3: Enforce Hard Caps (Per Bar / Per Beat / Per Role).
    /// Story F1: OverrideCanRemoveProtectedOnsets policy controls IsProtected onset pruning.
    /// Story G1: Supports structured diagnostics via GrooveDiagnosticsCollector.
    /// 
    /// Enforcement order: Candidate caps -> Group caps -> Role density -> MaxHitsPerBar -> MaxHitsPerBeat
    /// 
    /// Protection hierarchy (Story F1):
    /// - IsMustHit: NEVER pruned regardless of policy
    /// - IsNeverRemove: NEVER pruned regardless of policy
    /// - IsProtected: Prunable ONLY when OverrideCanRemoveProtectedOnsets=true
    /// - Unprotected: Pruned first based on score (strength, probability bias, provenance)
    /// 
    /// RNG Usage: Currently uses Random(rngSeed) for deterministic tie-breaking.
    /// TODO: When Story A2 RngFor helper is implemented, replace with RngFor(bar, role, "PrunePick").
    /// </remarks>
    public sealed class GrooveCapsEnforcer
    {
        /// <summary>
        /// Enforces all hard caps on a groove bar plan, pruning excess onsets deterministically.
        /// Story G1: Supports optional structured diagnostics collection.
        /// </summary>
        /// <param name="barPlan">The bar plan with selected onsets to enforce caps on.</param>
        /// <param name="preset">The groove preset definition containing constraint policies.</param>
        /// <param name="segmentProfile">The segment profile for this bar (optional).</param>
        /// <param name="variationCatalog">The variation catalog for provenance lookup (optional).</param>
        /// <param name="rngSeed">Deterministic RNG seed for tie-breaking (bar number recommended).</param>
        /// <param name="mergePolicy">Optional merge policy controlling protected onset removal.</param>
        /// <param name="diagnosticsEnabled">Whether to collect diagnostics (optional, no behavior change).</param>
        /// <returns>A new GrooveBarPlan with FinalOnsets after cap enforcement.</returns>
        public GrooveBarPlan EnforceHardCaps(
            GrooveBarPlan barPlan,
            GroovePresetDefinition preset,
            SegmentGrooveProfile? segmentProfile,
            GrooveVariationCatalog? variationCatalog,
            int rngSeed,
            GrooveOverrideMergePolicy? mergePolicy = null,
            bool diagnosticsEnabled = false)
        {
            ArgumentNullException.ThrowIfNull(barPlan);
            ArgumentNullException.ThrowIfNull(preset);

            // Use default policy if not provided (safest: no protected removal)
            var effectivePolicy = mergePolicy ?? new GrooveOverrideMergePolicy();

            // Start with all onsets: BaseOnsets + SelectedVariationOnsets, or FinalOnsets if already set
            var workingOnsets = barPlan.FinalOnsets != null && barPlan.FinalOnsets.Count > 0
                ? barPlan.FinalOnsets.ToList()
                : barPlan.BaseOnsets.Concat(barPlan.SelectedVariationOnsets).ToList();

            // Story G1: Create collector for each role when diagnostics enabled
            var roleCollectors = new Dictionary<string, GrooveDiagnosticsCollector>();

            // Group onsets by role for per-role enforcement
            var onsetsByRole = workingOnsets.GroupBy(o => o.Role).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (role, roleOnsets) in onsetsByRole)
            {
                // Story G1: Create collector for this role
                GrooveDiagnosticsCollector? collector = null;
                if (diagnosticsEnabled)
                {
                    collector = new GrooveDiagnosticsCollector(barPlan.BarNumber, role);
                    roleCollectors[role] = collector;
                }

                // Enforce caps in order: candidate -> group -> role -> per-beat
                var prunedOnsets = EnforceRoleCaps(
                    roleOnsets,
                    role,
                    preset,
                    segmentProfile,
                    variationCatalog,
                    barPlan.BarNumber,
                    rngSeed,
                    effectivePolicy,
                    collector);

                onsetsByRole[role] = prunedOnsets;

                // Story G1: Record final counts
                collector?.RecordOnsetCounts(
                    baseCount: barPlan.BaseOnsets.Count(o => o.Role == role),
                    variationCount: barPlan.SelectedVariationOnsets.Count(o => o.Role == role),
                    finalCount: prunedOnsets.Count);
            }

            // Flatten back to single list
            var finalOnsets = onsetsByRole.Values.SelectMany(o => o).OrderBy(o => o.Beat).ToList();

            // Story G1: Build diagnostics from first role collector (or null if disabled)
            // Note: Full multi-role diagnostics would require a different GrooveBarPlan structure
            GrooveBarDiagnostics? diagnostics = null;
            if (diagnosticsEnabled && roleCollectors.Count > 0)
            {
                var firstCollector = roleCollectors.Values.First();
                diagnostics = firstCollector.Build();
            }

            return barPlan with
            {
                FinalOnsets = finalOnsets,
                Diagnostics = diagnostics
            };
        }

        private List<GrooveOnset> EnforceRoleCaps(
            List<GrooveOnset> roleOnsets,
            string role,
            GroovePresetDefinition preset,
            SegmentGrooveProfile? segmentProfile,
            GrooveVariationCatalog? variationCatalog,
            int barNumber,
            int rngSeed,
            GrooveOverrideMergePolicy mergePolicy,
            GrooveDiagnosticsCollector? collector)
        {
            var workingOnsets = roleOnsets.ToList();


            // 1. Enforce per-candidate MaxAddsPerBar
            workingOnsets = EnforceCandidateCaps(workingOnsets, variationCatalog, mergePolicy, collector);

            // 2. Enforce per-group MaxAddsPerBar
            workingOnsets = EnforceGroupCaps(workingOnsets, variationCatalog, mergePolicy, collector);

            // 3. Enforce RoleMaxDensityPerBar (global role cap)
            workingOnsets = EnforceRoleMaxDensityPerBar(workingOnsets, role, preset, mergePolicy, collector);

            // 4. Enforce RoleRhythmVocabulary.MaxHitsPerBar
            workingOnsets = EnforceMaxHitsPerBar(workingOnsets, role, preset, mergePolicy, collector);

            // 5. Enforce RoleRhythmVocabulary.MaxHitsPerBeat
            workingOnsets = EnforceMaxHitsPerBeat(workingOnsets, role, preset, barNumber, rngSeed, mergePolicy, collector);

            return workingOnsets;
        }

        private List<GrooveOnset> EnforceCandidateCaps(
            List<GrooveOnset> onsets,
            GrooveVariationCatalog? catalog,
            GrooveOverrideMergePolicy mergePolicy,
            GrooveDiagnosticsCollector? collector)
        {
            if (catalog == null)
                return onsets;

            // Build candidate cap lookup
            var candidateCaps = new Dictionary<string, int>();
            foreach (var layer in catalog.HierarchyLayers)
            {
                foreach (var group in layer.CandidateGroups)
                {
                    foreach (var candidate in group.Candidates)
                    {
                        var key = $"{group.GroupId}_{candidate.Role}_{candidate.OnsetBeat}";
                        if (candidate.MaxAddsPerBar > 0)
                            candidateCaps[key] = candidate.MaxAddsPerBar;
                    }
                }
            }

            // Count occurrences per candidate
            var candidateCounts = new Dictionary<string, int>();
            foreach (var onset in onsets)
            {
                if (onset.Provenance?.GroupId != null && onset.Provenance?.CandidateId != null)
                {
                    var key = $"{onset.Provenance.GroupId}_{onset.Role}_{onset.Beat}";
                    candidateCounts[key] = candidateCounts.GetValueOrDefault(key) + 1;
                }
            }

            // Prune excess per candidate
            var result = new List<GrooveOnset>();
            var currentCounts = new Dictionary<string, int>();

            foreach (var onset in onsets)
            {
                if (onset.Provenance?.GroupId != null && onset.Provenance?.CandidateId != null)
                {
                    var key = $"{onset.Provenance.GroupId}_{onset.Role}_{onset.Beat}";
                    int currentCount = currentCounts.GetValueOrDefault(key);
                    int maxAllowed = candidateCaps.GetValueOrDefault(key, int.MaxValue);

                    // Use policy-aware removal check (Story F1)
                    if (currentCount >= maxAllowed && OverrideMergePolicyEnforcer.CanRemoveOnset(onset, mergePolicy))
                    {
                        // Story G1: Record prune decision
                        collector?.RecordPrune(
                            GrooveDiagnosticsCollector.MakeOnsetId(onset),
                            $"candidate cap exceeded ({maxAllowed})",
                            onset.IsMustHit || onset.IsNeverRemove || onset.IsProtected);
                        continue;
                    }

                    currentCounts[key] = currentCount + 1;
                }

                result.Add(onset);
            }

            return result;
        }

        private List<GrooveOnset> EnforceGroupCaps(
            List<GrooveOnset> onsets,
            GrooveVariationCatalog? catalog,
            GrooveOverrideMergePolicy mergePolicy,
            GrooveDiagnosticsCollector? collector)
        {
            if (catalog == null)
                return onsets;

            // Build group cap lookup
            var groupCaps = new Dictionary<string, int>();
            foreach (var layer in catalog.HierarchyLayers)
            {
                foreach (var group in layer.CandidateGroups)
                {
                    if (group.MaxAddsPerBar > 0)
                        groupCaps[group.GroupId] = group.MaxAddsPerBar;
                }
            }

            // Count and prune per group
            var result = new List<GrooveOnset>();
            var groupCounts = new Dictionary<string, int>();

            // Score and sort onsets for deterministic pruning
            var scoredOnsets = onsets
                .Select(o => new
                {
                    Onset = o,
                    Score = ComputePruneScore(o, catalog)
                })
                .OrderByDescending(x => x.Score) // Higher score = keep first
                .ThenBy(x => x.Onset.Beat) // Stable tie-break
                .ToList();

            foreach (var item in scoredOnsets)
            {
                var onset = item.Onset;
                if (onset.Provenance?.GroupId != null)
                {
                    var groupId = onset.Provenance.GroupId;
                    int currentCount = groupCounts.GetValueOrDefault(groupId);
                    int maxAllowed = groupCaps.GetValueOrDefault(groupId, int.MaxValue);

                    // Use policy-aware removal check (Story F1)
                    if (currentCount >= maxAllowed && OverrideMergePolicyEnforcer.CanRemoveOnset(onset, mergePolicy))
                    {
                        // Story G1: Record prune decision
                        collector?.RecordPrune(
                            GrooveDiagnosticsCollector.MakeOnsetId(onset),
                            $"group cap exceeded ({groupId}: {maxAllowed})",
                            onset.IsMustHit || onset.IsNeverRemove || onset.IsProtected);
                        continue;
                    }

                    groupCounts[groupId] = currentCount + 1;
                }

                result.Add(onset);
            }


            // Restore original order
            return result.OrderBy(o => o.Beat).ToList();
        }

        private List<GrooveOnset> EnforceRoleMaxDensityPerBar(
            List<GrooveOnset> onsets,
            string role,
            GroovePresetDefinition preset,
            GrooveOverrideMergePolicy mergePolicy,
            GrooveDiagnosticsCollector? collector)
        {
            var roleConstraints = preset.ProtectionPolicy?.RoleConstraintPolicy?.RoleMaxDensityPerBar;
            if (roleConstraints == null || !roleConstraints.TryGetValue(role, out int maxDensity))
                return onsets;

            if (onsets.Count <= maxDensity)
                return onsets;

            return PruneToCount(onsets, maxDensity, preset.VariationCatalog, mergePolicy, collector, "role density cap");
        }

        private List<GrooveOnset> EnforceMaxHitsPerBar(
            List<GrooveOnset> onsets,
            string role,
            GroovePresetDefinition preset,
            GrooveOverrideMergePolicy mergePolicy,
            GrooveDiagnosticsCollector? collector)
        {
            var vocab = preset.ProtectionPolicy?.RoleConstraintPolicy?.RoleVocabulary;
            if (vocab == null || !vocab.TryGetValue(role, out var roleVocab))
                return onsets;

            int maxHits = roleVocab.MaxHitsPerBar;
            if (onsets.Count <= maxHits)
                return onsets;

            return PruneToCount(onsets, maxHits, preset.VariationCatalog, mergePolicy, collector, "max hits per bar");
        }

        private List<GrooveOnset> EnforceMaxHitsPerBeat(
            List<GrooveOnset> onsets,
            string role,
            GroovePresetDefinition preset,
            int barNumber,
            int rngSeed,
            GrooveOverrideMergePolicy mergePolicy,
            GrooveDiagnosticsCollector? collector)
        {
            var vocab = preset.ProtectionPolicy?.RoleConstraintPolicy?.RoleVocabulary;
            if (vocab == null || !vocab.TryGetValue(role, out var roleVocab))
                return onsets;

            int maxHitsPerBeat = roleVocab.MaxHitsPerBeat;

            // Group by beat bucket (integer part of beat position)
            var beatBuckets = onsets.GroupBy(o => (int)Math.Floor(o.Beat)).ToList();

            var result = new List<GrooveOnset>();
            var rng = new Random(rngSeed);

            foreach (var bucket in beatBuckets)
            {
                var beatOnsets = bucket.ToList();
                if (beatOnsets.Count <= maxHitsPerBeat)
                {
                    result.AddRange(beatOnsets);
                    continue;
                }

                int beatNumber = bucket.Key;

                // Separate non-removable from prunable using policy (Story F1)
                var nonRemovable = beatOnsets.Where(o => !OverrideMergePolicyEnforcer.CanRemoveOnset(o, mergePolicy)).ToList();
                var prunable = beatOnsets.Where(o => OverrideMergePolicyEnforcer.CanRemoveOnset(o, mergePolicy)).ToList();

                // If non-removable already exceed limit, keep all non-removable (configuration error)
                if (nonRemovable.Count >= maxHitsPerBeat)
                {
                    result.AddRange(nonRemovable);
                    continue;
                }

                int prunableToKeep = maxHitsPerBeat - nonRemovable.Count;

                // Score and sort prunable for deterministic selection
                var scored = prunable
                    .Select(o => new
                    {
                        Onset = o,
                        Score = ComputePruneScore(o, preset.VariationCatalog)
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Onset.Beat) // Stable tie-break by exact position
                    .ToList();

                // Keep top prunableToKeep, with RNG tie-break if needed
                var toKeep = scored.Take(prunableToKeep).ToList();

                // If we have ties at the boundary, use RNG
                bool usedTieBreak = false;
                if (toKeep.Count == prunableToKeep && scored.Count > prunableToKeep)
                {
                    var boundaryScore = toKeep.Last().Score;
                    var tiedOnsets = scored.Where(x => Math.Abs(x.Score - boundaryScore) < 0.0001).ToList();

                    if (tiedOnsets.Count > 1)
                    {
                        // Use RNG to break tie
                        var shuffled = tiedOnsets.OrderBy(x => rng.Next()).ToList();
                        toKeep = scored
                            .Where(x => x.Score > boundaryScore || shuffled.Take(1).Contains(x))
                            .Take(prunableToKeep)
                            .ToList();
                        usedTieBreak = true;
                    }
                }

                result.AddRange(nonRemovable);
                result.AddRange(toKeep.Select(x => x.Onset));

                // Story G1: Record prune decisions for removed onsets
                int prunedCount = beatOnsets.Count - nonRemovable.Count - toKeep.Count;
                if (collector != null && prunedCount > 0)
                {
                    var keptBeats = new HashSet<decimal>(toKeep.Select(x => x.Onset.Beat));
                    foreach (var prunable_onset in prunable.Where(o => !keptBeats.Contains(o.Beat)))
                    {
                        string reason = usedTieBreak ? "per-beat cap (tie-break)" : "per-beat cap exceeded";
                        collector.RecordPrune(
                            GrooveDiagnosticsCollector.MakeOnsetId(prunable_onset),
                            reason,
                            prunable_onset.IsMustHit || prunable_onset.IsNeverRemove || prunable_onset.IsProtected);
                    }
                }
            }

            return result.OrderBy(o => o.Beat).ToList();
        }

        private List<GrooveOnset> PruneToCount(
            List<GrooveOnset> onsets,
            int targetCount,
            GrooveVariationCatalog? catalog,
            GrooveOverrideMergePolicy mergePolicy,
            GrooveDiagnosticsCollector? collector,
            string pruneReason)
        {
            if (onsets.Count <= targetCount)
                return onsets;

            // Separate non-removable from prunable onsets using policy (Story F1)
            var nonRemovable = onsets.Where(o => !OverrideMergePolicyEnforcer.CanRemoveOnset(o, mergePolicy)).ToList();
            var prunable = onsets.Where(o => OverrideMergePolicyEnforcer.CanRemoveOnset(o, mergePolicy)).ToList();

            // If non-removable count already exceeds target, keep all non-removable (configuration error)
            if (nonRemovable.Count >= targetCount)
            {
                return nonRemovable;
            }

            // Calculate how many prunable we can keep
            int prunableToKeep = targetCount - nonRemovable.Count;

            if (prunable.Count <= prunableToKeep)
            {
                // No pruning needed for prunables
                return nonRemovable.Concat(prunable).OrderBy(o => o.Beat).ToList();
            }

            // Score and sort prunable onsets for deterministic pruning
            var scored = prunable
                .Select(o => new
                {
                    Onset = o,
                    Score = ComputePruneScore(o, catalog)
                })
                .OrderByDescending(x => x.Score) // Higher score = keep
                .ThenBy(x => x.Onset.Beat) // Stable tie-break
                .ToList();

            var keptPrunable = scored.Take(prunableToKeep).Select(x => x.Onset).ToList();

            // Story G1: Record prune decisions for removed onsets
            if (collector != null)
            {
                var keptBeats = new HashSet<decimal>(keptPrunable.Select(o => o.Beat));
                foreach (var prunedOnset in prunable.Where(o => !keptBeats.Contains(o.Beat)))
                {
                    collector.RecordPrune(
                        GrooveDiagnosticsCollector.MakeOnsetId(prunedOnset),
                        pruneReason,
                        prunedOnset.IsMustHit || prunedOnset.IsNeverRemove || prunedOnset.IsProtected);
                }
            }

            return nonRemovable.Concat(keptPrunable).OrderBy(o => o.Beat).ToList();
        }

        /// <summary>
        /// Computes a deterministic prune score for an onset. Higher score = less likely to be pruned.
        /// </summary>
        private double ComputePruneScore(GrooveOnset onset, GrooveVariationCatalog? catalog)
        {
            double score = 0.0;

            // Protection level (highest priority)
            if (onset.IsMustHit)
                return double.MaxValue; // Never prune
            if (onset.IsNeverRemove)
                return 10000.0; // Very high priority
            if (onset.IsProtected)
                score += 100.0;

            // Onset strength (musical importance)
            score += onset.Strength switch
            {
                OnsetStrength.Downbeat => 50.0,
                OnsetStrength.Backbeat => 45.0,
                OnsetStrength.Strong => 40.0,
                OnsetStrength.Pickup => 30.0,
                OnsetStrength.Offbeat => 20.0,
                OnsetStrength.Ghost => 10.0,
                _ => 25.0
            };

            // Candidate probability bias (variation weight)
            if (onset.Provenance?.GroupId != null && catalog != null)
            {
                var candidate = FindCandidate(onset, catalog);
                if (candidate != null)
                {
                    var group = FindGroup(onset.Provenance.GroupId, catalog);
                    double weight = candidate.ProbabilityBias * (group?.BaseProbabilityBias ?? 1.0);
                    score += weight * 10.0; // Scale to reasonable range
                }
            }

            // Provenance: anchors slightly preferred over variations
            if (onset.Provenance?.Source == GrooveOnsetSource.Anchor)
                score += 5.0;

            return score;
        }

        private GrooveOnsetCandidate? FindCandidate(GrooveOnset onset, GrooveVariationCatalog catalog)
        {
            foreach (var layer in catalog.HierarchyLayers)
            {
                foreach (var group in layer.CandidateGroups)
                {
                    if (group.GroupId == onset.Provenance?.GroupId)
                    {
                        var candidate = group.Candidates.FirstOrDefault(c =>
                            c.Role == onset.Role &&
                            Math.Abs(c.OnsetBeat - onset.Beat) < 0.001m);

                        if (candidate != null)
                            return candidate;
                    }
                }
            }

            return null;
        }

        private GrooveCandidateGroup? FindGroup(string groupId, GrooveVariationCatalog catalog)
        {
            foreach (var layer in catalog.HierarchyLayers)
            {
                var group = layer.CandidateGroups.FirstOrDefault(g => g.GroupId == groupId);
                if (group != null)
                    return group;
            }

            return null;
        }
    }
}
