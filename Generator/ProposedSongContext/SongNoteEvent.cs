namespace Music.Generator
{
    /// <summary>
    /// A single note event within a song part track.
    /// Contains MIDI data plus metadata for analysis and display.
    /// </summary>
    public sealed class SongNoteEvent
    {
        // ============ Core MIDI Properties ============

        /// <summary>
        /// MIDI note number (0-127).
        /// </summary>
        public int NoteNumber { get; set; }

        /// <summary>
        /// Absolute position in ticks from song start.
        /// </summary>
        public long AbsolutePositionTicks { get; set; }

        /// <summary>
        /// Duration in ticks.
        /// </summary>
        public int DurationTicks { get; set; }

        /// <summary>
        /// Note-on velocity (0-127).
        /// </summary>
        public int Velocity { get; set; }

        // TO DO - CHECK WITH AI IF THIS IS REALLY NEEDED. HAS NO MIDI EQUIVALENT. ONLY NOTES EXIST.
        /// <summary>
        /// Whether this is a rest (placeholder for timing).
        /// </summary>
        public bool IsRest { get; set; }

        // ============ Musical Context Metadata ============

        /// <summary>
        /// Bar number where this note occurs (1-based).
        /// </summary>
        public int BarNumber { get; set; }

        /// <summary>
        /// Beat position within the bar (1-based, decimal for subdivisions).
        /// </summary>
        public decimal BeatPosition { get; set; }

        /// <summary>
        /// Pitch class (0-11, where 0=C).
        /// </summary>
        public int PitchClass { get; set; }

        /// <summary>
        /// Whether this note is a chord tone in the current harmony context.
        /// </summary>
        public bool IsChordTone { get; set; }

        /// <summary>
        /// Whether this note is on a strong beat.
        /// </summary>
        public bool IsStrongBeat { get; set; }

        // ============ Pattern/Origin Metadata ============

        /// <summary>
        /// ID of the pattern instance this note belongs to (null if not from a pattern).
        /// </summary>
        public string? PatternInstanceId { get; set; }

        /// <summary>
        /// The type of note selection that produced this note.
        /// </summary>
        public NoteSelectionType SelectionType { get; set; }

        /// <summary>
        /// Optional reference to chord metadata if this note is part of a chord.
        /// </summary>
        public SongChordMetadata? ChordMetadata { get; set; }

        public SongNoteEvent()
        {
            Velocity = 100;
            SelectionType = NoteSelectionType.Direct;
        }
    }

    /// <summary>
    /// Describes how a note was selected during generation.
    /// </summary>
    public enum NoteSelectionType
    {
        Direct,           // Explicitly specified
        ChordTone,        // Selected from chord tones
        ScaleTone,        // Selected from scale tones
        PassingTone,      // Diatonic passing tone
        ApproachNote,     // Leading into a target note
        PatternDerived    // Derived from a pattern transformation
    }

    /// <summary>
    /// Metadata for notes that are part of a chord voicing.
    /// </summary>
    public sealed class SongChordMetadata
    {
        public string Key { get; set; } = string.Empty;
        public int Degree { get; set; }
        public string Quality { get; set; } = string.Empty;
        public string Bass { get; set; } = string.Empty;
        public int VoiceIndex { get; set; } // Position within the chord voicing
    }
}