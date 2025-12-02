using Music.Domain;
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
        public string MidiProgramName { get; set; }
        //public string NotionPartName { get; set; }

        public byte MidiProgramNumber { get; set; }

        public List<PhraseNote> PhraseNotes { get; set; } = new();

        public Phrase(string midiProgramName, List<PhraseNote> phraseNotes)
        {
            MidiProgramName = midiProgramName;
            PhraseNotes = phraseNotes;

            // Resolve MIDI program number from the instrument name
            var instrument = MidiInstrument.GetGeneralMidiInstruments()
                .FirstOrDefault(i => i.Name.Equals(midiProgramName, StringComparison.OrdinalIgnoreCase));
            
            MidiProgramNumber = instrument?.ProgramNumber ?? 0; // Default to 0 (Acoustic Grand Piano) if not found
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
        public bool IsRest { get; set; }

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
        public int Duration { get; set; }
        public int Dots { get; set; }

        // ... maybe need a tuplet class?
        public int? TupletActualNotes { get; set; }  // The 'm' in m:n (e.g., 3 in a triplet)
        public int? TupletNormalNotes { get; set; }  // The 'n' in m:n (e.g., 2 in a triplet)
        public PhraseChord? phraseChord { get; set; } // Metadata only. Means the note is part of this type chord.

        public PhraseNote(
            int noteNumber,
            int absolutePositionTicks,
            int noteDurationTicks,
            int noteOnVelocity = 100,
            bool isRest = false)
        {
            NoteNumber = noteNumber;
            AbsolutePositionTicks = absolutePositionTicks;
            NoteDurationTicks = noteDurationTicks;
            NoteOnVelocity = noteOnVelocity;
            IsRest = isRest;

            // Compute the pitch and rhythm properties here
            // TBD
        }
    }
}