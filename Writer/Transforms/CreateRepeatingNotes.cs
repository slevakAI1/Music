using Music.Designer;
using Music.Domain;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Transforms WriterFormData to Phrase objects with MIDI tick-based timing.
    /// </summary>
    public static class CreateRepeatingNotes
    {
        /// <summary>
        /// Creates a Phrase with a repeating set of the specified MIDI note number.
        /// </summary>
        /// <param name="noteNumber">The MIDI note number (0-127). Use 60 for Middle C.</param>
        /// <param name="repeatCount">Number of times to repeat the note.</param>
        /// <param name="noteDurationTicks">Duration of each note in MIDI ticks. Default is 480 (quarter note).</param>
        /// <param name="noteOnVelocity">MIDI velocity (0-127). Default is 100.</param>
        /// <param name="isRest">Whether the note should be treated as a rest. Default is false.</param>
        /// <returns>A Phrase object containing the repeating notes.</returns>
        public static Phrase Execute(
            int noteNumber,
            int repeatCount = 1,
            int noteDurationTicks = 480,
            int noteOnVelocity = 100,
            bool isRest = false)
        {
            var phraseNotes = new List<PhraseNote>();
            int currentPosition = 0;

            for (int i = 0; i < repeatCount; i++)
            {
                var phraseNote = new PhraseNote(
                    noteNumber: noteNumber,
                    absolutePositionTicks: currentPosition,
                    noteDurationTicks: noteDurationTicks,
                    noteOnVelocity: noteOnVelocity,
                    isRest: isRest);

                phraseNotes.Add(phraseNote);
                currentPosition += noteDurationTicks;
            }

            var phrase = new Phrase(phraseNotes);
            return phrase;
        }
    }
}

/*         // Helper methods preserved for potential future use
        private static int CalculateNoteNumber(char step, int alter, int octave)
        {
            // C4 = MIDI note 60
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

        private static int CalculateNoteDurationTicks(int duration, int dots, int ticksPerQuarterNote)
        {
            // Duration: 1=whole, 2=half, 4=quarter, 8=eighth, etc.
            // Base ticks for this duration
            int baseTicks = (ticksPerQuarterNote * 4) / duration;

            // Apply dots: each dot adds half of the previous value
            int totalTicks = baseTicks;
            int dotValue = baseTicks;
            for (int i = 0; i < dots; i++)
            {
                dotValue /= 2;
                totalTicks += dotValue;
            }

            return totalTicks;
        }

        private static int GetNoteValue(string? noteValueString)
        {
            if (noteValueString != null && Music.MusicConstants.NoteValueMap.TryGetValue(noteValueString, out var nv))
            {
                return nv;
            }
            return 4; // default quarter note
        }

        private static int GetAlter(string? accidental)
        {
            return (accidental ?? "Natural") switch
            {
                var s when s.Equals("Sharp", StringComparison.OrdinalIgnoreCase) => 1,
                var s when s.Equals("Flat", StringComparison.OrdinalIgnoreCase) => -1,
                _ => 0
            };
        }

*/