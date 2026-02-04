// AI: purpose=Compute density target count for drum candidate selection (moved from Groove in Story 4.3).
// AI: invariants=Deterministic; same inputs => same output; no RNG; clamps to [0..MaxEvents].
// AI: deps=BarContext, RoleDensityTarget, DrumPolicyDecision.
// AI: change=Story 5.3: Simplified, removed deleted policy dependencies.


using Music.Generator.Agents.Common;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums
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
    /// Computes density target count for drum candidate selection.
    /// Story C1: Implements role-based density calculation.
    /// Story 4.3: Moved to Drum Generator namespace (part-generation concerns).
    /// Story 5.3: Simplified after policy deletion.
    /// </summary>
    public static class DrumDensityCalculator
    {
        /// <summary>
        /// Computes the target count of candidates to select for a role in a bar.
        /// Story C1: TargetCount = round(Density01 * MaxEventsPerBar) clamped to [0..MaxEventsPerBar].
        /// </summary>
        /// <param name="barContext">Bar context with segment profile.</param>
        /// <param name="role">Role name (e.g., "Kick", "Snare").</param>
        /// <param name="density01">Density value [0.0..1.0].</param>
        /// <param name="maxEventsPerBar">Maximum events per bar.</param>
        /// <returns>Density result with target count and provenance information.</returns>
        public static GrooveDensityResult ComputeDensityTarget(
            BarContext barContext,
            string role,
            double density01 = 0.5,
            int maxEventsPerBar = 16)
        {
            ArgumentNullException.ThrowIfNull(barContext);
            ArgumentException.ThrowIfNullOrWhiteSpace(role);

            // Clamp inputs
            double densityEffective = Clamp01(density01);
            int maxEventsEffective = Math.Max(0, maxEventsPerBar);

            // Compute target count with rounding (MidpointRounding.AwayFromZero)
            double raw = densityEffective * maxEventsEffective;
            int targetCount = (int)Math.Round(raw, MidpointRounding.AwayFromZero);

            // Clamp target count to [0..maxEventsEffective]
            targetCount = Math.Clamp(targetCount, 0, maxEventsEffective);

            string explanation = $"Density={densityEffective:F2}, MaxEvents={maxEventsEffective}, Target={targetCount}";

            return new GrooveDensityResult(
                TargetCount: targetCount,
                Density01Used: densityEffective,
                MaxEventsPerBarUsed: maxEventsEffective,
                Explanation: explanation);
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
