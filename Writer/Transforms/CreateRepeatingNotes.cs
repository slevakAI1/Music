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
            int noteDurationTicks,
            int repeatCount,
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
