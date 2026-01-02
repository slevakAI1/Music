using MusicGen.Lyrics;

namespace Music.Generator
{
    // AI: purpose=Represents a single pronunciation for a word from CMU dictionary with parsed phonemes and syllable structure.
    // AI: invariants=ARPAbet phonemes stored raw; StressSyllables built lazily; stress digits 0/1/2 map to StressLevel enum.
    // AI: note=Multiple pronunciations per word possible (homographs); use VariantId to distinguish (e.g., "READ" vs "READ(2)").
    public sealed class WordPronunciation
    {
        public string Word { get; init; } = string.Empty;
        
        public string? VariantId { get; init; }
        
        // AI: RawPhonemes: space-separated ARPAbet from CMU dict (e.g., "K AE1 T" for "CAT"); preserves stress digits.
        public string RawPhonemes { get; init; } = string.Empty;
        
        // AI: Phonemes: parsed ARPAbet tokens preserving stress markers (e.g., ["K", "AE1", "T"]).
        public List<string> Phonemes { get; init; } = new();
        
        // AI: StressSyllables: cached syllable grouping with stress info; built on first access via GetStressSyllables().
        private List<StressSyllable>? _stressSyllables;

        // AI: GetStressSyllables: groups phonemes into syllables based on vowel nuclei; caches result.
        // AI: logic=Each vowel (phoneme with stress digit) starts a new syllable; consonants attach to nearest vowel.
        public List<StressSyllable> GetStressSyllables()
        {
            if (_stressSyllables != null)
                return _stressSyllables;

            _stressSyllables = new List<StressSyllable>();
            
            if (Phonemes.Count == 0)
                return _stressSyllables;

            var currentSyllable = new StressSyllable();
            var inNucleus = false;

            foreach (var phoneme in Phonemes)
            {
                var (basePhone, stress) = ParseStress(phoneme);

                if (stress.HasValue)
                {
                    // Start new syllable if we already have a nucleus
                    if (inNucleus && currentSyllable.Nucleus.Count > 0)
                    {
                        _stressSyllables.Add(currentSyllable);
                        currentSyllable = new StressSyllable();
                        inNucleus = false;
                    }

                    // This is a vowel - it's the nucleus
                    currentSyllable.Nucleus.Add(basePhone);
                    currentSyllable.Stress = stress.Value;
                    inNucleus = true;
                }
                else if (!inNucleus)
                {
                    // Consonant before nucleus = onset
                    currentSyllable.Onset.Add(basePhone);
                }
                else
                {
                    // Consonant after nucleus = coda
                    currentSyllable.Coda.Add(basePhone);
                }
            }

            // Add final syllable
            if (currentSyllable.Nucleus.Count > 0)
            {
                _stressSyllables.Add(currentSyllable);
            }

            return _stressSyllables;
        }

        // AI: ParseStress: extracts base phoneme and optional stress level from ARPAbet token (e.g., "AE1" -> ("AE", Primary)).
        private static (string basePhone, StressLevel? stress) ParseStress(string phoneme)
        {
            if (phoneme.Length > 0 && char.IsDigit(phoneme[^1]))
            {
                var basePhone = phoneme[..^1];
                var stressDigit = phoneme[^1] - '0';
                var stress = stressDigit switch
                {
                    0 => StressLevel.Unstressed,
                    1 => StressLevel.Primary,
                    2 => StressLevel.Secondary,
                    _ => StressLevel.Unstressed
                };
                return (basePhone, stress);
            }
            
            return (phoneme, null);
        }

        // AI: GetRhymeKey: returns rhyme signature based on nucleus+coda of final stressed syllable; empty if no stress.
        // AI: use=Group words by rhyme families for lyric generation; case-insensitive comparison recommended.
        public string GetRhymeKey()
        {
            var syllables = GetStressSyllables();
            
            // Find last stressed syllable (primary or secondary)
            for (int i = syllables.Count - 1; i >= 0; i--)
            {
                if (syllables[i].Stress != StressLevel.Unstressed)
                {
                    var nucleus = string.Join("", syllables[i].Nucleus);
                    var coda = string.Join("", syllables[i].Coda);
                    return nucleus + coda;
                }
            }
            
            return string.Empty;
        }

        // AI: CountSyllables: returns number of vowel nuclei found in pronunciation.
        public int CountSyllables() => GetStressSyllables().Count;

        // AI: HasPrimaryStress: true if any syllable has primary stress.
        public bool HasPrimaryStress() => GetStressSyllables().Any(s => s.Stress == StressLevel.Primary);
    }

    // AI: StressSyllable: phonological structure matching LyricsClasses.SyllablePhones; onset/nucleus/coda grouping.
    // AI: invariants=Nucleus must contain at least one vowel; Stress defaults to Unstressed.
    public sealed class StressSyllable
    {
        public List<string> Onset { get; init; } = new();
        public List<string> Nucleus { get; init; } = new();
        public List<string> Coda { get; init; } = new();
        public StressLevel Stress { get; set; } = StressLevel.Unstressed;

        // AI: ToLyricPhones: converts to LyricsClasses.SyllablePhones format for direct use in lyric track.
        public MusicGen.Lyrics.SyllablePhones ToLyricPhones()
        {
            var phones = new MusicGen.Lyrics.SyllablePhones();
            
            foreach (var p in Onset)
                phones.Onset.Add(p);
            
            foreach (var p in Nucleus)
                phones.Nucleus.Add(p);
            
            foreach (var p in Coda)
                phones.Coda.Add(p);
            
            return phones;
        }

        public override string ToString()
        {
            var onset = string.Join("", Onset);
            var nucleus = string.Join("", Nucleus);
            var coda = string.Join("", Coda);
            var stress = Stress == StressLevel.Primary ? "1" : Stress == StressLevel.Secondary ? "2" : "0";
            return $"{onset}[{nucleus}]{coda}<{stress}>";
        }
    }
}
