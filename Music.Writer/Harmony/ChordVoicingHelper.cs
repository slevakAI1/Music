using MusicTheory;
using Music.Designer;

namespace Music.Writer
{
    /// <summary>
    /// Internal helper for generating chord voicings from harmony parameters.
    /// Shared by conversion and pitch context systems.
    /// </summary>
    internal static class ChordVoicingHelper
    {
        /// <summary>
        /// Generates chord voicing as MIDI note numbers from harmony parameters.
        /// </summary>
        /// <param name="key">The key (e.g., "C major", "F# minor")</param>
        /// <param name="degree">The scale degree (1-7)</param>
        /// <param name="quality">The chord quality (e.g., "Major", "Minor7")</param>
        /// <param name="bass">The bass note option (e.g., "root", "3rd", "5th")</param>
        /// <param name="baseOctave">The base octave for chord voicing</param>
        /// <returns>List of MIDI note numbers representing the chord</returns>
        /// <exception cref="ArgumentException">When parameters are invalid</exception>
        /// <exception cref="InvalidOperationException">When chord cannot be constructed</exception>
        public static List<int> GenerateChordMidiNotes(
            string key,
            int degree,
            string quality,
            string bass,
            int baseOctave)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (string.IsNullOrWhiteSpace(quality))
                throw new ArgumentException("Quality cannot be null or empty", nameof(quality));
            if (string.IsNullOrWhiteSpace(bass))
                throw new ArgumentException("Bass cannot be null or empty", nameof(bass));

