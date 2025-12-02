using MusicTheory;

namespace Music.Writer
{
    /// <summary>
    /// Represents a musical phrase for composition and MIDI generation.
    /// 
    /// A Phrase encapsulates a sequence of musical events (notes, chords, rests) for a single instrument or part.
    /// It is designed to support flexible music writing, including overlapping notes and chords, and serves as the
    /// primary input for transformations into timed notes and MIDI events. This abstraction enables composers and
    /// algorithms to work with high-level musical ideas before rendering them into concrete playback or notation.
    /// </summary>
    public sealed class Phrase
    {
        public string MidiPartName { get; set; }
        //public string NotionPartName { get; set; }
        public byte MidiProgramNumber { get; set; }
        public List<PhraseEvent> PhraseEvents { get; set; } = new();

        public Phrase(string midiPartName, byte midiProgramNumber, List<PhraseEvent>? phraseEvents = null)
        {
            MidiPartName = midiPartName;
            MidiProgramNumber = midiProgramNumber;
            PhraseEvents = phraseEvents ?? new List<PhraseEvent>();
        }
    }

    /// <summary>
    /// Describes a single musical event within a phrase, such as a note, chord, or rest.
    /// 
    /// PhraseEvent provides detailed properties for rhythm, chord structure, and note grouping, allowing
    /// for expressive and complex musical constructs. It enables the representation of both simple notes
    /// and advanced chords, supporting accurate conversion to timed notes and MIDI playback.
    /// </summary>
    public sealed class PhraseEvent
    {
        public bool IsRest { get; set; }

        // Rhythm Tuplet
        public string? TupletNumber { get; set; }   // for musicxml only
        public int? TupletActualNotes { get; set; }  // The 'm' in m:n (e.g., 3 in a triplet)
        public int? TupletNormalNotes { get; set; }  // The 'n' in m:n (e.g., 2 in a triplet)

        // Chords
        public PhraseChord? PhraseChord { get; set; }
        public List<PhraseNote>? PhraseNotes { get; set; }

        // public other - pitch bend, etc...future

        public PhraseEvent(
            bool isRest,
            string? tupletNumber = null,
            int? tupletActualNotes = null,
            int? tupletNormalNotes = null,
            PhraseChord? phraseChord = null,
            List<PhraseNote>? phraseNotes = null)
        {
            IsRest = isRest;
            TupletNumber = tupletNumber;
            TupletActualNotes = tupletActualNotes;
            TupletNormalNotes = tupletNormalNotes;
            PhraseChord = phraseChord;
            PhraseNotes = phraseNotes;
        }
    }

    /// <summary>
    /// Encapsulates the intent and structure of a chord within a musical phrase.
    /// 
    /// PhraseChord provides a high-level, declarative representation of a chord, including its key, degree, quality, and voicing type.
    /// This abstraction allows composers and algorithms to specify harmonic content without committing to specific notes,
    /// enabling flexible rendering, transposition, and arrangement. It is primarily used to generate the actual notes of a chord
    /// during MIDI or notation conversion, supporting expressive and reusable harmonic patterns in music composition.
    /// </summary>
    public sealed class PhraseChord
    {
        // Chords
        public bool IsChord { get; set; }
        public string? ChordKey { get; set; }
        public int? ChordDegree { get; set; }
        public string? ChordQuality { get; set; } // list from musictheory chordtype i think
        public string? ChordBase { get; set; }
        public string? ChordType { get; set; } = "Straight"; // Just an idea. Straight, arppegiated, etc ???

        public PhraseChord(
            bool isChord,
            string? chordKey = null,
            int? chordDegree = null,
            string? chordQuality = null,
            string? chordBase = null,
            string? chordType = "Straight")
        {
            IsChord = isChord;
            ChordKey = chordKey;
            ChordDegree = chordDegree;
            ChordQuality = chordQuality;
            ChordBase = chordBase;
            ChordType = chordType;
        }
    }

    /// <summary>
    /// Represents a single note within a phrase event, including pitch, timing, and velocity.
    /// 
    /// PhraseNote is the atomic unit for musical playback and notation, capturing all necessary
    /// information for MIDI and MusicXML conversion. It supports both direct note entry and notes
    /// generated from chords, enabling precise control over musical expression and timing.
    /// </summary>
    public sealed class PhraseNote
    {
        // MIDI-related.  480 ticks / quarter note is standard
        public int NoteNumber { get; set; } // note volume
        public int AbsolutePositionTicks { get; set; } //  note start
        public int NoteDurationTicks { get; set; } // note length
        public int NoteOnVelocity { get; set; } = 100; // note volume

        // Calculated fields - can be used for display purposes. Also used by musicxml.

        // Pitch
        public char Step { get; set; }
        public int Alter { get; set; }
        public int Octave { get; set; }

        // Rhythm
        public int Duration { get; set; }
        public int Dots { get; set; }

        public PhraseNote(
            int noteNumber,
            int absolutePositionTicks,
            int noteDurationTicks,
            int noteOnVelocity = 100,
            char step = '\0',
            int alter = 0,
            int octave = 0,
            int duration = 0,
            int dots = 0)
        {
            NoteNumber = noteNumber;
            AbsolutePositionTicks = absolutePositionTicks;
            NoteDurationTicks = noteDurationTicks;
            NoteOnVelocity = noteOnVelocity;
            Step = step;
            Alter = alter;
            Octave = octave;
            Duration = duration;
            Dots = dots;
        }
    }
}