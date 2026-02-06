// AI: purpose=stateless music math (MIDI, ticks, chord symbols); zero UI deps; invariants=TicksPerQuarterNote=480, MIDI 0-127
// AI: contracts=input from WriterFormData; ChordQuality.Normalize for symbols;
// AI: change=adding rhythms: update CalculateRhythm lookup tables; chords: sync with ChordQuality.All; keep TransposeNote sharp/flat logic
using Music.Generator;

namespace Music.Writer
{
    /// <summary>
    /// Pure calculation helpers for musical computations.
    /// No UI dependencies - can be used standalone.
    /// </summary>
    public static class MusicCalculations
    {
        // AI: purpose=Extract noteNumber, noteDurationTicks, repeatCount from WriterFormData; relies on DTO default semantics
        public static (int noteNumber, int noteDurationTicks, int repeatCount)
            GetRepeatingNotesParameters(WriterFormData formData)
        {
            // Extract repeat count
            var repeatCount = formData.NumberOfNotes ?? 1;

            // Calculate MIDI note number from step, accidental, and octave
            var noteNumber = CalculateMidiNoteNumber(
                formData.Step,
                formData.OctaveAbsolute ?? 4,
                formData.Accidental);

            // Calculate note duration in ticks
            var noteDurationTicks = CalculateNoteDurationTicks(
                formData.NoteValue,
                formData.Dots,
                formData.TupletNumber,
                formData.TupletCount ?? 0,
                formData.TupletOf ?? 0);

            return (noteNumber, noteDurationTicks, repeatCount);
        }

        // AI: purpose=Build standard chord symbol from key, degree, quality, bass; depends on ChordQuality.Normalize; supports major/minor
        public static string ConvertToChordNotation(string key, int degree, string quality, string bass)
        {
            // Parse key to get root and mode
            var parts = key.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var keyRoot = parts[0];
            var mode = parts[1];

            // Build scale intervals (major or natural minor)
            int[] intervals = mode.Equals("major", StringComparison.OrdinalIgnoreCase)
                ? new[] { 0, 2, 4, 5, 7, 9, 11 }  // Major scale
                : new[] { 0, 2, 3, 5, 7, 8, 10 }; // Natural minor scale

            // Get root note of the chord based on degree
            var rootSemitones = intervals[degree - 1];
            var chordRoot = TransposeNote(keyRoot, rootSemitones);

            // Normalize quality to standard chord symbol (in case it's a long name)
            var normalizedQuality = ChordQuality.Normalize(quality);

            // Build chord symbol by appending the quality suffix to the root
            var symbol = $"{chordRoot}{normalizedQuality}";

            // Add slash chord notation if not root position
            if (!bass.Equals("root", StringComparison.OrdinalIgnoreCase))
            {
                var bassInterval = bass.ToLowerInvariant() switch
                {
                    "3rd" => GetChordInterval(normalizedQuality, 1),
                    "5th" => GetChordInterval(normalizedQuality, 2),
                    "7th" => GetChordInterval(normalizedQuality, 3),
                    _ => 0
                };
                if (bassInterval > 0)
                {
                    var bassNote = TransposeNote(chordRoot, bassInterval);
                    symbol = $"{symbol}/{bassNote}";
                }
            }

            return symbol;
        }

        private static string TransposeNote(string note, int semitones)
        {
            var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            var flatNames = new[] { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

            // Parse input note
            var baseNote = note[0].ToString().ToUpper();
            var accidental = note.Length > 1 ? note[1] : ' ';

            // Find starting position
            var startIndex = Array.FindIndex(noteNames, n => n[0] == baseNote[0]);
            if (accidental == '#') startIndex++;
            if (accidental == 'b') startIndex--;

            // Transpose
            var newIndex = (startIndex + semitones) % 12;
            if (newIndex < 0) newIndex += 12;

            // Prefer sharps unless original used flats
            return accidental == 'b' ? flatNames[newIndex] : noteNames[newIndex];
        }

        private static int GetChordInterval(string quality, int noteIndex)
        {
            // Get semitones for 3rd, 5th, 7th, etc. based on chord quality (uses standard chord symbols)
            return (quality, noteIndex) switch
            {
                (_, 0) => 0, // Root
                ("", 1) or ("maj7", 1) or ("6", 1) or ("7", 1) or ("9", 1) => 4, // Major 3rd
                ("m", 1) or ("m7", 1) or ("m6", 1) or ("dim", 1) or ("m7b5", 1) => 3, // Minor 3rd
                ("dim", 2) or ("dim7", 2) or ("m7b5", 2) => 6, // Diminished 5th
                (_, 2) => 7, // Perfect 5th (most common)
                ("maj7", 3) => 11, // Major 7th
                ("7", 3) or ("m7", 3) or ("m7b5", 3) => 10, // Minor 7th
                ("dim7", 3) => 9, // Diminished 7th
                _ => 0
            };
        }

        // AI: purpose=Compute MIDI note number from step, octave and accidental; returns 0-127 using (octave+1)*12+base+alter
        public static int CalculateMidiNoteNumber(char step, int octave, string accidental)
        {
            // Convert accidental string to alter value
            int alter = accidental switch
            {
                "Sharp" or "#" => 1,
                "Flat" or "b" => -1,
                "Natural" => 0,
                _ => 0
            };

            // Calculate MIDI note number
            int baseNote = step switch
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

        // AI: purpose=Compute duration in ticks from noteValue,dots,tuplet; uses MusicConstants.TicksPerQuarterNote for base ticks
        public static int CalculateNoteDurationTicks(
            string noteValue,
            int dots,
            string tupletNumber,
            int tupletCount,
            int tupletOf)
        {
            // Parse the duration from the display string format "Name (1/n)"
            // Examples: "Whole (1)", "Half (1/2)", "Quarter (1/4)", "Eighth (1/8)", "16th (1/16)", "32nd (1/32)"
            int duration = ParseNoteValueDuration(noteValue);

            // Base ticks for this duration (e.g., quarter=480, eighth=240)
            int baseTicks = (MusicConstants.TicksPerQuarterNote * 4) / duration;

            // Apply dots (each dot adds half of the previous value)
            int dottedTicks = ApplyDots(baseTicks, dots);

            // Apply tuplet if specified
            return ApplyTuplet(dottedTicks, tupletNumber, tupletCount, tupletOf);
        }

        // AI: purpose=Parse denominator from note value display like "Quarter (1/4)"; returns numeric duration or 4 on failure
        private static int ParseNoteValueDuration(string noteValue)
        {
            if (string.IsNullOrWhiteSpace(noteValue))
                return 4; // Default to quarter note

            // Extract the denominator from formats like "Quarter (1/4)" or "Whole (1)"
            // For "Whole (1)", the duration is 1
            // For others like "Quarter (1/4)", extract the "4"
            var parenIndex = noteValue.IndexOf('(');
            if (parenIndex < 0)
                return 4;

            var valuesPart = noteValue.Substring(parenIndex + 1).TrimEnd(')').Trim();

            // Handle "Whole (1)" case - no slash
            if (!valuesPart.Contains('/'))
            {
                return int.TryParse(valuesPart, out int wholeValue) && wholeValue == 1 ? 1 : 4;
            }

            // Handle fraction format like "1/4", "1/8", etc.
            var parts = valuesPart.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[1], out int denominator))
            {
                return denominator;
            }

            return 4; // Default to quarter note
        }

