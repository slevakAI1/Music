using MusicTheory;   // https://github.com/phmatray/MusicTheory
using Music.Designer;

/*
Known Limitations and Unsupported Features
Based on my analysis of the MusicTheory library and your requirements, here are the exceptions and unsupported features:
✅ Fully Supported Qualities:
•	Triads: maj, min, dim, aug, sus2, sus4, 5
•	6ths: maj6, min6, 6/9
•	7ths: dom7, maj7, min7, dim7, hdim7, minMaj7
•	Extensions: 9, maj9, min9, 11, 13
•	Adds: add9, add11
❌ Unsupported/Partially Supported:
1.	add13 - MusicTheory doesn't have a direct ChordType for this
2.	minAdd9 - MusicTheory doesn't have a direct ChordType for this
3.	Bass inversions 9th, 11th, 13th - MusicTheory's ChordInversion enum only supports up to Third (for 7th in bass). Higher inversions aren't directly supported, so these default to root position.
🔧 Additional Limitations:
4.	Double sharps/flats - Mapped to single sharp/flat in the output
5.	All Keys Supported - The key parser handles all 30 keys you've listed (15 major + 15 minor)
 */

namespace Music.Generator
{
    /// <summary>
    /// Converts HarmonicEvent objects to lists of notes compatible with GeneratorData.
    /// Uses the MusicTheory library to generate chord voicings.
    /// </summary>
    public static class HarmonicChordConverter
    {
        /// <summary>
        /// Represents a single note with pitch information for use in GeneratorData.
        /// </summary>
        public sealed class ChordNote
        {
            public char Step { get; init; }           // C, D, E, F, G, A, B
            public string Accidental { get; init; }   // "Natural", "Sharp", "Flat"
            public int Octave { get; init; }          // Scientific pitch notation
        }

        /// <summary>
        /// Converts a HarmonicEvent to a list of notes representing the chord.
        /// </summary>
        /// <param name="harmonicEvent">The harmonic event containing key, degree, quality, and bass.</param>
        /// <param name="baseOctave">The base octave for the root note (default: 4).</param>
        /// <returns>A list of ChordNote objects representing the chord voicing.</returns>
        /// <exception cref="ArgumentNullException">When harmonicEvent is null.</exception>
        /// <exception cref="InvalidOperationException">When the chord cannot be constructed.</exception>
        public static List<ChordNote> Convert(HarmonicEvent harmonicEvent, int baseOctave = 4)
        {
            if (harmonicEvent == null)
                throw new ArgumentNullException(nameof(harmonicEvent));

            try
            {
                // Step 1: Parse the key to get the root note and scale
                var (keyRoot, keyMode) = ParseKey(harmonicEvent.Key);
                
                // Step 2: Build the scale for the key
                var scaleType = keyMode.Equals("major", StringComparison.OrdinalIgnoreCase) 
                    ? ScaleType.Major 
                    : ScaleType.NaturalMinor;
                var scale = new Scale(keyRoot, scaleType);
                
                // Step 3: Get the chord root note based on the degree (1-7)
                var scaleNotes = scale.GetNotes().ToList();
                if (harmonicEvent.Degree < 1 || harmonicEvent.Degree > 7)
                    throw new InvalidOperationException($"Degree must be 1-7, got {harmonicEvent.Degree}");
                
                var chordRoot = scaleNotes[harmonicEvent.Degree - 1];
                
                // Step 4: Map quality string to ChordType
                var chordType = MapQualityToChordType(harmonicEvent.Quality);
                
                // Step 5: Create the chord
                var chord = new Chord(chordRoot, chordType);
                
                // Step 6: Apply inversion based on bass
                var invertedChord = ApplyBassInversion(chord, harmonicEvent.Bass);
                
                // Step 7: Get the notes and convert to ChordNote format
                var chordNotes = invertedChord.GetNotes().ToList();
                var result = new List<ChordNote>();
                
                foreach (var note in chordNotes)
                {
                    result.Add(new ChordNote
                    {
                        Step = note.Name.ToString()[0],
                        Accidental = MapAlteration(note.Alteration),
                        Octave = note.Octave
                    });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to convert HarmonicEvent: Key={harmonicEvent.Key}, " +
                    $"Degree={harmonicEvent.Degree}, Quality={harmonicEvent.Quality}, " +
                    $"Bass={harmonicEvent.Bass}", ex);
            }
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
        /// Applies chord inversion based on bass option.
        /// </summary>
        private static Chord ApplyBassInversion(Chord chord, string bassOption) => bassOption.ToLowerInvariant() switch
        {
            "root" => chord,
            "3rd" => chord.WithInversion(ChordInversion.First),
            "5th" => chord.WithInversion(ChordInversion.Second),
            "7th" => chord.WithInversion(ChordInversion.Third),
            // For extensions beyond 7th, we can't directly invert via ChordInversion enum
            // MusicTheory library doesn't support higher inversions directly
            "9th" or "11th" or "13th" => chord, // Keep root position for now
            _ => throw new NotSupportedException($"Bass option '{bassOption}' is not supported")
        };

        /// <summary>
        /// Maps MusicTheory Alteration to GeneratorData accidental string.
        /// </summary>
        private static string MapAlteration(Alteration alteration) => alteration switch
        {
            Alteration.Natural => "Natural",
            Alteration.Sharp => "Sharp",
            Alteration.Flat => "Flat",
            Alteration.DoubleSharp => "Sharp", // Map to single sharp
            Alteration.DoubleFlat => "Flat",   // Map to single flat
            _ => "Natural"
        };
    }
}