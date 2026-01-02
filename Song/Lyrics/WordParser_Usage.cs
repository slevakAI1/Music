/*
 * WordParser and Lyric Phonetics System - Usage Guide
 * 
 * This system provides automated phonetic analysis of lyrics using the CMU Pronouncing Dictionary.
 * The dictionary contains ~60,000 words with ARPAbet pronunciations and stress markings.
 * 
 * ARCHITECTURE:
 * 
 * 1. WordParser (Singleton)
 *    - Loads cmudict.rep at startup (initialized in Program.Main)
 *    - Provides fast O(1) word lookup
 *    - Returns WordPronunciation objects with parsed phonemes
 * 
 * 2. WordPronunciation
 *    - Represents a single pronunciation variant
 *    - Stores raw ARPAbet phonemes from dictionary
 *    - Lazily parses into StressSyllable objects (onset/nucleus/coda)
 *    - Provides rhyme key generation and syllable counting
 * 
 * 3. StressSyllable
 *    - Mirrors LyricsClasses.SyllablePhones structure
 *    - Groups phonemes into onset/nucleus/coda (singing-friendly)
 *    - Converts directly to LyricPhrase syllables
 * 
 * 4. LyricPhoneticsHelper
 *    - Bridge between WordParser and LyricTrack
 *    - Parses raw text into phonetically-aware LyricPhrases
 *    - Auto-marks breath points and rhyme groups
 * 
 * ============================================================================
 * BASIC USAGE - WORD LOOKUP:
 * ============================================================================
 */

using Music.Generator;
using MusicGen.Lyrics;

namespace Music.Examples
{
    internal static class WordParserUsageExamples
    {
        public static void Example1_SimpleWordLookup()
        {
            // Simple word lookup
            var pronunciation = WordParser.Instance.TryLookup("singing");
            if (pronunciation != null)
            {
                Console.WriteLine($"Word: {pronunciation.Word}");
                Console.WriteLine($"ARPAbet: {pronunciation.RawPhonemes}");
                Console.WriteLine($"Syllables: {pronunciation.CountSyllables()}");
                Console.WriteLine($"Has primary stress: {pronunciation.HasPrimaryStress()}");
                
                // Get syllable breakdown
                foreach (var syllable in pronunciation.GetStressSyllables())
                {
                    Console.WriteLine($"  {syllable}"); // Shows onset[nucleus]coda<stress>
                }
            }
        }

        public static void Example2_HandleHomographs()
        {
            // Handle words with multiple pronunciations (homographs)
            var readPronunciations = WordParser.Instance.Lookup("read"); // Returns list
            foreach (var p in readPronunciations)
            {
                Console.WriteLine($"Variant {p.VariantId ?? "1"}: {p.RawPhonemes}");
            }
        }

        /*
         * ============================================================================
         * ADVANCED USAGE - RHYME FINDING:
         * ============================================================================
         */

        public static void Example3_FindRhymes()
        {
            // Find rhyming words
            var rhymes = WordParser.Instance.GetRhymingWords("singing", maxResults: 20);
            // Returns: ["ringing", "bringing", "clinging", "stinging", ...]
        }

        public static void Example4_FindBySyllableCount()
        {
            // Find words by syllable count (for meter matching)
            var twoSyllableWords = WordParser.Instance.GetWordsBySyllableCount(2, maxResults: 100);
        }

        public static void Example5_CustomPatternMatching()
        {
            // Custom pattern matching (e.g., words ending with specific stress)
            var wordsWithFinalStress = WordParser.Instance.GetWordsMatchingPattern(
                p => {
                    var syllables = p.GetStressSyllables();
                    return syllables.Count > 0 && 
                           syllables[^1].Stress == StressLevel.Primary;
                },
                maxResults: 50
            );
        }

        /*
         * ============================================================================
         * LYRIC TRACK INTEGRATION:
         * ============================================================================
         */

        public static void Example6_ParseTextToPhrase()
        {
            // Parse text into a LyricPhrase with automatic phonetics
            var phrase1 = new LyricPhrase();
            LyricPhoneticsHelper.ParseTextToLyricPhrase(phrase1, "I love to sing");

            // Result structure:
            // phrase1.Words[0].Text = "I"
            // phrase1.Words[0].Syllables[0].Phones.Nucleus = ["AY"]
            // phrase1.Words[1].Text = "love"
            // phrase1.Words[1].Syllables[0].Phones.Onset = ["L"]
            // phrase1.Words[1].Syllables[0].Phones.Nucleus = ["AH"]
            // phrase1.Words[1].Syllables[0].Phones.Coda = ["V"]
            // ... etc
        }

