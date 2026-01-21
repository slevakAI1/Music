// AI: purpose=Shared context for all agent decisions; immutable record ensures determinism.
// AI: invariants=BarNumber/Beat are 1-based; EnergyLevel/TensionLevel/MotifPresenceScore are 0.0-1.0.
// AI: deps=MusicConstants.eSectionType for section classification; Rng system for RngStreamKey.
// AI: change=Extend via inheritance for instrument-specific contexts (DrummerContext, GuitarContext, etc.).

using Music;

namespace Music.Generator.Agents.Common
{
    /// <summary>
    /// Base context provided to all musical operators for decision-making.
    /// Immutable record ensures deterministic behavior across runs.
    /// Instrument-specific agents extend this with additional fields.
    /// </summary>
    public record AgentContext
    {
        /// <summary>
        /// Current bar number (1-based).
        /// </summary>
        public required int BarNumber { get; init; }

        /// <summary>
        /// Current beat within the bar (1-based, can be fractional like 1.5 for eighth offbeat).
        /// </summary>
        public required decimal Beat { get; init; }

        /// <summary>
        /// Section type for the current bar (Intro, Verse, Chorus, etc.).
        /// </summary>
        public required MusicConstants.eSectionType SectionType { get; init; }

        /// <summary>
        /// Position within the current phrase (0.0 = phrase start, 1.0 = phrase end).
        /// Derived from bar position relative to phrase boundaries.
        /// </summary>
        public required double PhrasePosition { get; init; }

        /// <summary>
        /// Number of bars remaining until the current section ends.
        /// Used for fill window decisions and section-end behaviors.
        /// </summary>
        public required int BarsUntilSectionEnd { get; init; }

        /// <summary>
        /// Overall energy level for this moment (0.0 = minimal, 1.0 = maximum).
        /// Derived from arrangement intent (Stage 7 energy system).
        /// </summary>
        public required double EnergyLevel { get; init; }

        /// <summary>
        /// Harmonic/rhythmic tension level (0.0 = resolved, 1.0 = maximum tension).
        /// Influences operator choices for tension-building or release.
        /// </summary>
        public required double TensionLevel { get; init; }

        /// <summary>
        /// How busy the arrangement is at this point (0.0 = sparse, 1.0 = dense).
        /// Derived from MotifPresenceMap; used to avoid over-cluttering.
        /// </summary>
        public required double MotifPresenceScore { get; init; }

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
        public static AgentContext CreateMinimal(
            int barNumber = 1,
            MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse,
            int seed = 42)
        {
            return new AgentContext
            {
                BarNumber = barNumber,
                Beat = 1.0m,
                SectionType = sectionType,
                PhrasePosition = 0.0,
                BarsUntilSectionEnd = 4,
                EnergyLevel = 0.5,
                TensionLevel = 0.0,
                MotifPresenceScore = 0.0,
                Seed = seed,
                RngStreamKey = $"Test_{barNumber}"
            };
        }
    }
}
