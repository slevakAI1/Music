using MusicTheory;   // https://github.com/phmatray/MusicTheory
using Music.Designer;

namespace Music.Writer
{
    /// <summary>
    /// Converts HarmonicEvent objects to lists of WriterNote compatible with Writer.
    /// Uses the MusicTheory library to generate chord voicings.
    /// </summary>
    public static class ConvertHarmonicEventToListOfPhraseNotes
    {
        /// <summary>
        /// Converts a HarmonicEvent to a list of notes representing the chord.
        /// </summary>
        /// <param name="harmonicEvent">The harmonic event containing key, degree, quality, and bass.</param>
        /// <param name="baseOctave">The base octave for the root note (default: 4).</param>
        /// <param name="noteValue">The note value (duration) for all notes in the chord.</param>
        /// <returns>A list of WriterNote objects representing the chord voicing.</returns>
        /// <exception cref="ArgumentNullException">When harmonicEvent is null.</exception>
        /// <exception cref="InvalidOperationException">When the chord cannot be constructed.</exception>
        public static List<PhraseNote> Convert(HarmonicEvent harmonicEvent, int baseOctave = 4, int noteValue = 4)
        {
            if (harmonicEvent == null)
                throw new ArgumentNullException(nameof(harmonicEvent));

            return Convert(harmonicEvent.Key, harmonicEvent.Degree, harmonicEvent.Quality, harmonicEvent.Bass, baseOctave, noteValue);
        }

        /// <summary>
        /// Converts harmonic parameters to a list of notes representing the chord.
        /// </summary>
        /// <param name="key">The key (e.g., "C major", "F# minor").</param>
        /// <param name="degree">The scale degree (1-7).</param>
        /// <param name="quality">The chord quality (e.g., "Major", "Minor7").</param>
        /// <param name="bass">The bass note option (e.g., "root", "3rd", "5th").</param>
        /// <param name="baseOctave">The base octave for the root note (default: 4).</param>
        /// <param name="noteValue">The note value (duration) for all notes in the chord.</param>
        /// <returns>A list of WriterNote objects representing the chord voicing.</returns>
        /// <exception cref="ArgumentException">When parameters are invalid.</exception>
        /// <exception cref="InvalidOperationException">When the chord cannot be constructed.</exception>
        public static List<PhraseNote> Convert(string key, int degree, string quality, string bass, int baseOctave = 4, int noteValue = 4)
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
                // We need to create a new Note with the same pitch class but at the base octave
                var chordRoot = new Note(degreeNote.Name, degreeNote.Alteration, baseOctave);
                var chord = new Chord(chordRoot, chordType);
                
                // Step 6: Get the notes in root position first
                var chordNotes = chord.GetNotes().ToList();
                
                // Step 7: Apply inversion by rotating and adjusting octaves
                var voicedNotes = ApplyVoicing(chordNotes, bass, baseOctave);
                
                // Step 8: Execute to PhraseNote format using the new constructor
                var result = new List<PhraseNote>();
                
                // Calculate note duration in ticks based on note value
                int noteDurationTicks = CalculateNoteDurationTicks(noteValue);
                
                // Create PhraseChord metadata object
                var phraseChord = new PhraseChord(
                    isChord: true,
                    chordKey: key,
                    chordDegree: degree,
                    chordQuality: quality,
                    chordBase: bass);
                
                for (int i = 0; i < voicedNotes.Count; i++)
                {
                    var note = voicedNotes[i];
                    
                    // Calculate MIDI note number
                    char step = note.Name.ToString()[0];
                    int alter = MapAlterationToAlter(note.Alteration);
                    int octave = note.Octave;
                    int noteNumber = CalculateMidiNoteNumber(step, alter, octave);
                    
                    // Create PhraseNote with MIDI properties
                    var phraseNote = new PhraseNote(
                        noteNumber: noteNumber,
                        absolutePositionTicks: 0, // Will be set by the calling code
                        noteDurationTicks: noteDurationTicks,
                        noteOnVelocity: 100,
                        isRest: false);
                    
                    // Attach chord metadata
                    phraseNote.phraseChord = phraseChord;
                    
                    result.Add(phraseNote);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to convert harmonic parameters: Key={key}, " +
                    $"Degree={degree}, Quality={quality}, " +
                    $"Bass={bass}", ex);
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
        /// Calculates note duration in ticks based on note value.
        /// Duration: 1=whole, 2=half, 4=quarter, 8=eighth, etc.
        /// </summary>
        private static int CalculateNoteDurationTicks(int noteValue)
        {
            return (MusicConstants.TicksPerQuarterNote * 4) / noteValue;
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
        /// Maps quality strings from HarmonicEditorForm to MusicTheory ChordType.
        /// </summary>
        private static ChordType MapQualityToChordType(string quality) => quality switch
        {
            // Triads
            "Major" => ChordType.Major,
            "Minor" => ChordType.Minor,
            "Diminished" => ChordType.Diminished,
            "Augmented" => ChordType.Augmented,
            "Sus2" => ChordType.Sus2,
            "Sus4" => ChordType.Sus4,
            "Power5" => ChordType.Power5,

            // 6ths
            "Major6" => ChordType.Major6,
            "Minor6" => ChordType.Minor6,
            "Major6Add9" => ChordType.Major6Add9,

            // 7ths
            "Dominant7" => ChordType.Dominant7,
            "Major7" => ChordType.Major7,
            "Minor7" => ChordType.Minor7,
            "Diminished7" => ChordType.Diminished7,
            "HalfDiminished7" => ChordType.HalfDiminished7,
            "MinorMajor7" => ChordType.MinorMajor7,

            // Extensions
            "Dominant9" => ChordType.Dominant9,
            "Major9" => ChordType.Major9,
            "Minor9" => ChordType.Minor9,
            "Dominant11" => ChordType.Dominant11,
            "Dominant13" => ChordType.Dominant13,

            // Adds
            "MajorAdd9" => ChordType.MajorAdd9,
            "MajorAdd11" => ChordType.MajorAdd11,

            _ => throw new NotSupportedException($"Chord quality '{quality}' is not supported")
        };

        /// <summary>
        /// Applies chord voicing based on bass option by rotating notes and adjusting octaves.
        /// </summary>
        /// <param name="notes">The chord notes in root position.</param>
        /// <param name="bassOption">The desired bass note (root, 3rd, 5th, 7th, etc.).</param>
        /// <param name="baseOctave">The base octave for proper voicing.</param>
        /// <returns>A list of notes with proper octave adjustments for the inversion.</returns>
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