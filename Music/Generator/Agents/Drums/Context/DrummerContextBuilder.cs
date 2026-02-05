// AI: purpose=Builds DrummerContext from Bar and cross-bar state; minimal stateless builder.
// AI: invariants=Builder is stateless; same inputs produce identical DrummerContext; bars/beats 1-based.
// AI: deps=Bar (contains all bar-derivable properties); no policy dependencies.
// AI: change=Epic DrummerContext-Dedup; simplified to minimal cross-bar state only.

using Music.Generator;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Input configuration for building DrummerContext.
    /// Groups required inputs to avoid large parameter lists.
    /// </summary>
    public sealed record DrummerContextBuildInput
    {
        /// <summary>Per-bar context (section, phrase position).</summary>
        public required Bar Bar { get; init; }

        /// <summary>Seed for deterministic generation.</summary>
        public int Seed { get; init; } = 42;

        /// <summary>Last kick beat position from previous bar (1-based, fractional). Null if unknown.</summary>
        public decimal? LastKickBeat { get; init; }

        /// <summary>Last snare beat position from previous bar (1-based, fractional). Null if unknown.</summary>
        public decimal? LastSnareBeat { get; init; }
    }

    /// <summary>
    /// Builds DrummerContext from Bar and cross-bar state.
    /// Stateless builder ensuring deterministic output for same inputs.
    /// Story 2.1: DrummerContextBuilder builds from Bar + cross-bar state only.
    /// </summary>
    public static class DrummerContextBuilder
    {
        /// <summary>
        /// Builds a DrummerContext from the provided input configuration.
        /// </summary>
        /// <param name="input">Input configuration containing all required data.</param>
        /// <returns>Immutable DrummerContext for operator decisions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input or input.Bar is null.</exception>
        public static DrummerContext Build(DrummerContextBuildInput input)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(input.Bar);

            var bar = input.Bar;

            // Build RNG stream key
            string rngStreamKey = $"Drummer_Bar{bar.BarNumber}";

            return new DrummerContext
            {
                Bar = bar,
                Seed = input.Seed,
                RngStreamKey = rngStreamKey,
                LastKickBeat = input.LastKickBeat,
                LastSnareBeat = input.LastSnareBeat
            };
        }
    }
}
