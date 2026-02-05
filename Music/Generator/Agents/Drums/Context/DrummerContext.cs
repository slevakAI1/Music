// AI: purpose=Drummer-specific context extending AgentContext; immutable record for deterministic operator decisions.
// AI: invariants=All fields immutable; Bar is canonical; ActiveRoles subset of GrooveRoles; BackbeatBeats valid for time signature.
// AI: deps=AgentContext base type; MotifPresenceMap optional; GrooveRoles; consumed by DrummerOperators.
// AI: change=Story 2.1, 9.3; extend with additional drum-specific fields as operators require; keep immutable.

using Music.Generator.Agents.Common;

using Music.Generator.Groove;
using Music.Generator.Material;

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
    /// Drummer-specific context extending AgentContext with drum-relevant fields.
    /// Provides operators with information about bar context, roles, recent hits, subdivision modes, and phrase positions.
    /// Story 2.1: Define Drummer-Specific Context.
    /// </summary>
    public sealed record DrummerContext : AgentContext
    {
        /// <summary>
        /// Canonical bar context for the current decisions.
        /// </summary>
        public required Bar Bar { get; init; }

        /// <summary>
        /// Energy level for this bar (0.0-1.0), derived from section intent.
        /// </summary>
        public required double EnergyLevel { get; init; }

        /// <summary>
        /// Optional motif presence map for motif-aware ducking.
        /// </summary>
        public MotifPresenceMap? MotifPresenceMap { get; init; }

        /// <summary>
        /// Which drum roles are enabled for this bar.
        /// Subset of GrooveRoles (Kick, Snare, ClosedHat, OpenHat, Crash, Ride, Tom1, Tom2, FloorTom).
        /// </summary>
        public required IReadOnlySet<string> ActiveRoles { get; init; }

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
        /// Current hi-hat timekeeping mode (Closed, Open, or Ride).
        /// Drives hat-related operators and subdivision decisions.
        /// </summary>
        public required HatMode CurrentHatMode { get; init; }

        /// <summary>
        /// Current hi-hat subdivision density (None, Eighth, Sixteenth).
        /// Affects HatLift/HatDrop operators and density calculations.
        /// </summary>
        public required HatSubdivision HatSubdivision { get; init; }

        /// <summary>
        /// True if current bar is within a fill window (typically phrase-end bars).
        /// Enables fill operators (TurnaroundFillShort, TurnaroundFillFull, etc.).
        /// </summary>
        public required bool IsFillWindow { get; init; }

        /// <summary>
        /// True if current bar is at a section boundary (first or last bar of section).
        /// Enables section-aware operators (CrashOnOne, SetupHit, etc.).
        /// </summary>
        public required bool IsAtSectionBoundary { get; init; }

        /// <summary>
        /// Backbeat beat positions for the current time signature (1-based integers).
        /// For 4/4: typically [2, 4]. For 3/4: typically [2]. For 6/8: typically [4].
        /// Used by backbeat-related operators.
        /// </summary>
        public required IReadOnlyList<int> BackbeatBeats { get; init; }

        /// <summary>
        /// Numerator of the current time signature (beats per bar).
        /// Used for beat validation and backbeat computation.
        /// </summary>
        public required int BeatsPerBar { get; init; }

        /// <summary>
        /// Creates a minimal DrummerContext for testing purposes.
        /// Extends AgentContext.CreateMinimal with drum-specific defaults.
        /// </summary>
        internal static double ResolveEnergyLevel(Bar bar)
        {
            var sectionType = bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            return sectionType switch
            {
                MusicConstants.eSectionType.Chorus => 0.8,
                MusicConstants.eSectionType.Solo => 0.7,
                MusicConstants.eSectionType.Bridge => 0.4,
                MusicConstants.eSectionType.Intro => 0.4,
                MusicConstants.eSectionType.Outro => 0.5,
                _ => 0.5
            };
        }

        public static DrummerContext CreateMinimal(
            int barNumber = 1,
            MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse,
            int seed = 42,
            IReadOnlySet<string>? activeRoles = null,
            IReadOnlyList<int>? backbeatBeats = null,
            MotifPresenceMap? motifPresenceMap = null)
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

            var energyLevel = ResolveEnergyLevel(bar);

            return new DrummerContext
            {
                // Base AgentContext fields
                Bar = bar,
                EnergyLevel = energyLevel,
                Seed = seed,
                RngStreamKey = $"Drummer_{barNumber}",
                MotifPresenceMap = motifPresenceMap,

                // Drummer-specific fields
                ActiveRoles = activeRoles ?? new HashSet<string> { GrooveRoles.Kick, GrooveRoles.Snare, GrooveRoles.ClosedHat },
                LastKickBeat = null,
                LastSnareBeat = null,
                CurrentHatMode = HatMode.Closed,
                HatSubdivision = HatSubdivision.Eighth,
                IsFillWindow = false,
                IsAtSectionBoundary = false,
                BackbeatBeats = backbeatBeats ?? new List<int> { 2, 4 },
                BeatsPerBar = 4
            };
        }
    }
}
