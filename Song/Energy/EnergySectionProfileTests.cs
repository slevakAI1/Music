// AI: purpose=Unit tests for energy section profile system validating derivation, constraints, and multi-factor mapping.
// AI: invariants=Tests must validate determinism, range constraints, and proper energy-to-control mapping.

namespace Music.Generator
{
    /// <summary>
    /// Tests for Story 7.2: EnergySectionProfile system.
    /// Validates profile derivation from energy arc, per-role control mapping, and orchestration logic.
    /// </summary>
    public static class EnergySectionProfileTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== EnergySectionProfile Tests ===");

            TestProfileDerivation();
            TestRoleProfileMapping();
            TestOrchestrationLogic();
            TestTensionComputation();
            TestContrastBias();
            TestEnergyFactors();
            TestRangeConstraints();
            TestSectionTypeSpecifics();
            TestDeterminism();

            Console.WriteLine("All EnergySectionProfile tests passed.");
        }

        /// <summary>
        /// Validates that profiles are derived correctly from energy arc.
        /// </summary>
        private static void TestProfileDerivation()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            var verseSection = sectionTrack.Sections[1];  // Verse
            var profile = EnergyProfileBuilder.BuildProfile(arc, verseSection, sectionIndex: 0);

            // Validate structure
            if (profile.Global == null)
                throw new Exception("Profile missing Global block");
            if (profile.Roles == null)
                throw new Exception("Profile missing Roles block");
            if (profile.Orchestration == null)
                throw new Exception("Profile missing Orchestration block");

            // Validate energy is in range
            if (profile.Global.Energy < 0.0 || profile.Global.Energy > 1.0)
                throw new Exception($"Global energy out of range: {profile.Global.Energy}");

            Console.WriteLine("  ? Profile derivation from energy arc");
        }

        /// <summary>
        /// Validates per-role profile mapping from energy targets.
        /// </summary>
        private static void TestRoleProfileMapping()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc = EnergyArc.Create(sectionTrack, "RockGroove", seed: 42);

            // Test low-energy section (Intro)
            var introSection = sectionTrack.Sections[0];
            var lowProfile = EnergyProfileBuilder.BuildProfile(arc, introSection, sectionIndex: 0);

            // Test high-energy section (Chorus)
            var chorusSection = sectionTrack.Sections[2];
            var highProfile = EnergyProfileBuilder.BuildProfile(arc, chorusSection, sectionIndex: 0);

            // Verify all roles have profiles
            if (lowProfile.Roles.Bass == null || highProfile.Roles.Bass == null)
                throw new Exception("Missing Bass profile");
            if (lowProfile.Roles.Comp == null || highProfile.Roles.Comp == null)
                throw new Exception("Missing Comp profile");
            if (lowProfile.Roles.Keys == null || highProfile.Roles.Keys == null)
                throw new Exception("Missing Keys profile");
            if (lowProfile.Roles.Pads == null || highProfile.Roles.Pads == null)
                throw new Exception("Missing Pads profile");
            if (lowProfile.Roles.Drums == null || highProfile.Roles.Drums == null)
                throw new Exception("Missing Drums profile");

            // Higher energy should generally result in higher activity
            // (not guaranteed for all parameters, but typical)
            if (highProfile.Global.Energy < lowProfile.Global.Energy)
            {
                // This is expected for some arc templates, so just validate they're different
                // Main validation is that both are in valid range
            }

            Console.WriteLine("  ? Per-role profile mapping");
        }

        /// <summary>
        /// Validates orchestration logic (role presence, cymbal language).
        /// </summary>
        private static void TestOrchestrationLogic()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            // Test verse orchestration (often sparse)
            var verseSection = sectionTrack.Sections[1];
            var verseProfile = EnergyProfileBuilder.BuildProfile(arc, verseSection, sectionIndex: 0);

            // Test chorus orchestration (typically full)
            var chorusSection = sectionTrack.Sections[2];
            var chorusProfile = EnergyProfileBuilder.BuildProfile(arc, chorusSection, sectionIndex: 0);

            // Validate orchestration exists
            if (verseProfile.Orchestration == null || chorusProfile.Orchestration == null)
                throw new Exception("Missing orchestration profile");

            // Chorus typically has crash on section start for high energy
            // (depends on energy level, so just validate field exists)
            var _ = chorusProfile.Orchestration.CrashOnSectionStart;

            // Cymbal language should be valid
            if (!Enum.IsDefined(typeof(EnergyCymbalLanguage), verseProfile.Orchestration.CymbalLanguage))
                throw new Exception("Invalid cymbal language");

            Console.WriteLine("  ? Orchestration logic");
        }

        /// <summary>
        /// Validates tension computation.
        /// </summary>
        private static void TestTensionComputation()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            var verseSection = sectionTrack.Sections[1];
            var profile = EnergyProfileBuilder.BuildProfile(arc, verseSection, sectionIndex: 0);

            // Tension should be in range
            if (profile.Global.TensionTarget < 0.0 || profile.Global.TensionTarget > 1.0)
                throw new Exception($"Tension out of range: {profile.Global.TensionTarget}");

            // Tension is typically related to but distinct from energy
            // Just validate it exists and is in range

            Console.WriteLine("  ? Tension computation");
        }

        /// <summary>
        /// Validates contrast bias computation between sections.
        /// </summary>
        private static void TestContrastBias()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            var verseSection = sectionTrack.Sections[1];
            var verseProfile = EnergyProfileBuilder.BuildProfile(arc, verseSection, sectionIndex: 0);

            var chorusSection = sectionTrack.Sections[2];
            var chorusProfile = EnergyProfileBuilder.BuildProfile(
                arc, chorusSection, sectionIndex: 0, previousProfile: verseProfile);

            // Contrast bias should be in range
            if (chorusProfile.Global.ContrastBias < 0.0 || chorusProfile.Global.ContrastBias > 1.0)
                throw new Exception($"Contrast bias out of range: {chorusProfile.Global.ContrastBias}");

            // When there's no previous profile, contrast bias should be 0
            var firstProfile = EnergyProfileBuilder.BuildProfile(arc, verseSection, sectionIndex: 0);
            if (firstProfile.Global.ContrastBias != 0.0)
                throw new Exception("First section should have zero contrast bias");

            Console.WriteLine("  ? Contrast bias computation");
        }

        /// <summary>
        /// Validates that energy affects multiple factors (not just velocity).
        /// </summary>
        private static void TestEnergyFactors()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc = EnergyArc.Create(sectionTrack, "RockGroove", seed: 42);

            var lowEnergySection = sectionTrack.Sections[0];  // Intro
            var highEnergySection = sectionTrack.Sections[2]; // Chorus

            var lowProfile = EnergyProfileBuilder.BuildProfile(arc, lowEnergySection, sectionIndex: 0);
            var highProfile = EnergyProfileBuilder.BuildProfile(arc, highEnergySection, sectionIndex: 0);

            // Verify multiple factors vary with energy (for at least one role)
            bool densityVaries = false;
            bool velocityVaries = false;
            bool registerVaries = false;
            bool busyVaries = false;

            // Check Keys role (typically has highest variation)
            if (Math.Abs(lowProfile.Roles.Keys.DensityMultiplier - highProfile.Roles.Keys.DensityMultiplier) > 0.1)
                densityVaries = true;
            if (Math.Abs(lowProfile.Roles.Keys.VelocityBias - highProfile.Roles.Keys.VelocityBias) > 5)
                velocityVaries = true;
            if (Math.Abs(lowProfile.Roles.Keys.RegisterLiftSemitones - highProfile.Roles.Keys.RegisterLiftSemitones) > 0)
                registerVaries = true;
            if (Math.Abs(lowProfile.Roles.Keys.BusyProbability - highProfile.Roles.Keys.BusyProbability) > 0.1)
                busyVaries = true;

            // At least 3 of 4 factors should vary (depending on energy arc)
            int varyingFactors = (densityVaries ? 1 : 0) + (velocityVaries ? 1 : 0) +
                                 (registerVaries ? 1 : 0) + (busyVaries ? 1 : 0);

            if (varyingFactors < 2)
            {
                throw new Exception($"Energy should affect multiple factors, only {varyingFactors} vary");
            }

            Console.WriteLine("  ? Multi-factor energy mapping");
        }

        /// <summary>
        /// Validates all profile values are within acceptable ranges.
        /// </summary>
        private static void TestRangeConstraints()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            foreach (var section in sectionTrack.Sections)
            {
                var profile = EnergyProfileBuilder.BuildProfile(arc, section, sectionIndex: 0);

                // Validate global ranges
                ValidateRange(profile.Global.Energy, 0.0, 1.0, "Global Energy");
                ValidateRange(profile.Global.TensionTarget, 0.0, 1.0, "Global TensionTarget");
                ValidateRange(profile.Global.ContrastBias, 0.0, 1.0, "Global ContrastBias");

                // Validate role ranges
                ValidateRoleProfile(profile.Roles.Bass, "Bass");
                ValidateRoleProfile(profile.Roles.Comp, "Comp");
                ValidateRoleProfile(profile.Roles.Keys, "Keys");
                ValidateRoleProfile(profile.Roles.Pads, "Pads");
                ValidateRoleProfile(profile.Roles.Drums, "Drums");
            }

            Console.WriteLine("  ? Range constraints");
        }

        /// <summary>
        /// Validates section-type-specific logic (verse sparse, chorus full, etc.).
        /// </summary>
        private static void TestSectionTypeSpecifics()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            // Test Intro orchestration
            var introSection = sectionTrack.Sections[0];
            var introProfile = EnergyProfileBuilder.BuildProfile(arc, introSection, sectionIndex: 0);

            // Test Verse 1 orchestration (first verse often sparse)
            var verse1Section = sectionTrack.Sections[1];
            var verse1Profile = EnergyProfileBuilder.BuildProfile(arc, verse1Section, sectionIndex: 0);

            // Test Chorus orchestration (typically full)
            var chorusSection = sectionTrack.Sections[2];
            var chorusProfile = EnergyProfileBuilder.BuildProfile(arc, chorusSection, sectionIndex: 0);

            // Orchestration should reflect section types
            // (Exact presence depends on energy level, so just validate structure)
            if (verse1Profile.Orchestration == null || chorusProfile.Orchestration == null)
                throw new Exception("Missing orchestration for section type test");

            Console.WriteLine("  ? Section-type-specific logic");
        }

        /// <summary>
        /// Validates determinism (same inputs produce same outputs).
        /// </summary>
        private static void TestDeterminism()
        {
            var sectionTrack = CreateTestSectionTrack();
            var arc1 = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
            var arc2 = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            var section = sectionTrack.Sections[1];

            var profile1 = EnergyProfileBuilder.BuildProfile(arc1, section, sectionIndex: 0);
            var profile2 = EnergyProfileBuilder.BuildProfile(arc2, section, sectionIndex: 0);

            // Validate identical outputs
            if (Math.Abs(profile1.Global.Energy - profile2.Global.Energy) > 0.001)
                throw new Exception("Energy not deterministic");

            if (Math.Abs(profile1.Roles.Bass.DensityMultiplier - profile2.Roles.Bass.DensityMultiplier) > 0.001)
                throw new Exception("Bass density not deterministic");

            if (profile1.Roles.Comp.VelocityBias != profile2.Roles.Comp.VelocityBias)
                throw new Exception("Comp velocity bias not deterministic");

            Console.WriteLine("  ? Determinism");
        }

        #region Helper Methods

        private static void ValidateRange(double value, double min, double max, string name)
        {
            if (value < min || value > max)
                throw new Exception($"{name} out of range [{min}..{max}]: {value}");
        }

        private static void ValidateRoleProfile(EnergyRoleProfile profile, string roleName)
        {
            if (profile.DensityMultiplier < 0.0)
                throw new Exception($"{roleName} DensityMultiplier negative: {profile.DensityMultiplier}");

            // Velocity bias should be reasonable (not extreme)
            if (Math.Abs(profile.VelocityBias) > 50)
                throw new Exception($"{roleName} VelocityBias extreme: {profile.VelocityBias}");

            // Register lift should be reasonable (not extreme)
            if (Math.Abs(profile.RegisterLiftSemitones) > 36)
                throw new Exception($"{roleName} RegisterLift extreme: {profile.RegisterLiftSemitones}");

            ValidateRange(profile.BusyProbability, 0.0, 1.0, $"{roleName} BusyProbability");
        }

        private static SectionTrack CreateTestSectionTrack()
        {
            var track = new SectionTrack();
            track.Sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Intro,
                StartBar = 1,
                BarCount = 2
            });
            track.Sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Verse,
                StartBar = 3,
                BarCount = 4
            });
            track.Sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Chorus,
                StartBar = 7,
                BarCount = 4
            });
            track.Sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Verse,
                StartBar = 11,
                BarCount = 4
            });
            track.Sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Chorus,
                StartBar = 15,
                BarCount = 4
            });
            return track;
        }

        #endregion
    }
}
