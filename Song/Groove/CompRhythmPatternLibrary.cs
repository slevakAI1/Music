// AI: purpose=Static library of comp rhythm patterns keyed by groove preset name and section type.
// AI: invariants=Pattern selection is deterministic by (grooveName, sectionType, barIndex); RNG only for tie-break.
// AI: deps=Used by Generator.GenerateGuitarTrack; patterns must align with GroovePresets CompOnsets structure.
// AI: change=When adding patterns, ensure IncludedOnsetIndices are valid for the target groove's CompOnsets count.

namespace Music.Generator
{
    /// <summary>
    /// Provides deterministic comp rhythm pattern selection based on groove, section type, and bar position.
    /// </summary>
    public static class CompRhythmPatternLibrary
    {
        // AI: GetPattern: deterministic selection of comp rhythm pattern subset.
        // AI: behavior=Uses (grooveName, sectionType, barIndex) for deterministic selection; falls back to default if no match.
        public static CompRhythmPattern GetPattern(string grooveName, MusicConstants.eSectionType sectionType, int barIndex)
        {
            // Deterministic pattern selection based on section and bar
            var key = (grooveName, sectionType);

            // Select pattern based on bar index modulo pattern count for variation
            var patterns = GetPatternsForKey(key);
            int patternIndex = (barIndex - 1) % patterns.Count;
            
            return patterns[patternIndex];
        }

        // AI: GetPatternsForKey: returns ordered list of patterns for given (groove, section) combination.
        // AI: behavior=Pattern order determines bar-to-bar variation; keep patterns musically coherent within a list.
        private static IReadOnlyList<CompRhythmPattern> GetPatternsForKey((string grooveName, MusicConstants.eSectionType sectionType) key)
        {
            // Match on groove name and section type
            return key switch
            {
                // BossaNovaBasic patterns - typical: offbeat emphasis
                ("BossaNovaBasic", MusicConstants.eSectionType.Verse) => BossaNovaVersePatterns,
                ("BossaNovaBasic", MusicConstants.eSectionType.Chorus) => BossaNovaChorusPatterns,
                ("BossaNovaBasic", _) => BossaNovaDefaultPatterns,

                // CountryTrain patterns - typical: backbeat emphasis
                ("CountryTrain", MusicConstants.eSectionType.Verse) => CountryTrainVersePatterns,
                ("CountryTrain", MusicConstants.eSectionType.Chorus) => CountryTrainChorusPatterns,
                ("CountryTrain", _) => CountryTrainDefaultPatterns,

                // DanceEDMFourOnFloor patterns - typical: syncopated hits
                ("DanceEDMFourOnFloor", MusicConstants.eSectionType.Verse) => DanceEDMVersePatterns,
                ("DanceEDMFourOnFloor", MusicConstants.eSectionType.Chorus) => DanceEDMChorusPatterns,
                ("DanceEDMFourOnFloor", _) => DanceEDMDefaultPatterns,

                // FunkSyncopated patterns - typical: heavy syncopation
                ("FunkSyncopated", MusicConstants.eSectionType.Verse) => FunkVersePatterns,
                ("FunkSyncopated", MusicConstants.eSectionType.Chorus) => FunkChorusPatterns,
                ("FunkSyncopated", _) => FunkDefaultPatterns,

                // Default fallback: use all onsets (full pattern)
                _ => DefaultFullPatterns
            };
        }

        // AI: Pattern definitions below - indices reference CompOnsets from corresponding GroovePreset.
        // AI: BossaNovaBasic CompOnsets: { 1.5m, 2.5m, 3.5m, 4.5m } (4 onsets, offbeats)

