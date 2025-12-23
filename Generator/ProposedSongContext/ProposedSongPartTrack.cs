

// TO DO - What about drum set - pseudo midi number 255? Is it handled/allowed here?


namespace Music.Generator
{
    /// <summary>
    /// A single instrument/voice track within a song.
    /// Contains all note events and references to patterns used.
    /// </summary>
    public sealed class ProposedSongPartTrack
    {
        /// <summary>
        /// Unique identifier for this track.
        /// </summary>
        public string TrackId { get; init; }

        /// <summary>
        /// The groove role this track fulfills (e.g., "Bass", "DrumKit", "Comp", "Pads").
        /// </summary>
        public string GrooveRole { get; set; }

        /// <summary>
        /// MIDI program name for playback.
        /// </summary>
        public string MidiProgramName { get; set; }

        /// <summary>
        /// MIDI program number (0-127, or 255 for drums).
        /// </summary>
        public int MidiProgramNumber { get; set; }

        /// <summary>
        /// MIDI channel (1-16, typically 10 for drums).
        /// </summary>
        public int MidiChannel { get; set; }

        /// <summary>
        /// All note events in this track, ordered by absolute position.
        /// </summary>
        public List<ProposedSongNoteEvent> NoteEvents { get; set; }

        /// <summary>
        /// ProposedPattern instances used in this track (for analysis and regeneration).
        /// </summary>
        public List<ProposedPatternInstance> PatternInstances { get; set; }

        /// <summary>
        /// Register constraints for this part.
        /// </summary>
        public RegisterConstraint RegisterConstraint { get; set; }

        public ProposedSongPartTrack()
        {
            TrackId = Guid.NewGuid().ToString("N");
            GrooveRole = string.Empty;
            MidiProgramName = string.Empty;
            NoteEvents = new List<ProposedSongNoteEvent>();
            PatternInstances = new List<ProposedPatternInstance>();
            RegisterConstraint = new RegisterConstraint();
        }
    }

    /// <summary>
    /// Defines the pitch range constraints for a part.
    /// </summary>
    public sealed class RegisterConstraint
    {
        /// <summary>
        /// Minimum MIDI note number for this part.
        /// </summary>
        public int MinMidiNote { get; set; }

        /// <summary>
        /// Maximum MIDI note number for this part.
        /// </summary>
        public int MaxMidiNote { get; set; }

        /// <summary>
        /// Preferred center MIDI note (for pitch selection).
        /// </summary>
        public int PreferredCenter { get; set; }

        public RegisterConstraint()
        {
            MinMidiNote = 0;
            MaxMidiNote = 127;
            PreferredCenter = 60;
        }
    }
}