namespace Music.Writer
{
    /// <summary>
    /// Pure calculation helpers for musical computations.
    /// No UI dependencies - can be used standalone.
    /// </summary>
    public static class MusicCalculations
    {
        /// <summary>
        /// Extracts all repeating note parameters from form data.
        /// </summary>
        public static (int noteNumber, int noteDurationTicks, int repeatCount, bool isRest)
            GetRepeatingNotesParameters(WriterFormData formData)
        {
            // Extract repeat count
            var repeatCount = formData.NumberOfNotes ?? 1;

            // Extract rest flag
            var isRest = formData.IsRest ?? false;

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

            return (noteNumber, noteDurationTicks, repeatCount, isRest);
        }

        /// <summary>
        /// Converts chord parameters to standard chord notation symbol (e.g., "Cmaj7", "Am", "G7/B").
        /// </summary>
        /// <param name="key">The key (e.g., "C major", "F# minor")</param>
        /// <param name="degree">The scale degree (1-7)</param>
        /// <param name="quality">The chord quality (e.g., "Major", "Minor7", "Dominant7")</param>
        /// <param name="bass">The bass note (e.g., "root", "3rd", "5th")</param>
        /// <returns>Chord symbol notation string</returns>
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

            // Map quality to chord symbol suffix
            var suffix = quality switch
            {
                "Major" => "",
                "Minor" => "m",
                "Diminished" => "dim",
                "Augmented" => "aug",
                "Dominant7" => "7",
                "Major7" => "maj7",
                "Minor7" => "m7",
                "Diminished7" => "dim7",
                "HalfDiminished7" => "m7b5",
                "MinorMajor7" => "m(maj7)",
                "Major6" => "6",
                "Minor6" => "m6",
                "Sus2" => "sus2",
                "Sus4" => "sus4",
                "Power5" => "5",
                "Dominant9" => "9",
                "Major9" => "maj9",
                "Minor9" => "m9",
                "Dominant11" => "11",
                "Dominant13" => "13",
                "MajorAdd9" => "add9",
                "MajorAdd11" => "add11",
                "Major6Add9" => "6/9",
                _ => ""
            };

            // Build base chord symbol
            var symbol = $"{chordRoot}{suffix}";

            // Add slash chord notation if not root position
            if (!bass.Equals("root", StringComparison.OrdinalIgnoreCase))
            {
                var bassInterval = bass.ToLowerInvariant() switch
                {
                    "3rd" => GetChordInterval(quality, 1),
                    "5th" => GetChordInterval(quality, 2),
                    "7th" => GetChordInterval(quality, 3),
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
            // Get semitones for 3rd, 5th, 7th, etc. based on chord quality
            return (quality, noteIndex) switch
            {
                (_, 0) => 0, // Root
                ("Major", 1) or ("Major7", 1) or ("Major6", 1) or ("Dominant7", 1) or ("Dominant9", 1) => 4, // Major 3rd
                ("Minor", 1) or ("Minor7", 1) or ("Minor6", 1) or ("Diminished", 1) or ("HalfDiminished7", 1) => 3, // Minor 3rd
                ("Diminished", 2) or ("Diminished7", 2) or ("HalfDiminished7", 2) => 6, // Diminished 5th
                (_, 2) => 7, // Perfect 5th (most common)
                ("Major7", 3) => 11, // Major 7th
                ("Dominant7", 3) or ("Minor7", 3) or ("HalfDiminished7", 3) => 10, // Minor 7th
                ("Diminished7", 3) => 9, // Diminished 7th
                _ => 0
            };
        }

        /// <summary>
        /// Calculates MIDI note number from musical note components.
        /// </summary>
        /// <param name="step">The note step (C, D, E, F, G, A, B)</param>
        /// <param name="octave">The octave number</param>
        /// <param name="accidental">The accidental string ("Sharp", "Flat", "Natural", etc.)</param>
        /// <returns>The MIDI note number (0-127)</returns>
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

        /// <summary>
        /// Calculates note duration in MIDI ticks.
        /// </summary>
        /// <param name="noteValue">The note value string (e.g., "Quarter (1/4)" for quarter note)</param>
        /// <param name="dots">Number of dots to apply</param>
        /// <param name="tupletNumber">Optional tuplet identifier</param>
        /// <param name="tupletCount">Tuplet count (m in m:n tuplet)</param>
        /// <param name="tupletOf">Tuplet basis (n in m:n tuplet)</param>
        /// <returns>Duration in MIDI ticks</returns>
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

        /// <summary>
        /// Parses the numeric duration value from a note value display string.
        /// </summary>
        /// <param name="noteValue">Display string like "Quarter (1/4)" or "Eighth (1/8)"</param>
        /// <returns>The numeric duration (1, 2, 4, 8, 16, 32), defaulting to 4 (quarter note)</returns>
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

        /// <summary>
        /// Applies dot duration extensions to a base duration.
        /// </summary>
        /// <param name="baseTicks">The base duration in ticks</param>
        /// <param name="dots">Number of dots to apply</param>
        /// <returns>The dotted duration in ticks</returns>
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

        /// <summary>
        /// Applies tuplet adjustment to a duration.
        /// </summary>
        /// <param name="dottedTicks">The dotted duration in ticks</param>
        /// <param name="tupletNumber">Optional tuplet identifier</param>
        /// <param name="tupletCount">Tuplet count (m in m:n tuplet)</param>
        /// <param name="tupletOf">Tuplet basis (n in m:n tuplet)</param>
        /// <returns>The tuplet-adjusted duration in ticks</returns>
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
    }
}