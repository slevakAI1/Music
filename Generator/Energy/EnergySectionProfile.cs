// AI: purpose=Main energy profile converting section energy targets into per-role actionable controls.
// AI: invariants=Energy/TensionTarget [0..1]; ContrastBias [0..1]; all role profiles non-null; orchestration non-null.
// AI: deps=Consumed by role generators (Bass, Comp, Keys, Pads, Drums) to apply energy intent; derived from EnergyArc via EnergyProfileBuilder.

namespace Music.Generator
{
    /// <summary>
    /// Complete energy profile for a section, containing global targets and per-role controls.
    /// Derived from EnergyArc and section characteristics; consumed by role generators.
    /// This is the primary contract between energy planning (Story 7.1) and role rendering.
    /// </summary>
    public sealed class EnergySectionProfile
    {
        /// <summary>
        /// Global energy and tension targets for the section.
        /// </summary>
        public required EnergyGlobalTargets Global { get; init; }

        /// <summary>
        /// Per-role energy controls (density, velocity, register, busy probability).
        /// </summary>
        public required EnergyRoleProfiles Roles { get; init; }

        /// <summary>
        /// Orchestration controls (role presence, cymbal language).
        /// </summary>
        public required EnergyOrchestrationProfile Orchestration { get; init; }

        /// <summary>
        /// The section this profile applies to.
        /// </summary>
        public required Section Section { get; init; }

        /// <summary>
        /// 0-based index of this section instance among sections of the same type.
        /// Used for A/A'/B variation logic (Story 7.6).
        /// </summary>
        public int SectionIndex { get; init; }
    }

    /// <summary>
    /// Global energy and tension targets for a section.
    /// Provides high-level intent that role profiles interpret.
    /// </summary>
    public sealed class EnergyGlobalTargets
    {
        /// <summary>
        /// Target energy level [0..1] for the section.
        /// 0.0 = minimal energy; 1.0 = maximum energy.
        /// Drives multiple musical factors (dynamics, density, register, orchestration).
        /// </summary>
        public double Energy { get; init; }

        /// <summary>
        /// Target tension level [0..1] for the section.
        /// 0.0 = resolved/stable; 1.0 = maximum tension/anticipation.
        /// Influences phrase-end "pull" events, build-ups, dramatic pauses.
        /// </summary>
        public double TensionTarget { get; init; }

        /// <summary>
        /// Contrast bias [0..1] indicating how much this section should differ from the previous.
        /// 0.0 = minimal contrast (similar to previous).
        /// 1.0 = maximum contrast (deliberately different).
        /// Used for A/A'/B variation decisions.
        /// </summary>
        public double ContrastBias { get; init; }

        /// <summary>
        /// Creates global targets with specified energy.
        /// </summary>
        public static EnergyGlobalTargets WithEnergy(double energy)
        {
            return new EnergyGlobalTargets
            {
                Energy = energy,
                TensionTarget = 0.0,
                ContrastBias = 0.0
            };
        }

        /// <summary>
        /// Creates global targets with energy and tension.
        /// </summary>
        public static EnergyGlobalTargets WithEnergyAndTension(double energy, double tension)
        {
            return new EnergyGlobalTargets
            {
                Energy = energy,
                TensionTarget = tension,
                ContrastBias = 0.0
            };
        }
    }

    /// <summary>
    /// Container for per-role energy profiles.
    /// All profiles are required; use EnergyRoleProfile.Neutral() if a role is inactive.
    /// </summary>
    public sealed class EnergyRoleProfiles
    {
        /// <summary>Bass role profile.</summary>
        public required EnergyRoleProfile Bass { get; init; }

        /// <summary>Comp (guitar/comp) role profile.</summary>
        public required EnergyRoleProfile Comp { get; init; }

        /// <summary>Keys role profile.</summary>
        public required EnergyRoleProfile Keys { get; init; }

        /// <summary>Pads role profile.</summary>
        public required EnergyRoleProfile Pads { get; init; }

        /// <summary>Drums role profile.</summary>
        public required EnergyRoleProfile Drums { get; init; }

        /// <summary>
        /// Creates neutral profiles for all roles (baseline energy).
        /// </summary>
        public static EnergyRoleProfiles Neutral()
        {
            return new EnergyRoleProfiles
            {
                Bass = EnergyRoleProfile.Neutral(),
                Comp = EnergyRoleProfile.Neutral(),
                Keys = EnergyRoleProfile.Neutral(),
                Pads = EnergyRoleProfile.Neutral(),
                Drums = EnergyRoleProfile.Neutral()
            };
        }

        /// <summary>
        /// Creates low-energy profiles for all roles.
        /// </summary>
        public static EnergyRoleProfiles LowEnergy()
        {
            return new EnergyRoleProfiles
            {
                Bass = EnergyRoleProfile.LowEnergy(),
                Comp = EnergyRoleProfile.LowEnergy(),
                Keys = EnergyRoleProfile.LowEnergy(),
                Pads = EnergyRoleProfile.LowEnergy(),
                Drums = EnergyRoleProfile.LowEnergy()
            };
        }

        /// <summary>
        /// Creates high-energy profiles for all roles.
        /// </summary>
        public static EnergyRoleProfiles HighEnergy()
        {
            return new EnergyRoleProfiles
            {
                Bass = EnergyRoleProfile.HighEnergy(),
                Comp = EnergyRoleProfile.HighEnergy(),
                Keys = EnergyRoleProfile.HighEnergy(),
                Pads = EnergyRoleProfile.HighEnergy(),
                Drums = EnergyRoleProfile.HighEnergy()
            };
        }
    }
}
