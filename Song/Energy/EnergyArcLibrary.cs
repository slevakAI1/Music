// AI: purpose=Library of predefined energy arc templates for common song forms and styles.
// AI: invariants=Templates are immutable; energy values [0..1]; provide at least one generic fallback.
// AI: deps=Used by EnergyArc for deterministic template selection; templates drive SectionEnergyProfile.

namespace Music.Generator
{
    /// <summary>
    /// Library of predefined energy arc templates for various musical styles and forms.
    /// Provides templates that define typical energy progressions for Pop, Rock, EDM, and other styles.
    /// </summary>
    public static class EnergyArcLibrary
    {
        /// <summary>
        /// Gets all available arc templates for a given style category.
        /// </summary>
        public static List<EnergyArcTemplate> GetTemplatesForStyle(string styleCategory)
        {
            return styleCategory.ToLowerInvariant() switch
            {
                "pop" => GetPopTemplates(),
                "rock" => GetRockTemplates(),
                "edm" => GetEDMTemplates(),
                "jazz" => GetJazzTemplates(),
                "country" => GetCountryTemplates(),
                _ => GetGenericTemplates()
            };
        }

        /// <summary>
        /// Gets generic fallback templates suitable for any style.
        /// </summary>
        public static List<EnergyArcTemplate> GetGenericTemplates()
        {
            return new List<EnergyArcTemplate>
            {
                CreatePopStandard(),
                CreateRockBuild()
            };
        }

        #region Pop Templates

        private static List<EnergyArcTemplate> GetPopTemplates()
        {
            return new List<EnergyArcTemplate>
            {
                CreatePopStandard(),
                CreatePopBuildAndRelease(),
                CreatePopIntenseChorus()
            };
        }

        /// <summary>
        /// Standard Pop arc: moderate verse, high chorus, bridge contrast.
        /// Typical progression: Intro (low) -> Verse (moderate) -> Chorus (high) -> Bridge (moderate-high).
        /// </summary>
        private static EnergyArcTemplate CreatePopStandard()
        {
            return new EnergyArcTemplate
            {
                Name = "PopStandard",
                Description = "Standard pop energy arc with verse-chorus contrast and gradual build",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.3,
                    [MusicConstants.eSectionType.Verse] = 0.4,
                    [MusicConstants.eSectionType.Chorus] = 0.8,
                    [MusicConstants.eSectionType.Bridge] = 0.6,
                    [MusicConstants.eSectionType.Outro] = 0.5,
                    [MusicConstants.eSectionType.Solo] = 0.7,
                    [MusicConstants.eSectionType.Custom] = 0.5
                },

                SectionTargets = new Dictionary<(MusicConstants.eSectionType, int), EnergySectionTarget>
                {
                    // Verse 1: lower energy
                    [(MusicConstants.eSectionType.Verse, 0)] = EnergySectionTarget.Uniform(0.4),

                    // Verse 2: slightly higher energy (typical pop progression)
                    [(MusicConstants.eSectionType.Verse, 1)] = EnergySectionTarget.Uniform(0.5),

                    // Final Chorus: peak energy
                    [(MusicConstants.eSectionType.Chorus, 2)] = EnergySectionTarget.WithPhraseMicroArc(
                        baseEnergy: 0.85,
                        peakOffset: 0.1  // Crescendo at phrase peak
                    )
                }
            };
        }