        public static void Example7_MarkBreathPoints()
        {
            var phrase2 = new LyricPhrase();
            LyricPhoneticsHelper.ParseTextToLyricPhrase(phrase2, "I love to sing");
            
            // Mark breath points automatically
            LyricPhoneticsHelper.MarkBreathPoints(phrase2);
            // Sets BreathAfter=true on syllables before punctuation and at phrase end
        }

        public static void Example8_MarkRhymeScheme()
        {
            // Mark rhyme scheme across multiple phrases
            var verse = new List<LyricPhrase>
            {
                new LyricPhrase { RawText = "Roses are red" },
                new LyricPhrase { RawText = "Violets are blue" },
                new LyricPhrase { RawText = "Sugar is sweet" },
                new LyricPhrase { RawText = "And so are you" }
            };

            foreach (var p in verse)
            {
                LyricPhoneticsHelper.ParseTextToLyricPhrase(p, p.RawText);
            }

            LyricPhoneticsHelper.MarkRhymeGroups(verse);
            // Result: verse[0].Syllables[last].RhymeGroup = "A" (red)
            //         verse[1].Syllables[last].RhymeGroup = "B" (blue)
            //         verse[2].Syllables[last].RhymeGroup = "C" (sweet)
            //         verse[3].Syllables[last].RhymeGroup = "B" (you)
        }

        /*
         * ============================================================================
         * BUILDING A COMPLETE LYRIC TRACK:
         * ============================================================================
         */

        public static void Example9_CompleteWorkflow()
        {
            // Full workflow
            var track = new LyricTrack
            {
                LanguageTag = "en-US",
                Defaults = new LyricDefaults
                {
                    AnchorIsSyllableStart = false, // Anchor at vowel start
                    DefaultConsonantTiming = new ConsonantTimingHints
                    {
                        LeadInTicks = 0,
                        TailOutTicks = 0
                    },
                    DefaultMelisma = new MelismaConstraint
                    {
                        MinNotes = 1,
                        MaxNotes = 3,
                        PreferMelisma = 0.2f
                    }
                }
            };

            var lines = new[]
            {
                "Amazing grace how sweet the sound",
                "That saved a wretch like me",
                "I once was lost but now am found",
                "Was blind but now I see"
            };

            foreach (var line in lines)
            {
                var phraseItem = new LyricPhrase();
                LyricPhoneticsHelper.ParseTextToLyricPhrase(phraseItem, line);
                LyricPhoneticsHelper.MarkBreathPoints(phraseItem);
                track.Phrases.Add(phraseItem);
            }

            LyricPhoneticsHelper.MarkRhymeGroups(track.Phrases);
        }
    }
}

/*
 * ============================================================================
 * SYLLABLE PHONES STRUCTURE (ARPAbet reference):
 * ============================================================================
 * 
 * ARPAbet vowels (nucleus):
 *   AO (thought), AA (father), IY (beat), EH (bet), AE (bat)
 *   UH (book), AH (but), AY (bite), EY (bait), OY (boy)
 *   AW (about), OW (boat), UW (boot), ER (bird)
 *   + stress digits: 0 (unstressed), 1 (primary), 2 (secondary)
 * 
 * ARPAbet consonants (onset/coda):
 *   Stops: P B T D K G
 *   Fricatives: F V TH DH S Z SH ZH HH
 *   Affricates: CH JH
 *   Nasals: M N NG
 *   Liquids: L R
 *   Semivowels: W Y
 * 
 * Example: "SINGING" = S IH1 NG IH0 NG
 *   Syllable 1: Onset=[S], Nucleus=[IH], Coda=[NG], Stress=Primary
 *   Syllable 2: Onset=[], Nucleus=[IH], Coda=[NG], Stress=Unstressed
 * 
 * ============================================================================
 * PERFORMANCE NOTES:
 * ============================================================================
 * 
 * - Dictionary loads once at startup (~60k words, <100ms)
 * - Lookup is O(1) via Dictionary
 * - Syllable parsing is deferred until first access (lazy)
 * - Rhyme search iterates full dictionary (cache results if used frequently)
 * - Thread-safe after initialization (singleton pattern)
 * 
 * ============================================================================
 * LIMITATIONS & FUTURE ENHANCEMENTS:
 * ============================================================================
 * 
 * Current:
 *   - Unknown words fall back to single-syllable placeholder
 *   - Syllable text display uses phonemes (not aligned to graphemes)
 *   - Tokenization is basic (whitespace split with punctuation handling)
 * 
 * Future:
 *   - Implement grapheme-to-phoneme alignment for accurate syllable text
 *   - Add custom pronunciation dictionary for names/slang
 *   - Integrate syllabification rules for unknown words
 *   - Add IPA output option alongside ARPAbet
 *   - Cache rhyme lookups for performance
 * 
 */
