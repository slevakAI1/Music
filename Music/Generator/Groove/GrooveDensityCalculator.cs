// AI: purpose=Compute density target count for groove candidate selection (Story C1, F1).
// AI: invariants=Deterministic; same inputs => same output; no RNG; clamps to [0..MaxEvents].
// AI: deps=GrooveBarContext, RoleDensityTarget, GrooveOrchestrationPolicy, GroovePolicyDecision, GrooveOverrideMergePolicy.
// AI: change=Story F1: OverrideCanRelaxConstraints controls whether segment can increase caps beyond base.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Result of density target computation with provenance for diagnostics.
    /// Story C1: Contains target count and inputs used for calculation.
    /// </summary>
    public sealed record GrooveDensityResult(
        int TargetCount,
        double Density01Used,
        int MaxEventsPerBarUsed,
        string Explanation);

    /// <summary>
    /// Computes density target count for groove candidate selection.
    /// Story C1: Implements role + section + policy-based density calculation.
    /// Story F1: OverrideCanRelaxConstraints controls whether segment can increase caps.
    /// </summary>
    public static class GrooveDensityCalculator
    {
        /// <summary>
        /// Computes the target count of candidates to select for a role in a bar.
        /// Story C1: TargetCount = round(Density01 * MaxEventsPerBar) clamped to [0..MaxEventsPerBar].
        /// Story F1: When OverrideCanRelaxConstraints=false, maxEvents cannot exceed base policy caps.
        /// </summary>
        /// <param name="barContext">Bar context with segment profile.</param>
        /// <param name="role">Role name (e.g., "Kick", "Snare").</param>
        /// <param name="roleConstraintPolicy">Optional role constraint policy for base caps.</param>
        /// <param name="orchestrationPolicy">Optional orchestration policy for section multipliers.</param>
        /// <param name="policyDecision">Optional policy decision with overrides.</param>
        /// <param name="mergePolicy">Optional merge policy controlling cap relaxation.</param>
        /// <returns>Density result with target count and provenance information.</returns>
        public static GrooveDensityResult ComputeDensityTarget(
            GrooveBarContext barContext,
            string role,
            GrooveRoleConstraintPolicy? roleConstraintPolicy = null,
            GrooveOrchestrationPolicy? orchestrationPolicy = null,
            GroovePolicyDecision? policyDecision = null,
            GrooveOverrideMergePolicy? mergePolicy = null)
        {
            ArgumentNullException.ThrowIfNull(barContext);
            ArgumentException.ThrowIfNullOrWhiteSpace(role);

            // Step 1: Resolve base density and max events from segment profile
            var (densityBase, maxEventsBase) = ResolveBaseDensityAndMaxEvents(
                barContext,
                role,
                roleConstraintPolicy);

            // Step 2: Apply section multiplier if orchestration policy available
            double multiplier = ResolveSectionMultiplier(
                barContext,
                role,
                orchestrationPolicy);

            double densityAfterMultiplier = Clamp01(densityBase * multiplier);

            // Step 3: Apply policy overrides (highest precedence)
            double densityEffective;
            int maxEventsEffective;
            bool densityOverridden = false;
            bool maxEventsOverridden = false;
            bool capsRelaxed = false;

            if (policyDecision?.Density01Override is not null)
            {
                densityEffective = Clamp01(policyDecision.Density01Override.Value);
                densityOverridden = true;
            }
            else
            {
                densityEffective = densityAfterMultiplier;
            }

            if (policyDecision?.MaxEventsPerBarOverride is not null)
            {
                int requestedMax = Math.Max(0, policyDecision.MaxEventsPerBarOverride.Value);
                
                // Story F1: Apply OverrideCanRelaxConstraints policy
                maxEventsEffective = OverrideMergePolicyEnforcer.ResolveEffectiveMaxHitsPerBar(
                    maxEventsBase,
                    requestedMax,
                    mergePolicy ?? new GrooveOverrideMergePolicy());
                    
                maxEventsOverridden = true;
                capsRelaxed = mergePolicy?.OverrideCanRelaxConstraints == true && requestedMax > maxEventsBase;
            }
            else
            {
                maxEventsEffective = Math.Max(0, maxEventsBase);
            }

            // Step 4: Compute target count with rounding (MidpointRounding.AwayFromZero)
            double raw = densityEffective * maxEventsEffective;
            int targetCount = (int)Math.Round(raw, MidpointRounding.AwayFromZero);

            // Step 5: Clamp target count to [0..maxEventsEffective]
            targetCount = Math.Clamp(targetCount, 0, maxEventsEffective);

            // Build explanation for diagnostics
            string explanation = BuildExplanation(
                densityBase,
                maxEventsBase,
                multiplier,
                densityOverridden,
                maxEventsOverridden,
                capsRelaxed,
                densityEffective,
                maxEventsEffective,
                targetCount);

            return new GrooveDensityResult(
                TargetCount: targetCount,
                Density01Used: densityEffective,
                MaxEventsPerBarUsed: maxEventsEffective,
                Explanation: explanation);
        }

        /// <summary>
        /// Resolves base density and max events from segment profile or fallbacks.
        /// </summary>
        private static (double densityBase, int maxEventsBase) ResolveBaseDensityAndMaxEvents(
            GrooveBarContext barContext,
            string role,
            GrooveRoleConstraintPolicy? roleConstraintPolicy)
        {
            // Look for RoleDensityTarget in segment profile
            var densityTarget = barContext.SegmentProfile?.DensityTargets
                ?.FirstOrDefault(t => string.Equals(t.Role, role, StringComparison.Ordinal));

            if (densityTarget is not null)
            {
                return (densityTarget.Density01, densityTarget.MaxEventsPerBar);
            }

            // Fallback: density = 0.0, maxEvents from roleConstraintPolicy or 0
            double densityBase = 0.0;
            int maxEventsBase = 0;

            if (roleConstraintPolicy?.RoleMaxDensityPerBar?.TryGetValue(role, out int maxDensity) == true)
            {
                maxEventsBase = maxDensity;
            }
            else if (roleConstraintPolicy?.RoleVocabulary?.TryGetValue(role, out var vocab) == true)
            {
                maxEventsBase = vocab.MaxHitsPerBar;
            }

            return (densityBase, maxEventsBase);
        }

        /// <summary>
        /// Resolves section density multiplier from orchestration policy.
        /// Returns 1.0 if not found.
        /// </summary>
        private static double ResolveSectionMultiplier(
            GrooveBarContext barContext,
            string role,
            GrooveOrchestrationPolicy? orchestrationPolicy)
        {
            if (orchestrationPolicy is null)
            {
                return 1.0;
            }

            // Try to find matching section type from barContext.Section
            // Convert enum to string for comparison with orchestration policy
            string? sectionType = barContext.Section?.SectionType.ToString();

            if (string.IsNullOrEmpty(sectionType))
            {
                return 1.0;
            }

            var sectionDefaults = orchestrationPolicy.DefaultsBySectionType
                ?.FirstOrDefault(d => string.Equals(d.SectionType, sectionType, StringComparison.OrdinalIgnoreCase));

            if (sectionDefaults?.RoleDensityMultiplier?.TryGetValue(role, out double multiplier) == true)
            {
                // Clamp multiplier to be non-negative
                return Math.Max(0.0, multiplier);
            }

            return 1.0;
        }


        /// <summary>
        /// Clamps density value to [0.0, 1.0] range.
        /// </summary>
        private static double Clamp01(double value)
        {
            return Math.Clamp(value, 0.0, 1.0);
        }

        /// <summary>
        /// Builds a concise explanation string for diagnostics.
        /// </summary>
        private static string BuildExplanation(
            double densityBase,
            int maxEventsBase,
            double multiplier,
            bool densityOverridden,
            bool maxEventsOverridden,
            bool capsRelaxed,
            double densityEffective,
            int maxEventsEffective,
            int targetCount)
        {
            var parts = new List<string>();

            if (densityOverridden)
            {
                parts.Add($"densityOverride={densityEffective:F2}");
            }
            else
            {
                parts.Add($"densityBase={densityBase:F2}");
                if (multiplier != 1.0)
                {
                    parts.Add($"multiplier={multiplier:F2}");
                    parts.Add($"densityAfter={densityEffective:F2}");
                }
            }

            if (maxEventsOverridden)
            {
                parts.Add($"maxEventsOverride={maxEventsEffective}");
                if (capsRelaxed)
                {
                    parts.Add("(relaxed)");
                }
            }
            else
            {
                parts.Add($"maxEventsBase={maxEventsBase}");
            }

            parts.Add($"target={targetCount}");

            return string.Join("; ", parts);
        }
    }
}