        /// <summary>
        /// Pop arc with deliberate build and release dynamics.
        /// Features gradual energy increase across verses.
        /// </summary>
        private static EnergyArcTemplate CreatePopBuildAndRelease()
        {
            return new EnergyArcTemplate
            {
                Name = "PopBuildAndRelease",
                Description = "Gradual build across sections with controlled release in bridge",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.25,
                    [MusicConstants.eSectionType.Verse] = 0.4,
                    [MusicConstants.eSectionType.Chorus] = 0.75,
                    [MusicConstants.eSectionType.Bridge] = 0.5,  // Energy drop for contrast
                    [MusicConstants.eSectionType.Outro] = 0.3,
                    [MusicConstants.eSectionType.Solo] = 0.7,
                    [MusicConstants.eSectionType.Custom] = 0.5
                },

                SectionTargets = new Dictionary<(MusicConstants.eSectionType, int), EnergySectionTarget>
                {
                    [(MusicConstants.eSectionType.Verse, 0)] = EnergySectionTarget.Uniform(0.35),
                    [(MusicConstants.eSectionType.Verse, 1)] = EnergySectionTarget.Uniform(0.45),
                    [(MusicConstants.eSectionType.Chorus, 0)] = EnergySectionTarget.Uniform(0.75),
                    [(MusicConstants.eSectionType.Chorus, 1)] = EnergySectionTarget.Uniform(0.8),
                    [(MusicConstants.eSectionType.Chorus, 2)] = EnergySectionTarget.Uniform(0.9),  // Final chorus peak
                }
            };
        }

        /// <summary>
        /// Pop arc with very high contrast between verse and chorus.
        /// </summary>
        private static EnergyArcTemplate CreatePopIntenseChorus()
        {
            return new EnergyArcTemplate
            {
                Name = "PopIntenseChorus",
                Description = "Maximum verse-chorus contrast with explosive chorus energy",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.3,
                    [MusicConstants.eSectionType.Verse] = 0.35,
                    [MusicConstants.eSectionType.Chorus] = 0.9,
                    [MusicConstants.eSectionType.Bridge] = 0.6,
                    [MusicConstants.eSectionType.Outro] = 0.4,
                    [MusicConstants.eSectionType.Solo] = 0.8,
                    [MusicConstants.eSectionType.Custom] = 0.5
                }
            };
        }

        #endregion

        #region Rock Templates

        private static List<EnergyArcTemplate> GetRockTemplates()
        {
            return new List<EnergyArcTemplate>
            {
                CreateRockBuild(),
                CreateRockConsistentHigh(),
                CreateRockDynamicShift()
            };
        }

        /// <summary>
        /// Rock arc with steady build to high-energy climax.
        /// Typical for rock anthems and stadium rock.
        /// </summary>
        private static EnergyArcTemplate CreateRockBuild()
        {
            return new EnergyArcTemplate
            {
                Name = "RockBuild",
                Description = "Progressive build to high-energy peak, typical rock anthem structure",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.4,
                    [MusicConstants.eSectionType.Verse] = 0.5,
                    [MusicConstants.eSectionType.Chorus] = 0.8,
                    [MusicConstants.eSectionType.Bridge] = 0.75,
                    [MusicConstants.eSectionType.Outro] = 0.6,
                    [MusicConstants.eSectionType.Solo] = 0.85,
                    [MusicConstants.eSectionType.Custom] = 0.6
                },

                SectionTargets = new Dictionary<(MusicConstants.eSectionType, int), EnergySectionTarget>
                {
                    [(MusicConstants.eSectionType.Verse, 0)] = EnergySectionTarget.Uniform(0.5),
                    [(MusicConstants.eSectionType.Verse, 1)] = EnergySectionTarget.Uniform(0.6),
                    [(MusicConstants.eSectionType.Chorus, 0)] = EnergySectionTarget.Uniform(0.8),
                    [(MusicConstants.eSectionType.Chorus, 1)] = EnergySectionTarget.Uniform(0.85),
                    [(MusicConstants.eSectionType.Chorus, 2)] = EnergySectionTarget.Uniform(0.95),  // Climax
                }
            };
        }

        /// <summary>
        /// Rock arc maintaining consistently high energy throughout.
        /// Typical for punk, hard rock, and high-energy rock.
        /// </summary>
        private static EnergyArcTemplate CreateRockConsistentHigh()
        {
            return new EnergyArcTemplate
            {
                Name = "RockConsistentHigh",
                Description = "Sustained high energy throughout, minimal dynamic contrast",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.7,
                    [MusicConstants.eSectionType.Verse] = 0.75,
                    [MusicConstants.eSectionType.Chorus] = 0.85,
                    [MusicConstants.eSectionType.Bridge] = 0.8,
                    [MusicConstants.eSectionType.Outro] = 0.75,
                    [MusicConstants.eSectionType.Solo] = 0.9,
                    [MusicConstants.eSectionType.Custom] = 0.75
                }
            };
        }

        /// <summary>
        /// Rock arc with dramatic energy shifts and dynamic range.
        /// Typical for progressive rock and dynamic rock styles.
        /// </summary>
        private static EnergyArcTemplate CreateRockDynamicShift()
        {
            return new EnergyArcTemplate
            {
                Name = "RockDynamicShift",
                Description = "Wide dynamic range with dramatic energy shifts between sections",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.3,
                    [MusicConstants.eSectionType.Verse] = 0.4,
                    [MusicConstants.eSectionType.Chorus] = 0.85,
                    [MusicConstants.eSectionType.Bridge] = 0.35,  // Drop for contrast
                    [MusicConstants.eSectionType.Outro] = 0.5,
                    [MusicConstants.eSectionType.Solo] = 0.9,
                    [MusicConstants.eSectionType.Custom] = 0.5
                }
            };
        }

        #endregion

        #region EDM Templates

        private static List<EnergyArcTemplate> GetEDMTemplates()
        {
            return new List<EnergyArcTemplate>
            {
                CreateEDMBuildDrop(),
                CreateEDMProgressive(),
                CreateEDMBreakdownBuild()
            };
        }

        /// <summary>
        /// EDM arc with classic build-up and drop structure.
        /// Low intro, build through verse, explosive drop at chorus.
        /// </summary>
        private static EnergyArcTemplate CreateEDMBuildDrop()
        {
            return new EnergyArcTemplate
            {
                Name = "EDMBuildDrop",
                Description = "Classic EDM build-up and drop: low intro/verse, explosive chorus drop",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.2,
                    [MusicConstants.eSectionType.Verse] = 0.3,   // Build-up
                    [MusicConstants.eSectionType.Chorus] = 0.95, // Drop
                    [MusicConstants.eSectionType.Bridge] = 0.25, // Breakdown
                    [MusicConstants.eSectionType.Outro] = 0.3,
                    [MusicConstants.eSectionType.Solo] = 0.9,
                    [MusicConstants.eSectionType.Custom] = 0.5
                },

                SectionTargets = new Dictionary<(MusicConstants.eSectionType, int), EnergySectionTarget>
                {
                    // Verse as build-up with phrase-level energy increase
                    [(MusicConstants.eSectionType.Verse, 0)] = EnergySectionTarget.WithPhraseMicroArc(
                        baseEnergy: 0.3,
                        startOffset: 0.0,
                        peakOffset: 0.2  // Build at phrase peak
                    ),

                    // Second verse builds higher
                    [(MusicConstants.eSectionType.Verse, 1)] = EnergySectionTarget.WithPhraseMicroArc(
                        baseEnergy: 0.4,
                        peakOffset: 0.25
                    ),

                    // Final chorus: maximum energy
                    [(MusicConstants.eSectionType.Chorus, 2)] = EnergySectionTarget.Uniform(1.0)
                }
            };
        }

        /// <summary>
        /// Progressive EDM arc with gradual energy increase and sustained high energy.
        /// </summary>
        private static EnergyArcTemplate CreateEDMProgressive()
        {
            return new EnergyArcTemplate
            {
                Name = "EDMProgressive",
                Description = "Gradual progressive build maintaining high energy in latter sections",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.4,
                    [MusicConstants.eSectionType.Verse] = 0.5,
                    [MusicConstants.eSectionType.Chorus] = 0.85,
                    [MusicConstants.eSectionType.Bridge] = 0.7,
                    [MusicConstants.eSectionType.Outro] = 0.6,
                    [MusicConstants.eSectionType.Solo] = 0.9,
                    [MusicConstants.eSectionType.Custom] = 0.6
                }
            };
        }

        /// <summary>
        /// EDM arc with breakdown-build-drop cycles.
        /// </summary>
        private static EnergyArcTemplate CreateEDMBreakdownBuild()
        {
            return new EnergyArcTemplate
            {
                Name = "EDMBreakdownBuild",
                Description = "Breakdown-build-drop pattern with extreme contrast",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.3,
                    [MusicConstants.eSectionType.Verse] = 0.35,  // Breakdown
                    [MusicConstants.eSectionType.Chorus] = 0.9,  // Drop
                    [MusicConstants.eSectionType.Bridge] = 0.2,  // Deep breakdown
                    [MusicConstants.eSectionType.Outro] = 0.25,
                    [MusicConstants.eSectionType.Solo] = 0.85,
                    [MusicConstants.eSectionType.Custom] = 0.5
                }
            };
        }

        #endregion

        #region Jazz Templates

        private static List<EnergyArcTemplate> GetJazzTemplates()
        {
            return new List<EnergyArcTemplate>
            {
                CreateJazzModerate(),
                CreateJazzDynamic()
            };
        }

        /// <summary>
        /// Jazz arc with moderate energy and subtle dynamics.
        /// Typical for jazz standards and bossa nova.
        /// </summary>
        private static EnergyArcTemplate CreateJazzModerate()
        {
            return new EnergyArcTemplate
            {
                Name = "JazzModerate",
                Description = "Moderate energy with subtle dynamics, typical for jazz standards",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.4,
                    [MusicConstants.eSectionType.Verse] = 0.5,
                    [MusicConstants.eSectionType.Chorus] = 0.6,
                    [MusicConstants.eSectionType.Bridge] = 0.55,
                    [MusicConstants.eSectionType.Outro] = 0.45,
                    [MusicConstants.eSectionType.Solo] = 0.7,
                    [MusicConstants.eSectionType.Custom] = 0.5
                }
            };
        }

        /// <summary>
        /// Jazz arc with wider dynamic range for more expressive playing.
        /// </summary>
        private static EnergyArcTemplate CreateJazzDynamic()
        {
            return new EnergyArcTemplate
            {
                Name = "JazzDynamic",
                Description = "Wide dynamic range for expressive jazz performance",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.3,
                    [MusicConstants.eSectionType.Verse] = 0.45,
                    [MusicConstants.eSectionType.Chorus] = 0.7,
                    [MusicConstants.eSectionType.Bridge] = 0.5,
                    [MusicConstants.eSectionType.Outro] = 0.35,
                    [MusicConstants.eSectionType.Solo] = 0.75,
                    [MusicConstants.eSectionType.Custom] = 0.5
                }
            };
        }

        #endregion

        #region Country Templates

        private static List<EnergyArcTemplate> GetCountryTemplates()
        {
            return new List<EnergyArcTemplate>
            {
                CreateCountryTraditional()
            };
        }

        /// <summary>
        /// Traditional country arc with moderate energy and storytelling dynamics.
        /// </summary>
        private static EnergyArcTemplate CreateCountryTraditional()
        {
            return new EnergyArcTemplate
            {
                Name = "CountryTraditional",
                Description = "Traditional country energy with moderate dynamics and storytelling feel",

                DefaultEnergiesBySectionType = new Dictionary<MusicConstants.eSectionType, double>
                {
                    [MusicConstants.eSectionType.Intro] = 0.4,
                    [MusicConstants.eSectionType.Verse] = 0.5,
                    [MusicConstants.eSectionType.Chorus] = 0.7,
                    [MusicConstants.eSectionType.Bridge] = 0.6,
                    [MusicConstants.eSectionType.Outro] = 0.45,
                    [MusicConstants.eSectionType.Solo] = 0.65,
                    [MusicConstants.eSectionType.Custom] = 0.5
                }
            };
        }

        #endregion
    }
}
