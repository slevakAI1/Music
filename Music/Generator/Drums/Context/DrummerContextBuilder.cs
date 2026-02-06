// AI: purpose=Stateless builder: construct DrummerContext from Bar + cross-bar state.
// AI: invariants=Deterministic: same input => same output; builder holds no state; beats/bar 1-based.
// AI: deps=Bar supplies bar-derived flags; consumed by DrummerOperators; no external policies.
// AI: change=Epic DrummerContext-Dedup: minimize cross-bar state to LastKickBeat/LastSnareBeat.

namespace Music.Generator.Drums.Context
{
    // AI: purpose=Container of inputs required to build a DrummerContext; avoids long param lists.
    public sealed record DrummerContextBuildInput
    {
        // Per-bar canonical context. Must be provided; Bar is authoritative for bar-derived flags.
        public required Bar Bar { get; init; }

        // Deterministic seed for RNG. Default chosen for tests; change deliberately.
        public int Seed { get; init; } = 42;

        // Last kick beat from prior context (1-based fractional). Null when unknown.
        public decimal? LastKickBeat { get; init; }

        // Last snare beat from prior context (1-based fractional). Null when unknown.
        public decimal? LastSnareBeat { get; init; }
    }

    // AI: purpose=Create DrummerContext from DrummerContextBuildInput; keep deterministic and minimal.
    public static class DrummerContextBuilder
    {
        // Build a DrummerContext from inputs. Throws ArgumentNullException when input or input.Bar is null.
        // Deterministic: RngStreamKey derived from bar number; do not include volatile state here.
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
