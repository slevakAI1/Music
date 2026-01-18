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
    /// 
    /// Enforcement order: Candidate caps -> Group caps -> Role density -> MaxHitsPerBar -> MaxHitsPerBeat
    /// 
    /// Protection hierarchy:
    /// - IsMustHit/IsNeverRemove: Never pruned (absolute protection)
    /// - IsProtected: Higher score, less likely to be pruned but can be if necessary
    /// - Unprotected: Pruned first based on score (strength, probability bias, provenance)
    /// 
    /// RNG Usage: Currently uses Random(rngSeed) for deterministic tie-breaking.
    /// TODO: When Story A2 RngFor helper is implemented, replace with RngFor(bar, role, "PrunePick").
    /// </remarks>
    public sealed class GrooveCapsEnforcer
    {
        /// <summary>
        /// Enforces all hard caps on a groove bar plan, pruning excess onsets deterministically.
        /// </summary>
        /// <param name="barPlan">The bar plan with selected onsets to enforce caps on.</param>
        /// <param name="preset">The groove preset definition containing constraint policies.</param>
        /// <param name="segmentProfile">The segment profile for this bar (optional).</param>
        /// <param name="variationCatalog">The variation catalog for provenance lookup (optional).</param>
        /// <param name="rngSeed">Deterministic RNG seed for tie-breaking (bar number recommended).</param>
        /// <param name="diagnosticsEnabled">Whether to collect diagnostics (optional, no behavior change).</param>
        /// <returns>A new GrooveBarPlan with FinalOnsets after cap enforcement.</returns>
        public GrooveBarPlan EnforceHardCaps(
            GrooveBarPlan barPlan,
            GroovePresetDefinition preset,
            SegmentGrooveProfile? segmentProfile,
            GrooveVariationCatalog? variationCatalog,
            int rngSeed,
            bool diagnosticsEnabled = false)
        {
            ArgumentNullException.ThrowIfNull(barPlan);
            ArgumentNullException.ThrowIfNull(preset);

            // Start with all onsets: BaseOnsets + SelectedVariationOnsets, or FinalOnsets if already set
            var workingOnsets = barPlan.FinalOnsets != null && barPlan.FinalOnsets.Count > 0
                ? barPlan.FinalOnsets.ToList()
                : barPlan.BaseOnsets.Concat(barPlan.SelectedVariationOnsets).ToList();

            var diagnostics = diagnosticsEnabled ? new List<string>() : null;

            // Group onsets by role for per-role enforcement
            var onsetsByRole = workingOnsets.GroupBy(o => o.Role).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (role, roleOnsets) in onsetsByRole)
            {
                diagnostics?.Add($"Enforcing caps for role '{role}', bar {barPlan.BarNumber}, initial count: {roleOnsets.Count}");

                // Enforce caps in order: candidate -> group -> role -> per-beat
                var prunedOnsets = EnforceRoleCaps(
                    roleOnsets,
                    role,
                    preset,
                    segmentProfile,
                    variationCatalog,
                    barPlan.BarNumber,
                    rngSeed,
                    diagnostics);

                onsetsByRole[role] = prunedOnsets;
            }

            // Flatten back to single list
            var finalOnsets = onsetsByRole.Values.SelectMany(o => o).OrderBy(o => o.Beat).ToList();

            return barPlan with
            {
                FinalOnsets = finalOnsets,
                Diagnostics = diagnosticsEnabled ? string.Join(Environment.NewLine, diagnostics!) : null
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
            List<string>? diagnostics)
        {
            var workingOnsets = roleOnsets.ToList();

            // 1. Enforce per-candidate MaxAddsPerBar
            workingOnsets = EnforceCandidateCaps(workingOnsets, variationCatalog, diagnostics);

            // 2. Enforce per-group MaxAddsPerBar
            workingOnsets = EnforceGroupCaps(workingOnsets, variationCatalog, diagnostics);

            // 3. Enforce RoleMaxDensityPerBar (global role cap)
            workingOnsets = EnforceRoleMaxDensityPerBar(workingOnsets, role, preset, diagnostics);

            // 4. Enforce RoleRhythmVocabulary.MaxHitsPerBar
            workingOnsets = EnforceMaxHitsPerBar(workingOnsets, role, preset, diagnostics);

            // 5. Enforce RoleRhythmVocabulary.MaxHitsPerBeat
            workingOnsets = EnforceMaxHitsPerBeat(workingOnsets, role, preset, barNumber, rngSeed, diagnostics);

            return workingOnsets;
        }

        private List<GrooveOnset> EnforceCandidateCaps(
            List<GrooveOnset> onsets,
            GrooveVariationCatalog? catalog,
            List<string>? diagnostics)
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

                    if (currentCount >= maxAllowed && !onset.IsMustHit && !onset.IsNeverRemove)
                    {
                        diagnostics?.Add($"  Pruned onset at beat {onset.Beat} (candidate cap: {maxAllowed})");
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
            List<string>? diagnostics)
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

                    if (currentCount >= maxAllowed && !onset.IsMustHit && !onset.IsNeverRemove)
                    {
                        diagnostics?.Add($"  Pruned onset at beat {onset.Beat} from group '{groupId}' (group cap: {maxAllowed})");
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
            List<string>? diagnostics)
        {
            var roleConstraints = preset.ProtectionPolicy?.RoleConstraintPolicy?.RoleMaxDensityPerBar;
            if (roleConstraints == null || !roleConstraints.TryGetValue(role, out int maxDensity))
                return onsets;

            if (onsets.Count <= maxDensity)
                return onsets;

            diagnostics?.Add($"  Enforcing RoleMaxDensityPerBar: {maxDensity} (current: {onsets.Count})");

            return PruneToCount(onsets, maxDensity, preset.VariationCatalog, diagnostics);
        }

        private List<GrooveOnset> EnforceMaxHitsPerBar(
            List<GrooveOnset> onsets,
            string role,
            GroovePresetDefinition preset,
            List<string>? diagnostics)
        {
            var vocab = preset.ProtectionPolicy?.RoleConstraintPolicy?.RoleVocabulary;
            if (vocab == null || !vocab.TryGetValue(role, out var roleVocab))
                return onsets;

            int maxHits = roleVocab.MaxHitsPerBar;
            if (onsets.Count <= maxHits)
                return onsets;

            diagnostics?.Add($"  Enforcing MaxHitsPerBar: {maxHits} (current: {onsets.Count})");

            return PruneToCount(onsets, maxHits, preset.VariationCatalog, diagnostics);
        }

        private List<GrooveOnset> EnforceMaxHitsPerBeat(
            List<GrooveOnset> onsets,
            string role,
            GroovePresetDefinition preset,
            int barNumber,
            int rngSeed,
            List<string>? diagnostics)
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
                diagnostics?.Add($"  Beat {beatNumber} has {beatOnsets.Count} onsets, max allowed: {maxHitsPerBeat}");

                // Separate protected from prunable
                var protected_onsets = beatOnsets.Where(o => o.IsMustHit || o.IsNeverRemove).ToList();
                var prunable = beatOnsets.Where(o => !o.IsMustHit && !o.IsNeverRemove).ToList();

                // If protected already exceed limit, keep all protected (configuration error)
                if (protected_onsets.Count >= maxHitsPerBeat)
                {
                    diagnostics?.Add($"  Warning: Beat {beatNumber} protected onsets ({protected_onsets.Count}) exceed limit");
                    result.AddRange(protected_onsets);
                    continue;
                }

                int prunableToKeep = maxHitsPerBeat - protected_onsets.Count;

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

                        diagnostics?.Add($"  Used RNG tie-break at beat {beatNumber}");
                    }
                }

                result.AddRange(protected_onsets);
                result.AddRange(toKeep.Select(x => x.Onset));

                int pruned = beatOnsets.Count - protected_onsets.Count - toKeep.Count;
                diagnostics?.Add($"  Pruned {pruned} onsets from beat {beatNumber}");
            }

            return result.OrderBy(o => o.Beat).ToList();
        }

        private List<GrooveOnset> PruneToCount(
            List<GrooveOnset> onsets,
            int targetCount,
            GrooveVariationCatalog? catalog,
            List<string>? diagnostics)
        {
            if (onsets.Count <= targetCount)
                return onsets;

            // Separate protected from prunable onsets
            var protected_onsets = onsets.Where(o => o.IsMustHit || o.IsNeverRemove).ToList();
            var prunable = onsets.Where(o => !o.IsMustHit && !o.IsNeverRemove).ToList();

            // If protected count already exceeds target, keep all protected (configuration error)
            if (protected_onsets.Count >= targetCount)
            {
                diagnostics?.Add($"  Warning: Protected onsets ({protected_onsets.Count}) exceed target ({targetCount})");
                return protected_onsets;
            }

            // Calculate how many prunable we can keep
            int prunableToKeep = targetCount - protected_onsets.Count;

            if (prunable.Count <= prunableToKeep)
            {
                // No pruning needed for prunables
                return protected_onsets.Concat(prunable).OrderBy(o => o.Beat).ToList();
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
            var pruned = prunable.Count - keptPrunable.Count;

            diagnostics?.Add($"  Pruned {pruned} onsets to reach target count {targetCount}");

            return protected_onsets.Concat(keptPrunable).OrderBy(o => o.Beat).ToList();
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
