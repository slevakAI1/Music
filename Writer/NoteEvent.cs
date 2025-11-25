namespace Music.Writer
{
    /// <summary>
    /// Represents a note or chord to be written into a measure (may cross measures)
    /// </summary>
    public sealed class NoteEvent
    {

        // Pitch
        public char Step { get; set; }
        public int Alter { get; set; }
        public int Octave { get; set; }
        public bool IsRest { get; set; }

        // Rhythm
        public int Duration { get; set; }
        public int Dots { get; set; }

        // Rhythm Tuplet
        public string? TupletNumber { get; set; }
        public int TupletActualNotes { get; set; }  // The 'm' in m:n (e.g., 3 in a triplet)
        public int TupletNormalNotes { get; set; }  // The 'n' in m:n (e.g., 2 in a triplet)

        // Chord Options
        public bool IsChord { get; set; }
        public string ChordKey { get; set; }
        public int? ChordDegree { get; set; }
        public string? ChordQuality { get; set; }
        public string? ChordBase { get; set; }
    }
}