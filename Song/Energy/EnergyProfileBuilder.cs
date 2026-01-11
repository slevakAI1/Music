// AI: purpose=Derives EnergySectionProfile from EnergyArc and section characteristics.
// AI: invariants=Deterministic for same inputs; all output values within valid ranges; respects style-specific mappings.
// AI: deps=Consumes EnergyArc, Section, SectionType; produces EnergySectionProfile consumed by role generators.

namespace Music.Generator
{
    /// <summary>
    /// Builds EnergySectionProfile instances from EnergyArc and section characteristics.
    /// Converts high-level energy targets into concrete per-role controls.
    /// All derivations are deterministic and style-aware.
    /// </summary>
    public static class EnergyProfileBuilder
    {
        /// <summary>
        /// Builds a complete energy profile for a section.
        /// </summary>
        /// <param name="energyArc">The song's energy arc.</param>
        /// <param name="section">The section to build profile for.</param>
        /// <param name="sectionIndex">0-based index of this section among same-type sections.</param>
        /// <param name="previousProfile">Optional previous section profile for contrast calculation.</param>
        public static EnergySectionProfile BuildProfile(
            EnergyArc energyArc,
            Section section,
            int sectionIndex,
            EnergySectionProfile? previousProfile = null)
        {
            ArgumentNullException.ThrowIfNull(energyArc);
            ArgumentNullException.ThrowIfNull(section);

            // Get energy target from arc
            var target = energyArc.GetTargetForSection(section, sectionIndex);

            // Build global targets
            var globalTargets = BuildGlobalTargets(target, section, previousProfile);

            // Build per-role profiles
            var roleProfiles = BuildRoleProfiles(
                globalTargets.Energy,
                section.SectionType,
                energyArc.GrooveName);

            // Build orchestration profile
            var orchestration = BuildOrchestrationProfile(
                globalTargets.Energy,
                section.SectionType,
                sectionIndex);

            return new EnergySectionProfile
            {
                Global = globalTargets,
                Roles = roleProfiles,
                Orchestration = orchestration,
                Section = section,
                SectionIndex = sectionIndex
            };
        }

        /// <summary>
        /// Builds global energy and tension targets.
        /// </summary>
        private static EnergyGlobalTargets BuildGlobalTargets(
            EnergySectionTarget target,
            Section section,
            EnergySectionProfile? previousProfile)
        {
            double energy = target.Energy;

            // Compute tension target (future enhancement: more sophisticated tension modeling)
            double tensionTarget = ComputeTensionTarget(target.Energy, section.SectionType);

            // Compute contrast bias relative to previous section
            double contrastBias = previousProfile != null
                ? ComputeContrastBias(energy, previousProfile.Global.Energy)
                : 0.0;

            return new EnergyGlobalTargets
            {
                Energy = energy,
                TensionTarget = tensionTarget,
                ContrastBias = contrastBias
            };
        }

        /// <summary>
        /// Builds per-role energy profiles based on global energy and section characteristics.
        /// </summary>
        private static EnergyRoleProfiles BuildRoleProfiles(
            double energy,
            MusicConstants.eSectionType sectionType,
            string grooveName)
        {
            // Map energy [0..1] to role-specific parameters
            // Energy factors: dynamics (velocity), density, register, busy probability

            return new EnergyRoleProfiles
            {
                Bass = BuildBassProfile(energy, sectionType),
                Comp = BuildCompProfile(energy, sectionType),
                Keys = BuildKeysProfile(energy, sectionType),
                Pads = BuildPadsProfile(energy, sectionType),
                Drums = BuildDrumsProfile(energy, sectionType, grooveName)
            };
        }

        /// <summary>
        /// Builds bass role profile from energy target.
        /// </summary>
        private static EnergyRoleProfile BuildBassProfile(
            double energy,
            MusicConstants.eSectionType sectionType)
        {
            // Bass: lower density variation, velocity bias, moderate busy probability
            // Register lift limited to avoid going too high
            return new EnergyRoleProfile
            {
                DensityMultiplier = MapEnergyToDensity(energy, minMult: 0.8, maxMult: 1.3),
                VelocityBias = MapEnergyToVelocityBias(energy, minBias: -15, maxBias: 15),
                RegisterLiftSemitones = 0,  // Bass stays in low register
                BusyProbability = MapEnergyToBusy(energy, minBusy: 0.2, maxBusy: 0.7)
            };
        }