        // AI: purpose=Apply dot extensions: each dot adds half the previous increment (1 dot=1.5x, 2 dots=1.75x)
        private static int ApplyDots(int baseTicks, int dots)
        {
            int dottedTicks = baseTicks;
            int addedValue = baseTicks / 2;

            for (int i = 0; i < dots; i++)
            {
                dottedTicks += addedValue;
                addedValue /= 2;
            }

            return dottedTicks;
        }

        // AI: purpose=Apply tuplet adjustment when tupletNumber provided; returns (dottedTicks*tupletOf)/tupletCount
        private static int ApplyTuplet(
            int dottedTicks,
            string tupletNumber,
            int tupletCount,
            int tupletOf)
        {
            if (!string.IsNullOrWhiteSpace(tupletNumber) && tupletCount > 0 && tupletOf > 0)
            {
                // Tuplet adjusts duration: e.g., triplet = 2/3 of normal duration
                return (dottedTicks * tupletOf) / tupletCount;
            }

            return dottedTicks;
        }

        // AI: purpose=Inverse of CalculateMidiNoteNumber; returns (Step,Alter,Octave); favors sharps for accidentals
        public static (char Step, int Alter, int Octave) CalculatePitch(int noteNumber)
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

        // AI: purpose=Map ticks to (Duration,Dots,TupletActual,TupletNormal); checks tuplets with ±1 tick tolerance then standard durations
        public static (int Duration, int Dots, int? TupletActual, int? TupletNormal) CalculateRhythm(int noteDurationTicks)
        {
            const int ticksPerQuarterNote = 480;

            // Check for common tuplets first
            var tupletDurations = new (int ticks, int duration, int dots, int actual, int normal)[]
            {
                // Triplets (3:2) - most common
                (320, 4, 0, 3, 2),   // Quarter note triplet
                (160, 8, 0, 3, 2),   // Eighth note triplet
                (80, 16, 0, 3, 2),   // Sixteenth note triplet
                (640, 2, 0, 3, 2),   // Half note triplet
                
                // Quintuplets (5:4)
                (384, 4, 0, 5, 4),   // Quarter note quintuplet (1920/5)
                (192, 8, 0, 5, 4),   // Eighth note quintuplet
                (96, 16, 0, 5, 4),   // Sixteenth note quintuplet
                
                // Sextuplets (6:4)
                (320, 8, 0, 6, 4),   // Eighth note sextuplet (1920/6)
                (160, 16, 0, 6, 4),  // Sixteenth note sextuplet
                
                // Septuplets (7:4)
                (274, 8, 0, 7, 4),   // Eighth note septuplet (approx 1920/7)
                (137, 16, 0, 7, 4),  // Sixteenth note septuplet
                
                // Nonuplets (9:8)
                (213, 8, 0, 9, 8),   // Eighth note nonuplet (1920/9)
                (107, 16, 0, 9, 8),  // Sixteenth note nonuplet
            };

            // Check for tuplet matches
            foreach (var (ticks, duration, dots, actual, normal) in tupletDurations)
            {
                if (Math.Abs(noteDurationTicks - ticks) <= 1) // Allow 1 tick tolerance for rounding
                {
                    return (duration, dots, actual, normal);
                }
            }

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

            // Find the closest match for standard durations
            foreach (var (ticks, duration, dots) in noteDurations)
            {
                if (noteDurationTicks == ticks)
                {
                    return (duration, dots, null, null);
                }
            }

            // If no exact match, calculate base duration without dots
            int wholeTicks = MusicConstants.TicksPerQuarterNote * 4;
            if (noteDurationTicks >= wholeTicks)
            {
                return (1, 0, null, null); // Default to whole note
            }

            // Calculate the closest power of 2 duration
            int calculatedDuration = 1;
            int closestTicks = wholeTicks;

            while (closestTicks > noteDurationTicks && calculatedDuration < 64)
            {
                calculatedDuration *= 2;
                closestTicks /= 2;
            }

            return (calculatedDuration, 0, null, null);
        }
    }
}
