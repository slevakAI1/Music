namespace Music.Generator
{
    // AI: purpose=Singleton loader for CMU pronunciation dictionary; provides fast lookup by word for lyric processing.
    // AI: invariants=Dictionary keys are uppercase; Load() called once at startup; thread-safe after initialization.
    // AI: deps=Reads cmudict.rep from Generator folder; format: WORD<space>PHONEMES per line, ## comments, variants via (N).
    // AI: perf=~60k words loaded once; lookup is O(1) via Dictionary; syllable parsing deferred until access.
    public sealed class WordParser
    {
        private static WordParser? _instance;
        private static readonly object _lock = new object();

        // AI: _pronunciations: maps uppercase word to list of pronunciations (handles homographs with variants).
        private readonly Dictionary<string, List<WordPronunciation>> _pronunciations;

        // AI: Instance: lazy singleton; ensures Load() called exactly once; thread-safe.
        public static WordParser Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new WordParser();
                        }
                    }
                }
                return _instance;
            }
        }

        // AI: private ctor: loads cmudict.rep from Generator folder; skips comments and blank lines.
        private WordParser()
        {
            _pronunciations = new Dictionary<string, List<WordPronunciation>>(StringComparer.OrdinalIgnoreCase);
            LoadDictionary();
        }

        // AI: LoadDictionary: reads cmudict.rep line-by-line; parses format "WORD  PHONEMES" or "WORD(N)  PHONEMES".
        // AI: errors=Silently skips malformed lines; logs to console in debug; does not throw on load failure.
        private void LoadDictionary()
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
                var filePath = Path.Combine(projectRoot, "Generator", "cmudict.rep");

                if (!File.Exists(filePath))
                {
#if DEBUG
                    Console.WriteLine($"[WordParser] Dictionary file not found: {filePath}");
#endif
                    return;
                }

                var lines = File.ReadAllLines(filePath);
                int loaded = 0;

                foreach (var line in lines)
                {
                    // Skip comments and empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("##"))
                        continue;

                    var entry = ParseLine(line);
                    if (entry != null)
                    {
                        var word = entry.Word.ToUpperInvariant();
                        
                        if (!_pronunciations.ContainsKey(word))
                        {
                            _pronunciations[word] = new List<WordPronunciation>();
                        }
                        
                        _pronunciations[word].Add(entry);
                        loaded++;
                    }
                }

#if DEBUG
                Console.WriteLine($"[WordParser] Loaded {loaded} pronunciations for {_pronunciations.Count} words");
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[WordParser] Error loading dictionary: {ex.Message}");
#endif
            }
        }

        // AI: ParseLine: parses "WORD  PHONEMES" or "WORD(2)  PHONEMES"; extracts word, variant, and phoneme list.
        // AI: format=Two-space separator between word and phonemes; variant in parens; phonemes space-separated ARPAbet.
        private static WordPronunciation? ParseLine(string line)
        {
            // Format: "WORD  PHONEMES" with two spaces as separator
            var parts = line.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return null;

            var wordPart = parts[0].Trim();
            var phonemesPart = parts[1].Trim();

            if (string.IsNullOrEmpty(wordPart) || string.IsNullOrEmpty(phonemesPart))
                return null;

            // Check for variant marker: WORD(2)
            string word;
            string? variantId = null;

            var parenIndex = wordPart.IndexOf('(');
            if (parenIndex > 0 && wordPart.EndsWith(')'))
            {
                word = wordPart.Substring(0, parenIndex);
                variantId = wordPart.Substring(parenIndex + 1, wordPart.Length - parenIndex - 2);
            }
            else
            {
                word = wordPart;
            }

            // Parse phonemes (space-separated, may include stress digits)
            var phonemes = phonemesPart
                .Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            return new WordPronunciation
            {
                Word = word,
                VariantId = variantId,
                RawPhonemes = phonemesPart,
                Phonemes = phonemes
            };
        }

        // AI: Lookup: returns all pronunciations for word (case-insensitive); empty list if not found.
        public List<WordPronunciation> Lookup(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return new List<WordPronunciation>();

            var key = word.ToUpperInvariant();
            return _pronunciations.TryGetValue(key, out var pronunciations) 
                ? pronunciations 
                : new List<WordPronunciation>();
        }

        // AI: TryLookup: returns first pronunciation or null; convenient for single-variant words.
        public WordPronunciation? TryLookup(string word)
        {
            var pronunciations = Lookup(word);
            return pronunciations.Count > 0 ? pronunciations[0] : null;
        }

        // AI: GetPronunciationCount: returns how many variants exist for word; 0 if not in dictionary.
        public int GetPronunciationCount(string word)
        {
            return Lookup(word).Count;
        }

        // AI: ContainsWord: true if dictionary has at least one pronunciation for word (case-insensitive).
        public bool ContainsWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            return _pronunciations.ContainsKey(word.ToUpperInvariant());
        }

        // AI: GetWordsMatchingPattern: returns words matching predicate; useful for rhyme search or syllable filtering.
        // AI: perf=Iterates entire dictionary; cache results if used frequently; limit results to avoid memory pressure.
        public List<string> GetWordsMatchingPattern(Func<WordPronunciation, bool> predicate, int maxResults = 100)
        {
            var results = new List<string>();
            
            foreach (var kvp in _pronunciations)
            {
                if (results.Count >= maxResults)
                    break;

                foreach (var pronunciation in kvp.Value)
                {
                    if (predicate(pronunciation))
                    {
                        results.Add(kvp.Key);
                        break; // Only add word once even if multiple variants match
                    }
                }
            }
            
            return results;
        }

        // AI: GetRhymingWords: finds words with matching rhyme key (nucleus+coda of final stressed syllable).
        // AI: use=Build rhyming dictionaries for lyric generation; results sorted by word.
        public List<string> GetRhymingWords(string targetWord, int maxResults = 50)
        {
            var targetPronunciation = TryLookup(targetWord);
            if (targetPronunciation == null)
                return new List<string>();

            var targetRhymeKey = targetPronunciation.GetRhymeKey();
            if (string.IsNullOrEmpty(targetRhymeKey))
                return new List<string>();

            return GetWordsMatchingPattern(
                p => p.GetRhymeKey().Equals(targetRhymeKey, StringComparison.OrdinalIgnoreCase) 
                     && !p.Word.Equals(targetWord, StringComparison.OrdinalIgnoreCase),
                maxResults)
                .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // AI: GetWordsBySyllableCount: returns words with exact syllable count; useful for meter constraints.
        public List<string> GetWordsBySyllableCount(int syllableCount, int maxResults = 100)
        {
            if (syllableCount < 1)
                return new List<string>();

            return GetWordsMatchingPattern(
                p => p.CountSyllables() == syllableCount,
                maxResults);
        }

        // AI: EnsureLoaded: explicit initialization call for startup; safe to call multiple times (idempotent).
        public static void EnsureLoaded()
        {
            // Access Instance to trigger lazy load
            _ = Instance;
        }
    }
}