        /// <summary>
        /// Builds comp role profile from energy target.
        /// </summary>
        private static EnergyRoleProfile BuildCompProfile(
            double energy,
            MusicConstants.eSectionType sectionType)
        {
            // Comp: moderate density/register variation, high velocity sensitivity
            return new EnergyRoleProfile
            {
                DensityMultiplier = MapEnergyToDensity(energy, minMult: 0.6, maxMult: 1.5),
                VelocityBias = MapEnergyToVelocityBias(energy, minBias: -20, maxBias: 20),
                RegisterLiftSemitones = MapEnergyToRegisterLift(energy, minLift: 0, maxLift: 12),
                BusyProbability = MapEnergyToBusy(energy, minBusy: 0.3, maxBusy: 0.8)
            };
        }

        /// <summary>
        /// Builds keys role profile from energy target.
        /// </summary>
        private static EnergyRoleProfile BuildKeysProfile(
            double energy,
            MusicConstants.eSectionType sectionType)
        {
            // Keys: high density/register variation for dramatic contrast
            return new EnergyRoleProfile
            {
                DensityMultiplier = MapEnergyToDensity(energy, minMult: 0.5, maxMult: 1.6),
                VelocityBias = MapEnergyToVelocityBias(energy, minBias: -20, maxBias: 20),
                RegisterLiftSemitones = MapEnergyToRegisterLift(energy, minLift: -12, maxLift: 24),
                BusyProbability = MapEnergyToBusy(energy, minBusy: 0.2, maxBusy: 0.7)
            };
        }

        /// <summary>
        /// Builds pads role profile from energy target.
        /// </summary>
        private static EnergyRoleProfile BuildPadsProfile(
            double energy,
            MusicConstants.eSectionType sectionType)
        {
            // Pads: sustained role, lower density variation, moderate register lift
            return new EnergyRoleProfile
            {
                DensityMultiplier = MapEnergyToDensity(energy, minMult: 0.7, maxMult: 1.4),
                VelocityBias = MapEnergyToVelocityBias(energy, minBias: -15, maxBias: 15),
                RegisterLiftSemitones = MapEnergyToRegisterLift(energy, minLift: 0, maxLift: 12),
                BusyProbability = MapEnergyToBusy(energy, minBusy: 0.1, maxBusy: 0.5)
            };
        }

        /// <summary>
        /// Builds drums role profile from energy target.
        /// </summary>
        private static EnergyRoleProfile BuildDrumsProfile(
            double energy,
            MusicConstants.eSectionType sectionType,
            string grooveName)
        {
            // Drums: high density/busy sensitivity, no register lift
            return new EnergyRoleProfile
            {
                DensityMultiplier = MapEnergyToDensity(energy, minMult: 0.7, maxMult: 1.6),
                VelocityBias = MapEnergyToVelocityBias(energy, minBias: -15, maxBias: 20),
                RegisterLiftSemitones = 0,  // Drums don't use register lift
                BusyProbability = MapEnergyToBusy(energy, minBusy: 0.2, maxBusy: 0.9)
            };
        }

