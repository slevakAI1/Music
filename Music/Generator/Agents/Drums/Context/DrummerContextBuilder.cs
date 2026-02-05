// AI: purpose=Builds DrummerContext from Bar and runtime state; pure builder for determinism.
// AI: invariants=Builder is stateless; same inputs produce identical DrummerContext; bars/beats 1-based.
// AI: deps=Bar, GrooveRoles.
// AI: change=Story 5.3: Simplified, removed deleted policy dependencies.

using Music.Generator.Agents.Common;
using Music.Generator.Groove;
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

        // AI: disconnect=Policy; policy decision removed while validating operator-only phrase generation.

        /// <summary>Seed for deterministic generation.</summary>
        public int Seed { get; init; } = 42;

        /// <summary>Energy level for this bar (0.0-1.0).</summary>
        public double EnergyLevel { get; init; } = 0.5;

        /// <summary>Tension level for this bar (0.0-1.0).</summary>
        public double TensionLevel { get; init; } = 0.0;

        /// <summary>Motif presence score for this bar (0.0-1.0).</summary>
        public double MotifPresenceScore { get; init; } = 0.0;

        /// <summary>Beats per bar (time signature numerator).</summary>
        public int BeatsPerBar { get; init; } = 4;

        /// <summary>Optional override for active roles; null uses orchestration policy defaults.</summary>
        public IReadOnlySet<string>? ActiveRolesOverride { get; init; }

        /// <summary>Last kick beat position from previous bar (1-based, fractional). Null if unknown.</summary>
        public decimal? LastKickBeat { get; init; }

        /// <summary>Last snare beat position from previous bar (1-based, fractional). Null if unknown.</summary>
        public decimal? LastSnareBeat { get; init; }

        /// <summary>Optional override for hat mode; null uses default based on energy.</summary>
        public HatMode? HatModeOverride { get; init; }

        /// <summary>Optional override for hat subdivision; null uses default based on energy.</summary>
        public HatSubdivision? HatSubdivisionOverride { get; init; }
    }

        /// <summary>
        /// Builds DrummerContext from Bar and related inputs.
        /// Stateless builder ensuring deterministic output for same inputs.
        /// Story 2.1: DrummerContextBuilder builds from Bar + policies.
        /// </summary>
    public static class DrummerContextBuilder
    {
        /// <summary>
        /// Default drum roles enabled when no orchestration policy is present.
        /// </summary>
        private static readonly IReadOnlySet<string> DefaultActiveRoles = new HashSet<string>
        {
            GrooveRoles.Kick,
            GrooveRoles.Snare,
            GrooveRoles.ClosedHat
        };

        /// <summary>
        /// All possible drum roles for validation.
        /// </summary>
        private static readonly IReadOnlySet<string> AllDrumRoles = new HashSet<string>
        {
            GrooveRoles.Kick,
            GrooveRoles.Snare,
            GrooveRoles.ClosedHat,
            GrooveRoles.OpenHat,
            "Crash",
            "Ride",
            "Tom1",
            "Tom2",
            "FloorTom"
        };

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

            // Resolve section type from Bar
            var sectionType = bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;

            // Determine active roles from defaults or overrides
            var activeRoles = ResolveActiveRoles(input, sectionType);

            // Compute backbeat beats for the time signature
            var backbeatBeats = ComputeBackbeatBeats(input.BeatsPerBar);

            // Determine if at section boundary (first or last bar)
            bool isAtSectionBoundary = bar.BarWithinSection == 0 || bar.BarsUntilSectionEnd == 0;

            // Determine hat mode and subdivision based on energy and overrides
            var hatMode = ResolveHatMode(input);
            var hatSubdivision = ResolveHatSubdivision(input);

            // Build RNG stream key
            string rngStreamKey = $"Drummer_Bar{bar.BarNumber}";

            return new DrummerContext
            {
                // Base AgentContext fields
                Bar = bar,
                Beat = 1.0m,
                EnergyLevel = input.EnergyLevel,
                TensionLevel = input.TensionLevel,
                MotifPresenceScore = input.MotifPresenceScore,
                Seed = input.Seed,
                RngStreamKey = rngStreamKey,

                // Drummer-specific fields
                ActiveRoles = activeRoles,
                LastKickBeat = input.LastKickBeat,
                LastSnareBeat = input.LastSnareBeat,
                CurrentHatMode = hatMode,
                HatSubdivision = hatSubdivision,
                IsFillWindow = isAtSectionBoundary, // Simplified: fill at boundaries
                IsAtSectionBoundary = isAtSectionBoundary,
                BackbeatBeats = backbeatBeats,
                BeatsPerBar = input.BeatsPerBar
            };
        }

        /// <summary>
        /// Resolves active roles from override or defaults.
        /// </summary>
        private static IReadOnlySet<string> ResolveActiveRoles(DrummerContextBuildInput input, MusicConstants.eSectionType sectionType)
        {
            // Use explicit override if provided
            if (input.ActiveRolesOverride != null)
                return ValidateRoles(input.ActiveRolesOverride);

            // Fall back to defaults
            return DefaultActiveRoles;
        }

        /// <summary>
        /// Validates that provided roles are in the allowed set.
        /// </summary>
        private static IReadOnlySet<string> ValidateRoles(IReadOnlySet<string> roles)
        {
            var validated = roles.Where(r => AllDrumRoles.Contains(r)).ToHashSet();
            return validated.Count > 0 ? validated : DefaultActiveRoles;
        }

        /// <summary>
        /// Computes backbeat beats for a given time signature numerator.
        /// </summary>
        private static IReadOnlyList<int> ComputeBackbeatBeats(int beatsPerBar)
        {
            return beatsPerBar switch
            {
                2 => new List<int> { 2 },           // 2/4: backbeat on 2
                3 => new List<int> { 2 },           // 3/4: backbeat on 2 (waltz)
                4 => new List<int> { 2, 4 },        // 4/4: backbeats on 2 and 4
                5 => new List<int> { 3, 5 },        // 5/4: beats 3 and 5 (3+2 grouping)
                6 => new List<int> { 4 },           // 6/8: backbeat on 4 (compound duple)
                7 => new List<int> { 3, 5, 7 },     // 7/8: beats 3, 5, 7 (2+2+3 grouping)
                _ => beatsPerBar >= 4               // Default: even beats
                    ? Enumerable.Range(1, beatsPerBar).Where(b => b % 2 == 0).ToList()
                    : new List<int> { beatsPerBar > 1 ? 2 : 1 }
            };
        }

        /// <summary>
        /// Resolves hat mode based on override or energy-based defaults.
        /// </summary>
        private static HatMode ResolveHatMode(DrummerContextBuildInput input)
        {
            // Use explicit override if provided
            if (input.HatModeOverride.HasValue)
                return input.HatModeOverride.Value;

            // Energy-based defaults: higher energy may use ride
            if (input.EnergyLevel >= 0.8)
                return HatMode.Ride;

            return HatMode.Closed;
        }

        /// <summary>
        /// Resolves hat subdivision based on override or energy-based defaults.
        /// </summary>
        private static HatSubdivision ResolveHatSubdivision(DrummerContextBuildInput input)
        {
            // Use explicit override if provided
            if (input.HatSubdivisionOverride.HasValue)
                return input.HatSubdivisionOverride.Value;

            // Energy-based defaults: higher energy uses denser subdivision
            if (input.EnergyLevel >= 0.7)
                return HatSubdivision.Sixteenth;
            if (input.EnergyLevel >= 0.3)
                return HatSubdivision.Eighth;

            return HatSubdivision.None;
        }
    }
}
