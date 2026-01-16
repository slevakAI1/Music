// AI: purpose=Expose lightweight drum-role knobs so higher-level stages (Stage 7) can drive drum behavior without rewriting engines.
// AI: invariants=All values are small, benign defaults so existing output is unchanged unless callers set them.
// AI: deps=Consumed by DrumVariationEngine, DrumFillEngine, and DrumTrackGenerator; changing defaults will alter drum generation.

namespace Music.Generator
{
    /// <summary>
    /// Tunable parameters for drum generation that Stage 7 (Section identity) can drive.
    /// These are intentionally lightweight and deterministic when held constant.
    /// Implements Story 6.5: expose drum-role parameters for A/A' variation hooks.
    /// </summary>
    public sealed class DrumRoleParameters
    {
        /// <summary>
        /// Multiplies density-related choices (ghost notes, extra hats/kicks, fill density).
        /// 1.0 = default behavior. Values &gt;1 increase activity; values &lt;1 reduce it.
        /// Range: [0.25, 2.5] is recommended for musical results.
        /// </summary>
        public double DensityMultiplier { get; init; } = 1.0;

        /// <summary>
        /// Additive bias applied to final computed velocities (signed). Small values recommended.
        /// Positive values increase loudness; negative values decrease it.
        /// Range: [-20, +20] is recommended.
        /// </summary>
        public double VelocityBias { get; init; } = 0.0;

        /// <summary>
        /// Probability [0..1] that the engine will choose more "busy" variations on a bar (tie-breaker).
        /// Adds to base probabilities for extra kicks/hats.
        /// </summary>
        public double BusyProbability { get; init; } = 0.0;

        /// <summary>
        /// Extra probability [0..1] of generating a fill on bars that would not otherwise be a transition.
        /// 0.0 = fills only at section boundaries (default).
        /// Higher values add fills within sections.
        /// </summary>
        public double FillProbability { get; init; } = 0.0;

        /// <summary>
        /// Multiplier applied to fill complexity (1.0 = default). Values &gt;1 make fills more complex.
        /// Scales the computed density/complexity of fills.
        /// Range: [0.5, 2.0] is recommended.
        /// </summary>
        public double FillComplexityMultiplier { get; init; } = 1.0;
    }
}