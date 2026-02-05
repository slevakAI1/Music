// AI: purpose=Drummer-specific context extending AgentContext; minimal contract for deterministic operator decisions.
// AI: invariants=All fields immutable; Bar is canonical; cross-bar state only (LastKickBeat, LastSnareBeat).
// AI: deps=AgentContext base type; Bar contains all bar-derivable properties; consumed by DrummerOperators.
// AI: change=Story 2.1, Epic DrummerContext-Dedup; Bar owns IsAtSectionBoundary, IsFillWindow, BackbeatBeats, BeatsPerBar.

using Music.Generator.Agents.Common;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Hi-hat mode: which cymbal is providing the timekeeping.
    /// </summary>
    public enum HatMode
    {
        /// <summary>Closed hi-hat is active timekeeping cymbal.</summary>
        Closed,

        /// <summary>Open hi-hat is active timekeeping cymbal.</summary>
        Open,

        /// <summary>Ride cymbal is active timekeeping cymbal (instead of hi-hat).</summary>
        Ride
    }

    /// <summary>
    /// Hi-hat subdivision: the rhythmic density of the hi-hat pattern.
    /// </summary>
    public enum HatSubdivision
    {
        /// <summary>No hi-hat subdivision (hats off or sparse).</summary>
        None,

        /// <summary>Eighth note subdivision (2 hits per beat).</summary>
        Eighth,

        /// <summary>Sixteenth note subdivision (4 hits per beat).</summary>
        Sixteenth
    }

    /// <summary>
    /// Drummer-specific context extending AgentContext.
    /// Contains only cross-bar state (LastKickBeat, LastSnareBeat); all bar-derivable data accessed via Bar property.
    /// Story 2.1: Define Drummer-Specific Context.
    /// </summary>
    public sealed record DrummerContext : AgentContext
    {
        /// <summary>
        /// Canonical bar context for the current decisions.
        /// </summary>
        public required Bar Bar { get; init; }

        /// <summary>
        /// Beat position of the last kick hit in current/recent context (1-based, fractional).
        /// Null if no recent kick hit is known.
        /// Used for coordination with bass and ghost note placement.
        /// </summary>
        public decimal? LastKickBeat { get; init; }

        /// <summary>
        /// Beat position of the last snare hit in current/recent context (1-based, fractional).
        /// Null if no recent snare hit is known.
        /// Used for ghost note placement decisions.
        /// </summary>
        public decimal? LastSnareBeat { get; init; }

        /// <summary>
        /// Creates a minimal DrummerContext for testing purposes.
        /// </summary>
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
