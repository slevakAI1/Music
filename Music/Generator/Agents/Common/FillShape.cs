// AI: purpose=Data structure for remembering fill characteristics; used by IAgentMemory to track recent fills.
// AI: invariants=FillShape is immutable record; BarPosition 1-based; DensityLevel 0.0-1.0.
// AI: deps=Referenced by IAgentMemory.GetLastFillShape(); agents use to avoid repetitive fill patterns.
// AI: change=Extend with additional fields if fill analysis needs more metadata (keep backward compat).

namespace Music.Generator.Agents.Common
{
    /// <summary>
    /// Describes the shape/characteristics of a fill for anti-repetition memory.
    /// Used by agents to remember and avoid repeating similar fill patterns.
    /// </summary>
    /// <param name="BarPosition">1-based bar number where the fill occurred.</param>
    /// <param name="RolesInvolved">Which roles participated in the fill (e.g., "Snare", "Kick", "Toms").</param>
    /// <param name="DensityLevel">Relative density of the fill (0.0 = sparse, 1.0 = dense).</param>
    /// <param name="DurationBars">How many bars the fill spanned (typically 1, but could be 0.5 or 2).</param>
    /// <param name="FillTag">Optional tag identifying the fill type (e.g., "SnareRoll", "TomPattern", "Breakdown").</param>
    public sealed record FillShape(
        int BarPosition,
        IReadOnlyList<string> RolesInvolved,
        double DensityLevel,
        decimal DurationBars,
        string? FillTag = null)
    {
        /// <summary>
        /// Creates an empty fill shape representing "no fill".
        /// </summary>
        public static FillShape Empty => new(
            BarPosition: 0,
            RolesInvolved: Array.Empty<string>(),
            DensityLevel: 0.0,
            DurationBars: 0);

        /// <summary>
        /// Returns true if this represents a meaningful fill (non-empty).
        /// </summary>
        public bool HasContent => BarPosition > 0 && RolesInvolved.Count > 0;
    }
}
