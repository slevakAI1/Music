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