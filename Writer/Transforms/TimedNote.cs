namespace Music.Writer
{
    /// <summary>
    /// Represents a note with explicit timing information for MIDI conversion.
    /// Delta represents ticks to wait since the previous note (0 = simultaneous).
    /// </summary>
    public sealed class TimedNote
    {
        /// <summary>
        /// Ticks to wait before playing this note (0 = play simultaneously with previous note).
        /// </summary>
        public long Delta { get; set; }

        /// <summary>
        /// MIDI note number (0-127).
        /// </summary>
        public byte NoteNumber { get; set; }

        /// <summary>
        /// Duration in ticks for this note.
        /// </summary>
        public long Duration { get; set; }

        /// <summary>
        /// Velocity (volume) for the note (0-127). Default is 100.
        /// </summary>
        public byte Velocity { get; set; } = 100;

        /// <summary>
        /// Whether this is a rest (if true, NoteNumber is ignored).
        /// </summary>
        public bool IsRest { get; set; }
    }
}