using MusicTheory;

namespace Music.Writer
{
    /// <summary>
    /// Represents a note or chord to be written into a measure (may cross measures)
    /// 
    ///    This affects musicxml (in the case of overlapping notes, that are not straight 
    ///    chords with same start and end) - doesnt handle that yet
    ///    
    /// </summary>
    


    // Phrase --> PhraseEvents (note, rest, chord/info) --> PhraseNotes


    public sealed class PhraseNote
    {

        // MIDI-related.  480 ticks / quarter note is standard

        public int AbsolutePositionTicks { get; set; } //  note start
        public int NoteDurationTicks { get; set; } // note length
        public int NoteOnVelocity { get; set; } // note volume
        public int NoteNumber { get; set; } // note volume

        // public other - pitch bend, etc...


        // Metadata / Calculated - used by music xml code still

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


        // Chords
        public Guid ChordId { get; set; } = Guid.Empty;  // To associate chord notes
        public bool IsChord { get; set; }




        // Seems like this may not belong in a single note record
        public ChordType ChordType { get; set; } // Straight, arppegiated, etc ???
        public string ChordKey { get; set; }
        public int? ChordDegree { get; set; }
        public string? ChordQuality { get; set; }
        public string? ChordBase { get; set; }
    }
}