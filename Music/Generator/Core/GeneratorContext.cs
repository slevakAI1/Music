// AI: purpose=Shared context for all agent decisions; immutable record ensures determinism.
// AI: invariants=Seed and RngStreamKey are required; keep deterministic RNG stream isolation stable.
// AI: deps=Rng system for RngStreamKey.
// AI: change=Extend via inheritance for instrument-specific contexts (DrummerContext, GuitarContext, etc.).

namespace Music.Generator.Core
{
    /// <summary>
    /// Base context provided to all musical operators for decision-making.
    /// Immutable record ensures deterministic behavior across runs.
    /// Instrument-specific agents extend this with additional fields.
    /// </summary>
    public record GeneratorContext
    {
        /// <summary>
        /// Master seed for deterministic generation.
        /// Same seed + same context = identical operator outputs.
        /// </summary>
        public required int Seed { get; init; }

        /// <summary>
        /// Key for selecting the appropriate RNG stream within the Rng system.
        /// Format: "{AgentType}_{Purpose}_{BarNumber}" for isolation.
        /// </summary>
        public required string RngStreamKey { get; init; }

        /// <summary>
        /// Creates a minimal context for testing purposes.
        /// All numeric values default to mid-range or zero.
        /// </summary>
        public static GeneratorContext CreateMinimal(
            int barNumber = 1,
            int seed = 42)
        {
            return new GeneratorContext
            {
                Seed = seed,
                RngStreamKey = $"Test_{barNumber}"
            };
        }
    }
}
