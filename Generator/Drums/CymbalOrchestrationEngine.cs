// AI: purpose=Deterministic cymbal orchestration for Story 6.4; handles crash/ride placement at section starts, phrase peaks, and endings.
// AI: invariants=All cymbal placement is deterministic for (seed, sectionType, sectionIndex, bar); respects style conventions from groove name.
// AI: deps=Uses RandomHelpers for seeding, SectionTrack for detecting boundaries, returns cymbal hit descriptions.

namespace Music.Generator
{
    /// <summary>
    /// Generates deterministic, intentional cymbal orchestration for drum parts.
    /// Implements Story 6.4: cymbal language marks phrases/sections to sound "produced."
    /// </summary>
    internal static class CymbalOrchestrationEngine
    {
        /// <summary>
        /// Describes a cymbal hit with articulation information.
        /// </summary>
        public sealed class CymbalHit
        {
            public string Type { get; init; } = string.Empty;  // "crash1" | "crash2" | "ride" | "choke"
            public decimal OnsetBeat { get; init; }
            public int TimingOffsetTicks { get; init; } = 0;
            public bool IsAccent { get; init; } = false;       // Hit with extra emphasis
        }

        /// <summary>
        /// Style characteristics for cymbal usage mapped from groove name.
        /// </summary>
        private sealed class CymbalStyle
        {
            public bool UsesCrashOnSectionStart { get; init; } = true;
            public bool UsesRideInChorus { get; init; } = false;
            public bool UsesPhraseAccents { get; init; } = true;
            public int PhrasePeakInterval { get; init; } = 4;  // Bars between phrase peaks
            public bool SupportsChoke { get; init; } = false;  // Stop/choke hits for endings
        }

        /// <summary>
        /// Maps groove name to cymbal style characteristics.
        /// Based on genre-specific drumming conventions.
        /// </summary>
        private static CymbalStyle GetCymbalStyle(string grooveName)
        {
            string normalized = grooveName?.Trim() ?? "default";

            return normalized switch
            {
                // === ROCK / POP ROCK ===
                // Rock: crash on section starts, ride often in chorus, frequent phrase accents
                "PopRockBasic" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = true,
                    UsesRideInChorus = true,
                    UsesPhraseAccents = true,
                    PhrasePeakInterval = 4,
                    SupportsChoke = true
                },

                // === METAL ===
                // Metal: heavy crash usage, ride less common, china cymbals (use crash2), supports chokes
                "MetalDoubleKick" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = true,
                    UsesRideInChorus = false,
                    UsesPhraseAccents = true,
                    PhrasePeakInterval = 4,
                    SupportsChoke = true
                },

                // === FUNK ===
                // Funk: minimal crashes (breaks the pocket), prefers consistent hat/ride
                "FunkSyncopated" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = false,  // Funk avoids heavy crashes
                    UsesRideInChorus = false,
                    UsesPhraseAccents = false,
                    PhrasePeakInterval = 8,
                    SupportsChoke = false
                },

