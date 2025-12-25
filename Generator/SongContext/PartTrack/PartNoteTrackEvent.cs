//using Music.Writer;

///// Represents a single note within a song track note event, including pitch, timing, and velocity.
///// 
///// PartTrackNoteEvent is the atomic unit for musical playback and notation, capturing all necessary
///// information for MIDI conversion. It supports both direct note entry and notes
///// generated from chords, enabling precise control over musical expression and timing.
///// </summary>
//public sealed class PartTrackNoteEvent
//{
//    // MIDI-related.  480 ticks / quarter note is standard
//    public int NoteNumber { get; set; } // note volume
//    public int AbsolutePositionTicks { get; set; } //  note start
//    public int NoteDurationTicks { get; set; } // note length
//    public int NoteOnVelocity { get; set; } = 100; // note volume

//    // Metadata fields - can be used for display purposes. Also used by musicxml.

//    // Pitch
//    public char Step { get; set; }
//    public int Alter { get; set; }
//    public int Octave { get; set; }

//    // Rhythm
//    public int Duration { get; set; } //  4=quarter, 8=eighth note, etc.
//    public int Dots { get; set; }

//    public int? TupletActualNotes { get; set; }  // The 'm' in m:n (e.g., 3 in a triplet)
//    public int? TupletNormalNotes { get; set; }  // The 'n' in m:n (e.g., 2 in a triplet)

//    public PartTrackNoteEvent(
//        int noteNumber,
//        int absolutePositionTicks,
//        int noteDurationTicks,
//        int noteOnVelocity = 100)
//    {
//        NoteNumber = noteNumber;
//        AbsolutePositionTicks = absolutePositionTicks;
//        NoteDurationTicks = noteDurationTicks;
//        NoteOnVelocity = noteOnVelocity;

//        // Calculate metadata fields from MIDI properties
//        (Step, Alter, Octave) = MusicCalculations.CalculatePitch(noteNumber);
//        (Duration, Dots, TupletActualNotes, TupletNormalNotes) = MusicCalculations.CalculateRhythm(noteDurationTicks);
//    }
//}
