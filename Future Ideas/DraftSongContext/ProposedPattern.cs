namespace Music.Generator
{
    /// <summary>
    /// A reusable musical pattern (hook, phrase, groove variation, etc.).
    /// Patterns are templates that can be instantiated at different positions in the song.
    /// </summary>
    public sealed class ProposedPattern
    {
        /// <summary>
        /// Unique identifier for this pattern.
        /// </summary>
        public string PatternId { get; init; }

        /// <summary>
        /// The type of pattern (Hook, Phrase, GrooveVariation, Fill, etc.).
        /// </summary>
        public PatternType PatternType { get; set; }

        /// <summary>
        /// The groove role this pattern applies to (or null if applicable to multiple).
        /// </summary>
        public string? GrooveRole { get; set; }

        /// <summary>
        /// Section type this pattern is designed for (or null if universal).
        /// </summary>
        public SectionType? TargetSectionType { get; set; }

        /// <summary>
        /// Length of the pattern in bars.
        /// </summary>
        public int LengthBars { get; set; }

        /// <summary>
        /// The note events in this pattern, with positions relative to pattern start.
        /// </summary>
        public List<PatternNoteEvent> NoteEvents { get; set; }

        /// <summary>
        /// Groove onset pattern used (if this pattern includes rhythm information).
        /// </summary>
        public PatternGrooveData? GrooveData { get; set; }

        /// <summary>
        /// Tags for pattern identification and retrieval.
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Energy level of this pattern (0.0-1.0).
        /// </summary>
        public double EnergyLevel { get; set; }

        public ProposedPattern()
        {
            PatternId = Guid.NewGuid().ToString("N");
            PatternType = PatternType.Phrase;
            LengthBars = 1;
            NoteEvents = new List<PatternNoteEvent>();
            Tags = new List<string>();
            EnergyLevel = 0.5;
        }
    }

    /// <summary>
    /// Types of musical patterns.
    /// </summary>
    public enum PatternType
    {
        Hook,            // Identity token - main memorable element
        Phrase,          // Melodic or rhythmic phrase
        GrooveVariation, // Variation of base groove
        Fill,            // Transitional fill
        Riff,            // Repeating instrumental figure
        Cadence,         // Phrase ending pattern
        Turnaround       // Transition pattern
    }

    /// <summary>
    /// A note event within a pattern, with relative positioning.
    /// </summary>
    public sealed class PatternNoteEvent
    {
        /// <summary>
        /// Relative bar within the pattern (0-based).
        /// </summary>
        public int RelativeBar { get; set; }

        /// <summary>
        /// Beat position within the bar (1-based, decimal for subdivisions).
        /// </summary>
        public decimal BeatPosition { get; set; }

        /// <summary>
        /// Pitch specification (can be absolute MIDI note or relative to chord/scale).
        /// </summary>
        public PatternPitchSpec PitchSpec { get; set; }

        /// <summary>
        /// Duration in ticks.
        /// </summary>
        public int DurationTicks { get; set; }

        /// <summary>
        /// Velocity (0-127).
        /// </summary>
        public int Velocity { get; set; }

        public PatternNoteEvent()
        {
            PitchSpec = new PatternPitchSpec();
            Velocity = 100;
        }
    }

    /// <summary>
    /// Specifies how to determine pitch for a pattern note.
    /// Allows patterns to be key/chord-independent.
    /// </summary>
    public sealed class PatternPitchSpec
    {
        /// <summary>
        /// The type of pitch specification.
        /// </summary>
        public PitchSpecType SpecType { get; set; }

        /// <summary>
        /// For Absolute: the MIDI note number.
        /// For ChordDegree/ScaleDegree: the degree (1-based).
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Octave offset from reference point.
        /// </summary>
        public int OctaveOffset { get; set; }

        public PatternPitchSpec()
        {
            SpecType = PitchSpecType.ChordDegree;
            Value = 1; // Root
        }
    }

    /// <summary>
    /// How to interpret the pitch value in a pattern.
    /// </summary>
    public enum PitchSpecType
    {
        Absolute,     // MIDI note number
        ChordDegree,  // 1=root, 3=third, 5=fifth, etc.
        ScaleDegree,  // 1-7 within current key
        Interval      // Semitones from previous note
    }

    /// <summary>
    /// Groove rhythm data embedded in a pattern.
    /// </summary>
    public sealed class PatternGrooveData
    {
        /// <summary>
        /// Source groove preset name.
        /// </summary>
        public string SourcePresetName { get; set; } = string.Empty;

        /// <summary>
        /// Custom onset positions that override/augment the preset.
        /// </summary>
        public List<decimal> CustomOnsets { get; set; }

        /// <summary>
        /// Whether tension layer onsets are included.
        /// </summary>
        public bool IncludeTensionLayer { get; set; }

        public PatternGrooveData()
        {
            CustomOnsets = new List<decimal>();
        }
    }
}