                // === ELECTRONIC / EDM / DANCE ===
                // EDM: predictable crash on section starts, no ride, regular phrase accents
                "DanceEDMFourOnFloor" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = true,
                    UsesRideInChorus = false,
                    UsesPhraseAccents = true,
                    PhrasePeakInterval = 8,
                    SupportsChoke = false
                },

                // === TRAP / HIP-HOP ===
                // Trap: minimal cymbal work, very sparse crashes
                "TrapModern" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = false,
                    UsesRideInChorus = false,
                    UsesPhraseAccents = false,
                    PhrasePeakInterval = 16,
                    SupportsChoke = false
                },

                "HipHopBoomBap" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = false,
                    UsesRideInChorus = false,
                    UsesPhraseAccents = false,
                    PhrasePeakInterval = 8,
                    SupportsChoke = false
                },

                "RapBasic" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = false,
                    UsesRideInChorus = false,
                    UsesPhraseAccents = false,
                    PhrasePeakInterval = 8,
                    SupportsChoke = false
                },

                // === LATIN / BOSSA NOVA ===
                // Bossa: ride cymbal is primary timekeeping, minimal crashes
                "BossaNovaBasic" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = false,
                    UsesRideInChorus = true,  // Ride is standard in bossa
                    UsesPhraseAccents = false,
                    PhrasePeakInterval = 8,
                    SupportsChoke = false
                },

                // === REGGAE / REGGAETON ===
                // Reggae: minimal cymbal work, avoids accents
                "ReggaeOneDrop" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = false,
                    UsesRideInChorus = false,
                    UsesPhraseAccents = false,
                    PhrasePeakInterval = 16,
                    SupportsChoke = false
                },

                "ReggaetonDembow" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = false,
                    UsesRideInChorus = false,
                    UsesPhraseAccents = false,
                    PhrasePeakInterval = 8,
                    SupportsChoke = false
                },

                // === COUNTRY ===
                // Country: crash on section starts, ride common, regular accents
                "CountryTrain" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = true,
                    UsesRideInChorus = true,
                    UsesPhraseAccents = true,
                    PhrasePeakInterval = 4,
                    SupportsChoke = false
                },

                // === JAZZ ===
                // Jazz: ride is primary timekeeping, minimal crashes, no predictable accents
                "JazzSwing" => new CymbalStyle
                {
                    UsesCrashOnSectionStart = false,
                    UsesRideInChorus = true,  // Ride is standard in jazz
                    UsesPhraseAccents = false,
                    PhrasePeakInterval = 8,
                    SupportsChoke = false
                },

                // === DEFAULT FALLBACK ===
                _ => new CymbalStyle
                {
                    UsesCrashOnSectionStart = true,
                    UsesRideInChorus = true,
                    UsesPhraseAccents = true,
                    PhrasePeakInterval = 4,
                    SupportsChoke = true
                }
            };
        }

        /// <summary>
        /// Determines if this bar should have a crash cymbal on downbeat based on section boundaries.
        /// Returns true if this is the first bar of a section (section start).
        /// </summary>
        public static bool ShouldCrashOnSectionStart(
            int bar,
            SectionTrack sectionTrack,
            string grooveName)
        {
            var style = GetCymbalStyle(grooveName);

            // Style must support section start crashes
            if (!style.UsesCrashOnSectionStart)
                return false;

            // Check if this is the first bar of a section
            if (sectionTrack.GetActiveSection(bar, out var currentSection) && currentSection != null)
            {
                return bar == currentSection.StartBar;
            }

            return false;
        }

        /// <summary>
        /// Determines if this bar should have a crash cymbal on a phrase peak.
        /// Phrase peaks occur at regular intervals (e.g., every 4 or 8 bars) within sections.
        /// </summary>
        public static bool ShouldCrashOnPhrasePeak(
            int bar,
            SectionTrack sectionTrack,
            string grooveName,
            int seed)
        {
            var style = GetCymbalStyle(grooveName);

            // Style must support phrase accents
            if (!style.UsesPhraseAccents)
                return false;

            // Check if we're in a section
            if (!sectionTrack.GetActiveSection(bar, out var currentSection) || currentSection == null)
                return false;

            // Calculate bar position within the section (1-based)
            int barInSection = bar - currentSection.StartBar + 1;

            // Crash on phrase peaks (every N bars, starting after the first bar)
            // Don't crash on the first bar (that's handled by ShouldCrashOnSectionStart)
            if (barInSection == 1)
                return false;

            // Deterministic check if this bar is a phrase peak
            bool isPhrasePeak = barInSection % style.PhrasePeakInterval == 1;

            // Add slight deterministic variation: not every phrase peak gets a crash
            if (isPhrasePeak)
            {
                var rng = RandomHelpers.CreateLocalRng(seed, $"{grooveName}_crash", bar, 0m);
                return rng.NextDouble() < 0.7;  // 70% probability
            }

            return false;
        }

        /// <summary>
        /// Determines if this section type should use ride cymbal instead of hi-hat.
        /// Used to select ride vs hat for timekeeping based on section energy.
        /// </summary>
        public static bool ShouldUseRideForSection(
            MusicConstants.eSectionType sectionType,
            string grooveName)
        {
            var style = GetCymbalStyle(grooveName);

            // Some styles always use ride (jazz, bossa)
            if (grooveName == "JazzSwing" || grooveName == "BossaNovaBasic")
                return true;

            // For styles that support ride in chorus, check section type
            if (style.UsesRideInChorus)
            {
                return sectionType == MusicConstants.eSectionType.Chorus ||
                       sectionType == MusicConstants.eSectionType.Bridge;
            }

            return false;
        }

        /// <summary>
        /// Determines if this bar should have a choke/stop hit for ending.
        /// Used for outros and final bars with supported styles.
        /// </summary>
        public static bool ShouldChokeOnEnding(
            int bar,
            int totalBars,
            SectionTrack sectionTrack,
            string grooveName)
        {
            var style = GetCymbalStyle(grooveName);

            // Style must support chokes
            if (!style.SupportsChoke)
                return false;

            // Check if this is the very last bar of the song
            if (bar == totalBars)
            {
                // Check if we're in an outro section
                if (sectionTrack.GetActiveSection(bar, out var currentSection) && currentSection != null)
                {
                    return currentSection.SectionType == MusicConstants.eSectionType.Outro;
                }
            }

            return false;
        }

        /// <summary>
        /// Generates cymbal hits for a specific bar based on orchestration rules.
        /// Returns empty list if no cymbals are needed for this bar.
        /// </summary>
        public static List<CymbalHit> GenerateCymbalHits(
            int bar,
            int totalBars,
            SectionTrack sectionTrack,
            MusicConstants.eSectionType sectionType,
            string grooveName,
            int seed)
        {
            var hits = new List<CymbalHit>();

            // Check for crash on section start
            if (ShouldCrashOnSectionStart(bar, sectionTrack, grooveName))
            {
                hits.Add(new CymbalHit
                {
                    Type = "crash1",
                    OnsetBeat = 1.0m,  // Downbeat
                    TimingOffsetTicks = 0,
                    IsAccent = true
                });
            }

            // Check for crash on phrase peak
            if (ShouldCrashOnPhrasePeak(bar, sectionTrack, grooveName, seed))
            {
                // Use crash2 for phrase peaks to vary the sound
                hits.Add(new CymbalHit
                {
                    Type = "crash2",
                    OnsetBeat = 1.0m,  // Downbeat
                    TimingOffsetTicks = 0,
                    IsAccent = true
                });
            }

            // Check for choke on ending
            if (ShouldChokeOnEnding(bar, totalBars, sectionTrack, grooveName))
            {
                // Choke happens on the last beat of the final bar
                hits.Add(new CymbalHit
                {
                    Type = "choke",
                    OnsetBeat = 4.0m,  // Last beat of the bar
                    TimingOffsetTicks = 0,
                    IsAccent = false
                });
            }

            return hits;
        }
    }
}
