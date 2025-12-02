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
        public int Duration { get; set; } //  4=quarter, 8=eighth note, etc.
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

            // Calculate metadata fields from MIDI properties
            if (!isRest)
            {
                (Step, Alter, Octave) = CalculatePitch(noteNumber);
            }
            (Duration, Dots) = CalculateRhythm(noteDurationTicks);
        }

        /// <summary>
        /// Calculates the pitch properties (Step, Alter, Octave) from MIDI note number.
        /// </summary>
        private static (char Step, int Alter, int Octave) CalculatePitch(int noteNumber)
        {
            int octave = (noteNumber / 12) - 1;
            int pitchClass = noteNumber % 12;

            return pitchClass switch
            {
                0 => ('C', 0, octave),
                1 => ('C', 1, octave),  // C#
                2 => ('D', 0, octave),
                3 => ('D', 1, octave),  // D#
                4 => ('E', 0, octave),
                5 => ('F', 0, octave),
                6 => ('F', 1, octave),  // F#
                7 => ('G', 0, octave),
                8 => ('G', 1, octave),  // G#
                9 => ('A', 0, octave),
                10 => ('A', 1, octave), // A#
                11 => ('B', 0, octave),
                _ => ('C', 0, octave)
            };
        }

        /// <summary>
        /// Calculates the rhythm properties (Duration, Dots) from note duration in ticks.
        /// Assumes 480 ticks per quarter note.
        /// </summary>
        private static (int Duration, int Dots) CalculateRhythm(int noteDurationTicks)
        {
            const int ticksPerQuarterNote = 480;
            
            // Common note durations in ticks (480 ticks per quarter note)
            var noteDurations = new (int ticks, int duration, int dots)[]
            {
                // Whole notes
                (1920, 1, 0),  // Whole note
                (2880, 1, 1),  // Dotted whole note
                
                // Half notes
                (960, 2, 0),   // Half note
                (1440, 2, 1),  // Dotted half note
                (2160, 2, 2),  // Double dotted half note
                
                // Quarter notes
                (480, 4, 0),   // Quarter note
                (720, 4, 1),   // Dotted quarter note
                (1080, 4, 2),  // Double dotted quarter note
                
                // Eighth notes
                (240, 8, 0),   // Eighth note
                (360, 8, 1),   // Dotted eighth note
                (540, 8, 2),   // Double dotted eighth note
                
                // Sixteenth notes
                (120, 16, 0),  // Sixteenth note
                (180, 16, 1),  // Dotted sixteenth note
                (270, 16, 2),  // Double dotted sixteenth note
                
                // Thirty-second notes
                (60, 32, 0),   // Thirty-second note
                (90, 32, 1),   // Dotted thirty-second note
                
                // Sixty-fourth notes
                (30, 64, 0),   // Sixty-fourth note
            };

            // Find the closest match
            foreach (var (ticks, duration1, dots) in noteDurations)
            {
                if (noteDurationTicks == ticks)
                {
                    return (duration1, dots);
                }
            }

            // If no exact match, calculate base duration without dots
            int wholeTicks = ticksPerQuarterNote * 4;
            if (noteDurationTicks >= wholeTicks)
            {
                return (1, 0); // Default to whole note
            }

            // Calculate the closest power of 2 duration
            int duration = 1;
            int closestTicks = wholeTicks;
            
            while (closestTicks > noteDurationTicks && duration < 64)
            {
                duration *= 2;
                closestTicks /= 2;
            }

            return (duration, 0);
        }
    }
}