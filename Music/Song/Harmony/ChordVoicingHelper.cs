// AI: purpose=Internal helper to build chord voicings as MIDI notes from harmony params; used by builders and pitch contexts.
// AI: invariants=Output MIDI list must match chord tones order after voicing; Normalize quality before mapping; exceptions indicate caller errors.
// AI: deps=Relies on PitchClassUtils.ParseKey, MusicTheory Scale/Chord/Note types and ChordQuality.Normalization mapping.
// AI: perf=Not hotpath; heavy allocations acceptable during song setup; avoid changing voicing order, it affects downstream consumers.

using MusicTheory;

namespace Music.Generator
{
    internal static class ChordVoicingHelper
    {
        // AI: GenerateChordMidiNotes: validates args, normalizes quality, maps to ChordType, voicing preserves chord-tone order.
        // AI: errors=throws InvalidOperationException on invalid degree, mapping, or other failures; callers catch/log as needed.
        public static List<int> GenerateChordMidiNotes(
            string key,
            int degree,
            string quality,
            string bass,
            int baseOctave)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (quality == null)
                throw new ArgumentException("Quality cannot be null", nameof(quality));
            if (string.IsNullOrWhiteSpace(bass))
                throw new ArgumentException("Bass cannot be null or empty", nameof(bass));

            try
            {
                // Step 1: Parse the key using shared logic (single source of truth)
                var parsed = PitchClassUtils.ParseKey(key);
                var (keyRoot, keyMode) = ConvertParsedKeyToNote(parsed);
                
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
                
                // Step 4: Map quality string to ChordType (normalize first)
                var normalizedQuality = ChordQuality.Normalize(quality);
                
                var chordType = MapQualityToChordType(normalizedQuality);
                
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

        // AI: CalculateMidiNoteNumber: mapping uses step->semitone base and (octave+1)*12 convention; keep formula stable.
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

        // AI: ConvertParsedKeyToNote: maps ParsedKey to MusicTheory.Note with octave=4 by convention.
        // AI: change=If ParseKey parsed shape changes, update this mapping accordingly.
        private static (Note root, string mode) ConvertParsedKeyToNote(ParsedKey parsed)
        {
            NoteName noteName = parsed.NoteLetter switch
            {
                'C' => NoteName.C,
                'D' => NoteName.D,
                'E' => NoteName.E,
                'F' => NoteName.F,
                'G' => NoteName.G,
                'A' => NoteName.A,
                'B' => NoteName.B,
                _ => throw new ArgumentException($"Invalid note letter: {parsed.NoteLetter}")
            };

            Alteration alteration = parsed.Alteration switch
            {
                -1 => Alteration.Flat,
                0 => Alteration.Natural,
                1 => Alteration.Sharp,
                _ => throw new ArgumentException($"Unsupported alteration: {parsed.Alteration}")
            };

            return (new Note(noteName, alteration, 4), parsed.Mode);
        }

        // AI: MapQualityToChordType: expects normalized short quality strings; update when adding qualities to ChordQuality.All.
        // AI: errors=throws NotSupportedException for unknown qualities to alert callers to update mappings.
        private static ChordType MapQualityToChordType(string quality)
        {
            return quality switch
            {
                // Triads
                "" => ChordType.Major,
                "m" => ChordType.Minor,
                "dim" => ChordType.Diminished,
                "aug" => ChordType.Augmented,
                "sus2" => ChordType.Sus2,
                "sus4" => ChordType.Sus4,
                "5" => ChordType.Power5,

                // 6ths
                "6" => ChordType.Major6,
                "m6" => ChordType.Minor6,
                "6/9" => ChordType.Major6Add9,

                // 7ths
                "7" => ChordType.Dominant7,
                "maj7" => ChordType.Major7,
                "m7" => ChordType.Minor7,
                "dim7" => ChordType.Diminished7,
                "m7b5" => ChordType.HalfDiminished7,
                "m(maj7)" => ChordType.MinorMajor7,

                // Extensions
                "9" => ChordType.Dominant9,
                "maj9" => ChordType.Major9,
                "m9" => ChordType.Minor9,
                "11" => ChordType.Dominant11,
                "13" => ChordType.Dominant13,

                // Adds
                "add9" => ChordType.MajorAdd9,
                "add11" => ChordType.MajorAdd11,

                _ => throw new NotSupportedException($"Chord quality '{quality}' is not supported. Valid chord symbols: {string.Join(", ", ChordQuality.ShortNames)}")
            };
        }

        // AI: ApplyVoicing: bassOption selects rotation index; if index >= chord size, returns root position unchanged.
        // AI: behavior=notes at/after bassIndex stay at baseOctave; notes before move up one octave to avoid octave clash.
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

        // AI: MapAlterationToAlter: converts MusicTheory.Alteration to semitone alter integers; keep mapping exhaustive.
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