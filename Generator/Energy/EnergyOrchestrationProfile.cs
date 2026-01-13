// AI: purpose=Orchestration controls defining which roles are present/featured and cymbal language preferences.
// AI: invariants=Role presence flags default to true; cymbal preferences are hints, not mandates.
// AI: deps=Used by EnergySectionProfile; consumed by Generator to control role inclusion and DrumTrackGenerator for cymbal selection.

namespace Music.Generator
{
    /// <summary>
    /// Orchestration profile controlling which roles are present/featured and cymbal language.
    /// Enables dynamic arrangement changes across sections (e.g., pads absent in verse, present in chorus).
    /// </summary>
    public sealed class EnergyOrchestrationProfile
    {
        /// <summary>
        /// Whether the Bass role is present/active in this section.
        /// </summary>
        public bool BassPresent { get; init; } = true;

        /// <summary>
        /// Whether the Comp (guitar/comp) role is present/active in this section.
        /// </summary>
        public bool CompPresent { get; init; } = true;

        /// <summary>
        /// Whether the Keys role is present/active in this section.
        /// </summary>
        public bool KeysPresent { get; init; } = true;

        /// <summary>
        /// Whether the Pads role is present/active in this section.
        /// </summary>
        public bool PadsPresent { get; init; } = true;

        /// <summary>
        /// Whether the Drums role is present/active in this section.
        /// </summary>
        public bool DrumsPresent { get; init; } = true;

        /// <summary>
        /// Cymbal language preference for this section.
        /// Guides drum orchestration (crash placement, ride vs hat selection).
        /// </summary>
        public EnergyCymbalLanguage CymbalLanguage { get; init; } = EnergyCymbalLanguage.Standard;

        /// <summary>
        /// Whether to place a crash cymbal at the start of this section.
        /// Typical for chorus starts, high-energy transitions.
        /// </summary>
        public bool CrashOnSectionStart { get; init; } = false;

        /// <summary>
        /// Whether to prefer ride cymbal over hi-hat for this section.
        /// Typical for higher-energy sections (chorus, bridge).
        /// </summary>
        public bool PreferRideOverHat { get; init; } = false;

        /// <summary>
        /// Creates a full orchestration profile (all roles present).
        /// </summary>
        public static EnergyOrchestrationProfile Full()
        {
            return new EnergyOrchestrationProfile
            {
                BassPresent = true,
                CompPresent = true,
                KeysPresent = true,
                PadsPresent = true,
                DrumsPresent = true,
                CymbalLanguage = EnergyCymbalLanguage.Standard,
                CrashOnSectionStart = false,
                PreferRideOverHat = false
            };
        }

        /// <summary>
        /// Creates a sparse orchestration profile (reduced layering).
        /// Typical for verses, intros.
        /// </summary>
        public static EnergyOrchestrationProfile Sparse()
        {
            return new EnergyOrchestrationProfile
            {
                BassPresent = true,
                CompPresent = true,
                KeysPresent = false,  // Keys often absent in sparse sections
                PadsPresent = false,  // Pads often absent in sparse sections
                DrumsPresent = true,
                CymbalLanguage = EnergyCymbalLanguage.Minimal,
                CrashOnSectionStart = false,
                PreferRideOverHat = false
            };
        }

        /// <summary>
        /// Creates a high-energy orchestration profile (full layers + crashes).
        /// Typical for choruses, climaxes.
        /// </summary>
        public static EnergyOrchestrationProfile HighEnergy()
        {
            return new EnergyOrchestrationProfile
            {
                BassPresent = true,
                CompPresent = true,
                KeysPresent = true,
                PadsPresent = true,
                DrumsPresent = true,
                CymbalLanguage = EnergyCymbalLanguage.Intense,
                CrashOnSectionStart = true,
                PreferRideOverHat = true
            };
        }
    }

    /// <summary>
    /// Cymbal language preference guiding drum orchestration.
    /// Hints for crash placement frequency and ride vs hat selection.
    /// </summary>
    public enum EnergyCymbalLanguage
    {
        /// <summary>
        /// Minimal cymbal use (primarily closed hi-hat).
        /// </summary>
        Minimal = 0,

        /// <summary>
        /// Standard cymbal orchestration (balanced crashes/hats).
        /// </summary>
        Standard = 1,

        /// <summary>
        /// Intense cymbal orchestration (frequent crashes, ride preference).
        /// </summary>
        Intense = 2
    }
}
