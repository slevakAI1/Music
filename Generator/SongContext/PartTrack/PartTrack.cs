using Music.Writer;

namespace Music.Generator
{
    /// <summary>
    /// Represents a single part/track for composition and MIDI generation.
    /// 
    /// PartTrack encapsulates a sequence of musical events (notes, chords, rests) for a single instrument or part.
    /// It is designed to support flexible music writing, including overlapping notes and chords, and serves as the
    /// primary input for transformations into timed notes and MIDI events. This abstraction enables composers and
    /// algorithms to work with high-level musical ideas before rendering them into concrete playback or notation.
    /// </summary>
    public sealed class PartTrack
    {
        // THESE GET SET BY GRID DROPDOWN CHANGE EVENT, WHAT ABOUT DEFAULT?
        public string MidiProgramName { get; set; }
        //public string NotionPartName { get; set; }
        public int MidiProgramNumber { get; set; }

        public List<PartTrackNoteEvent> PartTrackNoteEvents { get; set; } = new();

        public PartTrack(List<PartTrackNoteEvent> songTrackNoteEvent)
        {
            PartTrackNoteEvents = songTrackNoteEvent;
        }
    }

    /// <summary>
    /// Encapsulates the intent and structure of a chord within a song track.
    /// 
    /// SongTrackChord provides a high-level, declarative representation of a chord, including its key, degree, quality, and voicing type.
    /// This abstraction allows composers and algorithms to specify harmony content without committing to specific notes,
    /// enabling flexible rendering, transposition, and arrangement. It is primarily used to generate the actual notes of a chord
    /// during MIDI or notation conversion, supporting expressive and reusable harmonic patterns in music composition.
    /// </summary>
    /// 

    // TO DO - MED - NOT USED REMOVE

    //public sealed class SongTrackChord
    //{
    //    // Chords
    //    public bool IsChord { get; set; }
    //    public string? ChordKey { get; set; }
    //    public int? ChordDegree { get; set; }
    //    public string? ChordQuality { get; set; } // list from musictheory chordtype i think
    //    public string? ChordBase { get; set; }
    //    public string? ChordType { get; set; } = "Straight"; // Just an idea. Straight, arppegiated, etc ???

    //    public SongTrackChord(
    //        bool isChord,
    //        string? chordKey = null,
    //        int? chordDegree = null,
    //        string? chordQuality = null,
    //        string? chordBase = null,
    //        string? chordType = "Straight")
    //    {
    //        IsChord = isChord;
    //        ChordKey = chordKey;
    //        ChordDegree = chordDegree;
    //        ChordQuality = chordQuality;
    //        ChordBase = chordBase;
    //        ChordType = chordType;
    //    }
    //}

    /// <summary>
    /// Represents a single note within a song track note event, including pitch, timing, and velocity.
    /// 
    /// PartTrackNoteEvent is the atomic unit for musical playback and notation, capturing all necessary
    /// information for MIDI conversion. It supports both direct note entry and notes
    /// generated from chords, enabling precise control over musical expression and timing.
    /// </summary>
    public sealed class PartTrackNoteEvent
    {
        // MIDI-related.  480 ticks / quarter note is standard
        public int NoteNumber { get; set; } // note volume
        public int AbsolutePositionTicks { get; set; } //  note start
        public int NoteDurationTicks { get; set; } // note length
        public int NoteOnVelocity { get; set; } = 100; // note volume

        // Metadata fields - can be used for display purposes. Also used by musicxml.

        // Pitch
        public char Step { get; set; }
        public int Alter { get; set; }
        public int Octave { get; set; }

        // Rhythm
        public int Duration { get; set; } //  4=quarter, 8=eighth note, etc.
        public int Dots { get; set; }

        public int? TupletActualNotes { get; set; }  // The 'm' in m:n (e.g., 3 in a triplet)
        public int? TupletNormalNotes { get; set; }  // The 'n' in m:n (e.g., 2 in a triplet)

        public PartTrackNoteEvent(
            int noteNumber,
            int absolutePositionTicks,
            int noteDurationTicks,
            int noteOnVelocity = 100)
        {
            NoteNumber = noteNumber;
            AbsolutePositionTicks = absolutePositionTicks;
            NoteDurationTicks = noteDurationTicks;
            NoteOnVelocity = noteOnVelocity;

            // Calculate metadata fields from MIDI properties
            (Step, Alter, Octave) = MusicCalculations.CalculatePitch(noteNumber);
            (Duration, Dots, TupletActualNotes, TupletNormalNotes) = MusicCalculations.CalculateRhythm(noteDurationTicks);
        }
    }
}