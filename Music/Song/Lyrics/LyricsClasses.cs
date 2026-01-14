namespace MusicGen.Lyrics
{
    /// <summary>
    /// Synchronized lyrics track. Primary timing unit is the syllable nucleus (vowel).
    /// </summary>
    public sealed class LyricTrack
    {
        public string LanguageTag { get; set; } = "en-US";

        public LyricDefaults Defaults { get; set; } = new();

        public List<LyricPhrase> Phrases { get; } = new();
    }

    /// <summary>
    /// A lyric phrase/line that the system will align to a musical phrase.
    /// </summary>
    public sealed class LyricPhrase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>Raw input for display/editing.</summary>
        public string RawText { get; set; } = string.Empty;

        /// <summary>
        /// Optional: link to an existing section in your section track.
        /// </summary>
        public string? SectionId { get; set; }

        /// <summary>
        /// Optional placement hint. If null, solver chooses.
        /// Interpreted as the phrase start in musical ticks.
        /// </summary>
        /// 

        public MusicalTime? StartTime { get; set; }

        /// <summary>
        /// Optional phrase duration budget (min/target/max). Useful for fitting into bars.
        /// </summary>
        public TickSpanConstraint? DurationBudget { get; set; }

        public List<LyricWord> Words { get; } = new();

        /// <summary>
        /// Convenience list in performance order. Keep in sync with Words/Syllables if you use it.
        /// </summary>
        public List<LyricSyllable> Syllables { get; } = new();
    }

    public sealed class LyricWord
    {
        public string Text { get; set; } = string.Empty;

        /// <summary>True for punctuation tokens if you tokenize them separately.</summary>
        public bool IsPunctuation { get; set; }

        public List<LyricSyllable> Syllables { get; } = new();
    }

    /// <summary>
    /// Atomic alignment unit. One syllable maps to 1..N notes (melisma).
    /// Anchor time is interpreted as NucleusStart (vowel start) by default.
    /// </summary>
    public sealed class LyricSyllable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>Display text for this syllable (e.g., from hyphenation).</summary>
        public string Text { get; set; } = string.Empty;

        public StressLevel Stress { get; set; } = StressLevel.Unstressed;

        /// <summary>Manual emphasis override (e.g., user markup).</summary>
        public bool Emphasis { get; set; }

        /// <summary>
        /// Optional: tag end-rhyme groups (A, B, etc.) or a hash for rhyme families.
        /// Typically set on the last stressed syllable of a line.
        /// </summary>
        public string? RhymeGroup { get; set; }

        /// <summary>
        /// Indicates the syllable is followed by a good breath spot (comma, line break, explicit).
        /// Algorithms may insert a rest or phrase boundary here.
        /// </summary>
        public bool BreathAfter { get; set; }

        public SyllablePhones Phones { get; set; } = new();

        /// <summary>
        /// Optional fixed or initial anchor. If null, solver chooses.
        /// By convention this is NucleusStart (vowel start) unless Defaults.AnchorIsSyllableStart is true.
        /// </summary>
        public MusicalTime? AnchorTime { get; set; }

        /// <summary>
        /// Optional timing micro-hints for consonants around the nucleus anchor.
        /// LeadIn allows onset consonants before AnchorTime; TailOut allows coda after the last note.
        /// Set both to 0 for a simpler system.
        /// </summary>
        public ConsonantTimingHints ConsonantTiming { get; set; } = new();

        /// <summary>
        /// How many notes may carry this syllable's nucleus (melisma).
        /// </summary>
        public MelismaConstraint Melisma { get; set; } = new();
    }

    /// <summary>
    /// Standard singing-friendly phonological grouping: onset/nucleus/coda.
    /// Use IPA, ARPAbet, or your own symbols. Keep it flexible.
    /// </summary>
    public sealed class SyllablePhones
    {
        public List<string> Onset { get; } = new();
        public List<string> Nucleus { get; } = new();
        public List<string> Coda { get; } = new();

        /// <summary>
        /// Optional pronunciation variant id (e.g., reduced form in fast singing).
        /// </summary>
        public string? VariantId { get; set; }
    }

    public sealed class LyricDefaults
    {
        /// <summary>
        /// If true, AnchorTime represents syllable start (including onset).
        /// If false (recommended), AnchorTime represents nucleus start (vowel start).
        /// </summary>
        public bool AnchorIsSyllableStart { get; set; } = false;

        public ConsonantTimingHints DefaultConsonantTiming { get; set; } = new()
        {
            LeadInTicks = 0,
            TailOutTicks = 0
        };

        public MelismaConstraint DefaultMelisma { get; set; } = new()
        {
            MinNotes = 1,
            MaxNotes = 2,
            PreferMelisma = 0.1f
        };
    }

    public sealed class ConsonantTimingHints
    {
        /// <summary>
        /// How many ticks the onset consonants may start before the anchor.
        /// If your engine cannot represent negative offsets, keep 0.
        /// </summary>
        public long LeadInTicks { get; set; } = 0;

        /// <summary>
        /// How many ticks coda consonants may extend after the last pitched note.
        /// </summary>
        public long TailOutTicks { get; set; } = 0;
    }

    public sealed class MelismaConstraint
    {
        public int MinNotes { get; set; } = 1;
        public int MaxNotes { get; set; } = 1;

        /// <summary>
        /// 0..1 preference for stretching across notes when space allows.
        /// </summary>
        public float PreferMelisma { get; set; } = 0.0f;
    }

    public enum StressLevel
    {
        Unstressed = 0,
        Secondary = 1,
        Primary = 2
    }

    public readonly struct MusicalTime
    {
        public MusicalTime(long ticks) => Ticks = ticks;
        public long Ticks { get; }
        public override string ToString() => $"{Ticks} ticks";
    }

    public sealed class TickSpanConstraint
    {
        public long? MinTicks { get; set; }
        public long? TargetTicks { get; set; }
        public long? MaxTicks { get; set; }

        /// <summary>0..1 importance for solvers.</summary>
        public float Weight { get; set; } = 0.7f;
    }
}