        /// <summary>
        /// Builds orchestration profile from energy and section characteristics.
        /// </summary>
        private static EnergyOrchestrationProfile BuildOrchestrationProfile(
            double energy,
            MusicConstants.eSectionType sectionType,
            int sectionIndex)
        {
            System.Diagnostics.Debug.WriteLine($"[EnergyOrchestration] Building orchestration: SectionType={sectionType}, SectionIndex={sectionIndex}, Energy={energy:F2}");
            
            // Default: all roles present
            bool bassPresent = true;
            bool compPresent = true;
            bool keysPresent = true;
            bool padsPresent = true;
            bool drumsPresent = true;

            // Section-type-specific orchestration decisions
            switch (sectionType)
            {
                case MusicConstants.eSectionType.Intro:
                    // Intro: often sparse, build gradually
                    padsPresent = energy > 0.3;
                    keysPresent = energy > 0.2;  // Lowered from 0.4 to 0.2
                    System.Diagnostics.Debug.WriteLine($"[EnergyOrchestration] Intro section: keysPresent={keysPresent} (energy > 0.2 = {energy > 0.2})");
                    break;

                case MusicConstants.eSectionType.Verse:
                    // Verse 1 often sparser than Verse 2+
                    if (sectionIndex == 0)
                    {
                        padsPresent = energy > 0.4;
                        keysPresent = energy > 0.3;  // Lowered from 0.5 to 0.3
                        System.Diagnostics.Debug.WriteLine($"[EnergyOrchestration] Verse 1 section: keysPresent={keysPresent} (energy > 0.3 = {energy > 0.3})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnergyOrchestration] Verse {sectionIndex + 1} section: keysPresent={keysPresent} (default=true)");
                    }
                    break;

                case MusicConstants.eSectionType.Chorus:
                    // Chorus: typically full arrangement
                    // All roles present by default
                    System.Diagnostics.Debug.WriteLine($"[EnergyOrchestration] Chorus section: keysPresent={keysPresent} (default=true)");
                    break;

                case MusicConstants.eSectionType.Bridge:
                    // Bridge: can vary, but typically full
                    System.Diagnostics.Debug.WriteLine($"[EnergyOrchestration] Bridge section: keysPresent={keysPresent} (default=true)");
                    break;

                case MusicConstants.eSectionType.Outro:
                    // Outro: often winds down
                    padsPresent = energy > 0.3;
                    System.Diagnostics.Debug.WriteLine($"[EnergyOrchestration] Outro section: keysPresent={keysPresent} (default=true)");
                    break;
            }

            // Cymbal language based on energy
            var cymbalLanguage = energy switch
            {
                < 0.4 => EnergyCymbalLanguage.Minimal,
                < 0.7 => EnergyCymbalLanguage.Standard,
                _ => EnergyCymbalLanguage.Intense
            };

            // Crash on section start for high-energy sections
            bool crashOnStart = sectionType == MusicConstants.eSectionType.Chorus && energy > 0.6;

            // Prefer ride for high-energy sections
            bool preferRide = energy > 0.7;

            return new EnergyOrchestrationProfile
            {
                BassPresent = bassPresent,
                CompPresent = compPresent,
                KeysPresent = keysPresent,
                PadsPresent = padsPresent,
                DrumsPresent = drumsPresent,
                CymbalLanguage = cymbalLanguage,
                CrashOnSectionStart = crashOnStart,
                PreferRideOverHat = preferRide
            };
        }

        #region Mapping Functions

        /// <summary>
        /// Maps energy [0..1] to density multiplier.
        /// </summary>
        private static double MapEnergyToDensity(double energy, double minMult, double maxMult)
        {
            return minMult + (energy * (maxMult - minMult));
        }

        /// <summary>
        /// Maps energy [0..1] to velocity bias.
        /// </summary>
        private static int MapEnergyToVelocityBias(double energy, int minBias, int maxBias)
        {
            return (int)Math.Round(minBias + (energy * (maxBias - minBias)));
        }

        /// <summary>
        /// Maps energy [0..1] to register lift in semitones.
        /// </summary>
        private static int MapEnergyToRegisterLift(double energy, int minLift, int maxLift)
        {
            return (int)Math.Round(minLift + (energy * (maxLift - minLift)));
        }

        /// <summary>
        /// Maps energy [0..1] to busy probability.
        /// </summary>
        private static double MapEnergyToBusy(double energy, double minBusy, double maxBusy)
        {
            return minBusy + (energy * (maxBusy - minBusy));
        }

        /// <summary>
        /// Computes tension target from energy and section type.
        /// Tension is related to but distinct from energy.
        /// </summary>
        private static double ComputeTensionTarget(double energy, MusicConstants.eSectionType sectionType)
        {
            // For now, tension roughly tracks energy with section-specific offsets
            // Future: more sophisticated tension modeling (Story 7.5)
            double baseTension = energy * 0.5;  // Tension typically lower than energy

            // Section-type-specific tension adjustments
            return sectionType switch
            {
                MusicConstants.eSectionType.Verse => baseTension,
                MusicConstants.eSectionType.Chorus => baseTension * 0.8,  // Chorus resolves tension
                MusicConstants.eSectionType.Bridge => baseTension * 1.3,  // Bridge builds tension
                MusicConstants.eSectionType.Intro => baseTension * 0.7,
                MusicConstants.eSectionType.Outro => baseTension * 0.5,  // Outro resolves
                _ => baseTension
            };
        }

        /// <summary>
        /// Computes contrast bias relative to previous section energy.
        /// </summary>
        private static double ComputeContrastBias(double currentEnergy, double previousEnergy)
        {
            // Contrast bias is the absolute energy difference
            double difference = Math.Abs(currentEnergy - previousEnergy);

            // Clamp to [0..1]
            return Math.Clamp(difference, 0.0, 1.0);
        }

        #endregion
    }
}
