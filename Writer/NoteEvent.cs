namespace Music.Writer
{
    /// <summary>
    /// Represents a note or chord to be written into a measure (may cross measures)
    /// 
    /// 
    /// 
    /// 
    ///    BIG MISSING FUNCTIONALITY - overlapping notes within a phrase/track/part.
    ///       Each note should be represented as a start position, not just starting when
    ///       the last note ends!
    ///    
    ///    Needed for midi for overlapping notes
    /// 
    ///    This affects musicxml (in the case of overlapping notes, that are not straight 
    ///    chords with same start and end) - doesnt handle that yet
    ///    
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




        // In ticks 480/quarter note is the standard
        // Required to handle overlapping notes
        public int absoluteStartPosition { get; set; }  





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