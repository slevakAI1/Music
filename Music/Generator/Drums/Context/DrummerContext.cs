// AI: purpose=Drummer-specific context extending AgentContext; minimal contract for operator decisions.
// AI: invariants=Immutable fields; Bar is canonical; only cross-bar state: LastKickBeat, LastSnareBeat.
// AI: deps=GeneratorContext, Bar, Section; consumed by DrummerOperators; Bar owns bar-derived flags.

using Music.Generator.Core;

namespace Music.Generator.Drums.Context
{
    // Hi-hat mode: which cymbal provides the timekeeping.
    public enum HatMode
    {
        // Closed hi-hat is active timekeeping cymbal.
        Closed,

        // Open hi-hat is active timekeeping cymbal.
        Open,

        // Ride cymbal is active timekeeping cymbal (instead of hi-hat).
        Ride
    }

    // Hi-hat subdivision: rhythmic density of the hi-hat pattern.
    public enum HatSubdivision
    {
        // No hi-hat subdivision (hats off or sparse).
        None,

        // Eighth note subdivision (2 hits per beat).
        Eighth,

        // Sixteenth note subdivision (4 hits per beat).
        Sixteenth
    }

    // AI: purpose=Drummer-specific GeneratorContext; minimal contract for deterministic operator choices.
    // AI: invariants=Cross-bar state only; Bar supplies all bar-derived properties; instances immutable.
    public sealed record DrummerContext : GeneratorContext
    {
        // Canonical bar context for current decisions. Bar is authoritative for bar-derived flags.
        public required Bar Bar { get; init; }

        // Beat position of the last kick hit (1-based, fractional). Null when unknown.
        // Used to coordinate bass and ghost-kick placement across operators.
        public decimal? LastKickBeat { get; init; }

        // Beat position of the last snare hit (1-based, fractional). Null when unknown.
        // Used to influence ghost notes and snare-related decisions.
        public decimal? LastSnareBeat { get; init; }

        // Create a minimal DrummerContext for tests and examples. Deterministic seed and bar only.
        // NOTE: This helper should not be used in production composition pipelines.
        public static DrummerContext CreateMinimal(
            int barNumber = 1,
            MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse,
            int seed = 42)
        {
            var section = new Section { SectionType = sectionType, StartBar = 1, BarCount = 8 };
            var bar = new Bar
            {
                BarNumber = barNumber,
                Section = section,
                BarWithinSection = Math.Max(0, barNumber - section.StartBar),
                BarsUntilSectionEnd = Math.Max(0, section.StartBar + section.BarCount - 1 - barNumber),
                Numerator = 4,
                Denominator = 4,
                StartTick = 0
            };
            bar.EndTick = bar.StartTick + bar.TicksPerMeasure;

            return new DrummerContext
            {
                Bar = bar,
                Seed = seed,
                RngStreamKey = $"Drummer_{barNumber}",
                LastKickBeat = null,
                LastSnareBeat = null
            };
        }
    }
}
