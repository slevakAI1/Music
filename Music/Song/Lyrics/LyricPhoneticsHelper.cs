namespace Music.Generator
{
    // AI: purpose=Bridge between WordParser and LyricsClasses; converts CMU pronunciations to lyric syllables.
    // AI: use=Call ParseTextToLyricPhrase() to auto-populate LyricPhrase with phonetically-aware syllables.
    // AI: note=Handles unknown words by fallback splitting; preserves word boundaries and punctuation.
    public static class LyricPhoneticsHelper
    {
        // AI: ParseTextToLyricPhrase: tokenizes text, looks up pronunciations, populates phrase with syllables+phones.
        // AI: logic=Splits on whitespace; each token becomes LyricWord; syllables inherit stress from pronunciation.
        public static void ParseTextToLyricPhrase(MusicGen.Lyrics.LyricPhrase phrase, string text)
        {
            if (phrase == null || string.IsNullOrWhiteSpace(text))
                return;

            phrase.RawText = text;
            phrase.Words.Clear();
            phrase.Syllables.Clear();

            var tokens = TokenizeText(text);
            
            foreach (var token in tokens)
            {
                var word = CreateLyricWord(token);
                phrase.Words.Add(word);
                
                // Also maintain flat syllable list for convenience
                foreach (var syllable in word.Syllables)
                {
                    phrase.Syllables.Add(syllable);
                }
            }
        }

        // AI: CreateLyricWord: looks up pronunciation, creates LyricWord with syllables; fallback for unknown words.
        private static MusicGen.Lyrics.LyricWord CreateLyricWord(string token)
        {
            var word = new MusicGen.Lyrics.LyricWord { Text = token };
            
            // Check if punctuation
            if (IsPunctuation(token))
            {
                word.IsPunctuation = true;
                return word;
            }

            // Look up pronunciation
            var pronunciation = WordParser.Instance.TryLookup(token);
            
            if (pronunciation != null)
            {
                // Use CMU dictionary pronunciation
                var stressSyllables = pronunciation.GetStressSyllables();
                
                foreach (var stressSyllable in stressSyllables)
                {
                    var lyricSyllable = new MusicGen.Lyrics.LyricSyllable
                    {
                        Text = BuildSyllableText(stressSyllable),
                        Stress = stressSyllable.Stress,
                        Phones = stressSyllable.ToLyricPhones()
                    };
                    
                    word.Syllables.Add(lyricSyllable);
                }
            }
            else
            {
                // Fallback: create single syllable for unknown word
                var lyricSyllable = new MusicGen.Lyrics.LyricSyllable
                {
                    Text = token,
                    Stress = MusicGen.Lyrics.StressLevel.Unstressed,
                    Phones = new MusicGen.Lyrics.SyllablePhones()
                };
                
                word.Syllables.Add(lyricSyllable);
            }
            
            return word;
        }

        // AI: BuildSyllableText: constructs display text from phonemes; placeholder implementation uses phoneme string.
        // AI: TODO: Implement proper grapheme-to-phoneme alignment for accurate syllable text display.
        private static string BuildSyllableText(StressSyllable stressSyllable)
        {
            // Simple concatenation of phonemes for now
            // In production, would use grapheme alignment to extract correct text portion
            var onset = string.Join("", stressSyllable.Onset);
            var nucleus = string.Join("", stressSyllable.Nucleus);
            var coda = string.Join("", stressSyllable.Coda);
            
            return onset + nucleus + coda;
        }

        // AI: TokenizeText: splits on whitespace; preserves punctuation as separate tokens.
        private static List<string> TokenizeText(string text)
        {
            var tokens = new List<string>();
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                // Simple split: treat punctuation at end as separate token
                var trimmed = word.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Check for trailing punctuation
                if (trimmed.Length > 1 && IsPunctuation(trimmed[^1].ToString()))
                {
                    tokens.Add(trimmed[..^1]);
                    tokens.Add(trimmed[^1].ToString());
                }
                else
                {
                    tokens.Add(trimmed);
                }
            }
            
            return tokens;
        }

        // AI: IsPunctuation: checks if token is common punctuation; extend as needed.
        private static bool IsPunctuation(string token)
        {
            if (token.Length == 0)
                return false;
            
            if (token.Length == 1)
            {
                char c = token[0];
                return c == ',' || c == '.' || c == '!' || c == '?' || c == ';' || c == ':' || 
                       c == '-' || c == '(' || c == ')' || c == '"' || c == '\'';
            }
            
            return false;
        }

        // AI: MarkBreathPoints: auto-marks BreathAfter on syllables before punctuation or phrase end.
        // AI: call=After ParseTextToLyricPhrase to add breath hints for natural phrasing.
        public static void MarkBreathPoints(MusicGen.Lyrics.LyricPhrase phrase)
        {
            if (phrase?.Syllables == null || phrase.Syllables.Count == 0)
                return;

            // Mark last syllable of phrase
            phrase.Syllables[^1].BreathAfter = true;

            // Mark syllables before punctuation
            for (int i = 0; i < phrase.Words.Count - 1; i++)
            {
                var currentWord = phrase.Words[i];
                var nextWord = phrase.Words[i + 1];

                if (nextWord.IsPunctuation && currentWord.Syllables.Count > 0)
                {
                    currentWord.Syllables[^1].BreathAfter = true;
                }
            }
        }

        // AI: MarkRhymeGroups: tags final stressed syllables with rhyme group labels (A, B, C, etc.).
        // AI: use=Call with list of phrases in a verse/chorus to mark rhyme scheme.
        public static void MarkRhymeGroups(List<MusicGen.Lyrics.LyricPhrase> phrases)
        {
            if (phrases == null || phrases.Count == 0)
                return;

            var rhymeGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var labels = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int labelIndex = 0;

            foreach (var phrase in phrases)
            {
                // Find last stressed syllable in phrase
                MusicGen.Lyrics.LyricSyllable? lastStressed = null;
                
                for (int i = phrase.Syllables.Count - 1; i >= 0; i--)
                {
                    if (phrase.Syllables[i].Stress != MusicGen.Lyrics.StressLevel.Unstressed)
                    {
                        lastStressed = phrase.Syllables[i];
                        break;
                    }
                }

                if (lastStressed == null)
                    continue;

                // Compute rhyme key from phonemes
                var rhymeKey = ComputeRhymeKey(lastStressed.Phones);
                
                if (string.IsNullOrEmpty(rhymeKey))
                    continue;

                // Assign or retrieve rhyme group label
                if (!rhymeGroups.ContainsKey(rhymeKey))
                {
                    if (labelIndex < labels.Length)
                    {
                        rhymeGroups[rhymeKey] = labels[labelIndex].ToString();
                        labelIndex++;
                    }
                }

                lastStressed.RhymeGroup = rhymeGroups[rhymeKey];
            }
        }

        // AI: ComputeRhymeKey: builds rhyme signature from nucleus+coda phonemes.
        private static string ComputeRhymeKey(MusicGen.Lyrics.SyllablePhones phones)
        {
            var nucleus = string.Join("", phones.Nucleus);
            var coda = string.Join("", phones.Coda);
            return nucleus + coda;
        }
    }
}