            try
            {
                // Step 1: Parse the key to get the root note and scale
                var (keyRoot, keyMode) = ParseKey(key);
                
                // Step 2: Build the scale for the key
                var scaleType = keyMode.Equals("major", StringComparison.OrdinalIgnoreCase) 
                    ? ScaleType.Major 
                    : ScaleType.NaturalMinor;
                var scale = new Scale(keyRoot, scaleType);
                
                // Step 3: Get the chord root note based on the degree (1-7)
                var scaleNotes = scale.GetNotes().ToList();
                if (degree < 1 || degree > 7)
                    throw new InvalidOperationException($"Degree must be 1-7, got {degree}");
                
                var degreeNote = scaleNotes[degree - 1];
                
                // Step 4: Map quality string to ChordType
                var chordType = MapQualityToChordType(quality);
                
                // Step 5: Create the chord using the degree note's pitch class with the specified quality
                var chordRoot = new Note(degreeNote.Name, degreeNote.Alteration, baseOctave);
                var chord = new Chord(chordRoot, chordType);
                
                // Step 6: Get the notes in root position first
                var chordNotes = chord.GetNotes().ToList();
                
                // Step 7: Apply inversion by rotating and adjusting octaves
                var voicedNotes = ApplyVoicing(chordNotes, bass, baseOctave);
                
                // Step 8: Convert to MIDI note numbers
                var midiNotes = new List<int>();
                foreach (var note in voicedNotes)
                {
                    char step = note.Name.ToString()[0];
                    int alter = MapAlterationToAlter(note.Alteration);
                    int octave = note.Octave;
                    int noteNumber = CalculateMidiNoteNumber(step, alter, octave);
                    midiNotes.Add(noteNumber);
                }
                
                return midiNotes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to generate chord voicing: Key={key}, " +
                    $"Degree={degree}, Quality={quality}, Bass={bass}", ex);
            }
        }

        /// <summary>
        /// Calculates MIDI note number from note properties.
        /// </summary>
        private static int CalculateMidiNoteNumber(char step, int alter, int octave)
        {
            var baseNote = char.ToUpper(step) switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => 0
            };
            return (octave + 1) * 12 + baseNote + alter;
        }

        /// <summary>
        /// Parses a key string like "C major" or "F# minor" into root note and mode.
        /// </summary>
        private static (Note root, string mode) ParseKey(string keyString)
        {
            if (string.IsNullOrWhiteSpace(keyString))
                throw new ArgumentException("Key string cannot be empty", nameof(keyString));

            var parts = keyString.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid key format: '{keyString}'. Expected 'Note mode'", nameof(keyString));

            var noteStr = parts[0];
            var mode = parts[1];

            // Parse the note (e.g., "C", "F#", "Bb")
            NoteName noteName;
            Alteration alteration = Alteration.Natural;

            if (noteStr.Length == 1)
            {
                noteName = ParseNoteName(noteStr[0]);
            }
            else if (noteStr.Length == 2)
            {
                noteName = ParseNoteName(noteStr[0]);
                alteration = noteStr[1] switch
                {
                    '#' => Alteration.Sharp,
                    'b' => Alteration.Flat,
                    _ => throw new ArgumentException($"Invalid accidental: {noteStr[1]}")
                };
            }
            else
            {
                throw new ArgumentException($"Invalid note format: '{noteStr}'");
            }

            return (new Note(noteName, alteration, 4), mode);
        }

        /// <summary>
        /// Parses a character to NoteName.
        /// </summary>
        private static NoteName ParseNoteName(char c) => char.ToUpper(c) switch
        {
            'C' => NoteName.C,
            'D' => NoteName.D,
            'E' => NoteName.E,
            'F' => NoteName.F,
            'G' => NoteName.G,
            'A' => NoteName.A,
            'B' => NoteName.B,
            _ => throw new ArgumentException($"Invalid note name: {c}")
        };

        /// <summary>
        /// Maps quality strings from HarmonyEditorForm to MusicTheory ChordType.
        /// Handles normalization of quality strings used in HarmonyEvent.
        /// </summary>
        private static ChordType MapQualityToChordType(string quality)
        {
            // Normalize: trim and handle variations
            var normalized = quality?.Trim().ToLowerInvariant() ?? string.Empty;

            return normalized switch
            {
                // Triads
                "maj" or "major" => ChordType.Major,
                "min" or "minor" => ChordType.Minor,
                "dim" or "diminished" => ChordType.Diminished,
                "aug" or "augmented" => ChordType.Augmented,
                "sus2" => ChordType.Sus2,
                "sus4" => ChordType.Sus4,
                "5" or "power5" => ChordType.Power5,

                // 6ths
                "maj6" or "major6" => ChordType.Major6,
                "min6" or "minor6" => ChordType.Minor6,
                "6/9" or "major6add9" => ChordType.Major6Add9,

                // 7ths
                "dom7" or "dominant7" => ChordType.Dominant7,
                "maj7" or "major7" => ChordType.Major7,
                "min7" or "minor7" => ChordType.Minor7,
                "dim7" or "diminished7" => ChordType.Diminished7,
                "hdim7" or "halfdiminished7" => ChordType.HalfDiminished7,
                "minmaj7" or "minormajor7" => ChordType.MinorMajor7,

                // Extensions
                "9" or "dominant9" => ChordType.Dominant9,
                "maj9" or "major9" => ChordType.Major9,
                "min9" or "minor9" => ChordType.Minor9,
                "11" or "dominant11" => ChordType.Dominant11,
                "13" or "dominant13" => ChordType.Dominant13,

                // Adds
                "add9" or "majoradd9" => ChordType.MajorAdd9,
                "add11" or "majoradd11" => ChordType.MajorAdd11,
                "add13" => throw new NotSupportedException($"Chord quality 'add13' is not currently supported by MusicTheory library"),

                _ => throw new NotSupportedException($"Chord quality '{quality}' is not supported. Valid values include: maj, min, dim, aug, sus2, sus4, 5, maj6, min6, 6/9, dom7, maj7, min7, dim7, hdim7, minMaj7, 9, maj9, min9, 11, 13, add9, add11")
            };
        }

        /// <summary>
        /// Applies chord voicing based on bass option by rotating notes and adjusting octaves.
        /// </summary>
        private static List<Note> ApplyVoicing(List<Note> notes, string bassOption, int baseOctave)
        {
            if (notes == null || notes.Count == 0)
                return notes;

            var bassIndex = bassOption.ToLowerInvariant() switch
            {
                "root" => 0,
                "3rd" => 1,
                "5th" => 2,
                "7th" => 3,
                "9th" => 4,
                "11th" => 5,
                "13th" => 6,
                _ => throw new NotSupportedException($"Bass option '{bassOption}' is not supported")
            };

            // Validate index is within chord range
            if (bassIndex >= notes.Count)
            {
                // For inversions beyond the chord tones available, just return root position
                return notes;
            }

            if (bassIndex == 0)
            {
                // Root position - no changes needed
                return notes;
            }

            // Create a new list with proper voicing
            var voicedNotes = new List<Note>();
            
            // The bass note and all notes after it stay in the base octave
            for (int i = bassIndex; i < notes.Count; i++)
            {
                voicedNotes.Add(new Note(notes[i].Name, notes[i].Alteration, baseOctave));
            }
            
            // Notes before the bass note move up an octave
            for (int i = 0; i < bassIndex; i++)
            {
                voicedNotes.Add(new Note(notes[i].Name, notes[i].Alteration, baseOctave + 1));
            }

            return voicedNotes;
        }

        /// <summary>
        /// Maps MusicTheory Alteration to MusicXML alter value.
        /// </summary>
        private static int MapAlterationToAlter(Alteration alteration) => alteration switch
        {
            Alteration.Natural => 0,
            Alteration.Sharp => 1,
            Alteration.Flat => -1,
            Alteration.DoubleSharp => 2,
            Alteration.DoubleFlat => -2,
            _ => 0
        };
    }
}