        private static readonly IReadOnlyList<CompRhythmPattern> BossaNovaVersePatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "BossaNova_Verse_Sparse",
                IncludedOnsetIndices = new[] { 0, 2 },  // 1.5, 3.5
                Description = "Sparse offbeat hits"
            },
            new CompRhythmPattern
            {
                Name = "BossaNova_Verse_Standard",
                IncludedOnsetIndices = new[] { 0, 1, 2 },  // 1.5, 2.5, 3.5
                Description = "Standard three-hit pattern"
            }
        };

        private static readonly IReadOnlyList<CompRhythmPattern> BossaNovaChorusPatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "BossaNova_Chorus_Full",
                IncludedOnsetIndices = new[] { 0, 1, 2, 3 },  // All offbeats
                Description = "Full offbeat pattern"
            },
            new CompRhythmPattern
            {
                Name = "BossaNova_Chorus_Dense",
                IncludedOnsetIndices = new[] { 0, 1, 2, 3 },  // All offbeats
                Description = "Dense chorus pattern"
            }
        };

        private static readonly IReadOnlyList<CompRhythmPattern> BossaNovaDefaultPatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "BossaNova_Default",
                IncludedOnsetIndices = new[] { 0, 2, 3 },  // 1.5, 3.5, 4.5
                Description = "Default offbeat emphasis"
            }
        };

        // AI: CountryTrain CompOnsets: { 1.5m, 2.5m, 3.5m, 4.5m } (4 onsets, offbeats)

        private static readonly IReadOnlyList<CompRhythmPattern> CountryTrainVersePatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "CountryTrain_Verse_Light",
                IncludedOnsetIndices = new[] { 0, 2 },  // 1.5, 3.5
                Description = "Light backbeat chords"
            },
            new CompRhythmPattern
            {
                Name = "CountryTrain_Verse_Skip3",
                IncludedOnsetIndices = new[] { 0, 1, 3 },  // Skip 3.5
                Description = "Skip beat 3 offbeat"
            }
        };

        private static readonly IReadOnlyList<CompRhythmPattern> CountryTrainChorusPatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "CountryTrain_Chorus_Full",
                IncludedOnsetIndices = new[] { 0, 1, 2, 3 },  // All offbeats
                Description = "Full driving pattern"
            }
        };

        private static readonly IReadOnlyList<CompRhythmPattern> CountryTrainDefaultPatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "CountryTrain_Default",
                IncludedOnsetIndices = new[] { 0, 1, 2 },  // Most offbeats
                Description = "Standard country comp"
            }
        };

        // AI: DanceEDMFourOnFloor CompOnsets: { 1.5m, 2.5m, 3.5m, 4.5m } (4 onsets, offbeats)

        private static readonly IReadOnlyList<CompRhythmPattern> DanceEDMVersePatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "DanceEDM_Verse_Minimal",
                IncludedOnsetIndices = new[] { 1, 3 },  // 2.5, 4.5
                Description = "Minimal syncopation"
            },
            new CompRhythmPattern
            {
                Name = "DanceEDM_Verse_Alternate",
                IncludedOnsetIndices = new[] { 0, 2 },  // 1.5, 3.5
                Description = "Alternate syncopation"
            }
        };

        private static readonly IReadOnlyList<CompRhythmPattern> DanceEDMChorusPatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "DanceEDM_Chorus_Full",
                IncludedOnsetIndices = new[] { 0, 1, 2, 3 },  // All offbeats
                Description = "Full syncopated hits"
            }
        };

        private static readonly IReadOnlyList<CompRhythmPattern> DanceEDMDefaultPatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "DanceEDM_Default",
                IncludedOnsetIndices = new[] { 0, 1, 2, 3 },  // All offbeats
                Description = "Full EDM comp"
            }
        };

        // AI: FunkSyncopated CompOnsets: { 1.5m, 2.5m, 3.5m, 4.5m } (4 onsets, offbeats)

        private static readonly IReadOnlyList<CompRhythmPattern> FunkVersePatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "Funk_Verse_Syncopated",
                IncludedOnsetIndices = new[] { 0, 2, 3 },  // 1.5, 3.5, 4.5
                Description = "Funk syncopation pattern"
            },
            new CompRhythmPattern
            {
                Name = "Funk_Verse_Skip2",
                IncludedOnsetIndices = new[] { 0, 2 },  // Skip 2.5
                Description = "Skip beat 2 offbeat"
            }
        };

        private static readonly IReadOnlyList<CompRhythmPattern> FunkChorusPatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "Funk_Chorus_Full",
                IncludedOnsetIndices = new[] { 0, 1, 2, 3 },  // All offbeats
                Description = "Full funk comp"
            }
        };

        private static readonly IReadOnlyList<CompRhythmPattern> FunkDefaultPatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "Funk_Default",
                IncludedOnsetIndices = new[] { 0, 1, 2, 3 },  // All offbeats
                Description = "Full funk pattern"
            }
        };

        // AI: DefaultFullPatterns: fallback when groove/section combination not found.
        // AI: Assumes typical 4-onset CompOnsets pattern; uses all available onsets.
        private static readonly IReadOnlyList<CompRhythmPattern> DefaultFullPatterns = new[]
        {
            new CompRhythmPattern
            {
                Name = "Default_Full",
                IncludedOnsetIndices = new[] { 0, 1, 2, 3 },  // All onsets
                Description = "Use all available onsets"
            }
        };
    }
}
