namespace Music.Generator
{
    /// <summary>
    /// An instance of a pattern placed at a specific location in the song.
    /// Tracks transformations applied and enables pattern reuse analysis.
    /// </summary>
    public sealed class ProposedPatternInstance
    {
        /// <summary>
        /// Unique identifier for this instance.
        /// </summary>
        public string InstanceId { get; init; }

        /// <summary>
        /// Reference to the source pattern.
        /// </summary>
        public string SourcePatternId { get; set; }

        /// <summary>
        /// Bar where this instance starts (1-based).
        /// </summary>
        public int StartBar { get; set; }

        /// <summary>
        /// Transformations applied to this instance.
        /// </summary>
        public PatternTransformations Transformations { get; set; }

        /// <summary>
        /// IDs of the note events generated from this instance.
        /// </summary>
        public List<long> GeneratedNoteEventIds { get; set; }

        public ProposedPatternInstance()
        {
            InstanceId = Guid.NewGuid().ToString("N");
            SourcePatternId = string.Empty;
            StartBar = 1;
            Transformations = new PatternTransformations();
            GeneratedNoteEventIds = new List<long>();
        }
    }

    /// <summary>
    /// Transformations that can be applied when instantiating a pattern.
    /// </summary>
    public sealed class PatternTransformations
    {
        /// <summary>
        /// Transposition in semitones (for absolute pitch patterns).
        /// </summary>
        public int TransposeSemitones { get; set; }

        /// <summary>
        /// Octave shift.
        /// </summary>
        public int OctaveShift { get; set; }

        /// <summary>
        /// Rhythmic displacement in beats.
        /// </summary>
        public decimal RhythmicDisplacement { get; set; }

        /// <summary>
        /// Time stretch factor (1.0 = original, 2.0 = double duration).
        /// </summary>
        public double TimeStretch { get; set; }

        /// <summary>
        /// Velocity scale factor (1.0 = original).
        /// </summary>
        public double VelocityScale { get; set; }

        /// <summary>
        /// Whether to retrograde (reverse) the pattern.
        /// </summary>
        public bool Retrograde { get; set; }

        /// <summary>
        /// Whether to invert the melodic contour.
        /// </summary>
        public bool Invert { get; set; }

        /// <summary>
        /// Custom ornamentation applied.
        /// </summary>
        public OrnamentationType Ornamentation { get; set; }

        public PatternTransformations()
        {
            TimeStretch = 1.0;
            VelocityScale = 1.0;
        }
    }

    /// <summary>
    /// Types of ornamentation that can be applied.
    /// </summary>
    public enum OrnamentationType
    {
        None,
        GraceNotes,
        Trill,
        Mordent,
        Turn,
        Glissando
    }
}