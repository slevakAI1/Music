// AI: purpose=Compute density target count for drum selection; role-based, deterministic, no RNG.
// AI: invariants=Result clamped to [0..MaxEventsPerBar]; same inputs produce same output; no side-effects.
// AI: deps=Uses Bar context; external policy or role density may adjust density01 before calling.

using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators
{
    // AI: result=Contains computed integer TargetCount and provenance for diagnostics
    public sealed record GrooveDensityResult_Save(
        int TargetCount,
        double Density01Used,
        int MaxEventsPerBarUsed,
        string Explanation);

    // AI: purpose=Deterministic density calculator; formula: round(Density01*MaxEvents) clamped to valid range
    public static class Save_DrumDensityCalculator
    {
        // AI: behavior=Compute target count deterministically using MidpointRounding.AwayFromZero
        public static GrooveDensityResult_Save ComputeDensityTarget(
            Bar bar,
            string role,
            double density01 = 0.5,
            int maxEventsPerBar = 16)
        {
            ArgumentNullException.ThrowIfNull(bar);
            ArgumentException.ThrowIfNullOrWhiteSpace(role);

            double densityEffective = Clamp01(density01);
            int maxEventsEffective = Math.Max(0, maxEventsPerBar);

            double raw = densityEffective * maxEventsEffective;
            int targetCount = (int)Math.Round(raw, MidpointRounding.AwayFromZero);

            targetCount = Math.Clamp(targetCount, 0, maxEventsEffective);

            string explanation = $"Density={densityEffective:F2}, MaxEvents={maxEventsEffective}, Target={targetCount}";

            return new GrooveDensityResult_Save(
                TargetCount: targetCount,
                Density01Used: densityEffective,
                MaxEventsPerBarUsed: maxEventsEffective,
                Explanation: explanation);
        }

        // AI: util=Clamp value to [0.0,1.0]
        private static double Clamp01(double value)
        {
            return Math.Clamp(value, 0.0, 1.0);
        }

        // AI: util=BuildExplanation is retained for richer diagnostics; not used by default path
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
