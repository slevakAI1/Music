using Music.MyMidi;

namespace Music.Writer
{
    // Command execution logic for WriterForm
    public partial class WriterForm
    {
        // ========== COMMAND EXECUTION ==========

        /// <summary>
        /// Adds repeating notes to the phrases selected in the grid
        /// </summary>
        public void HandleRepeatNote(WriterFormData formData)
        {
            // Validate that phrases are selected before executing
            if (!ValidatePhrasesSelected())
                return;

            var (noteNumber, noteDurationTicks, repeatCount, isRest) =
                MusicCalculations.GetRepeatingNotesParameters(formData);

            var phrase = CreateRepeatingNotes.Execute(
                noteNumber: noteNumber,
                noteDurationTicks: noteDurationTicks,
                repeatCount: repeatCount,
                noteOnVelocity: 100,
                isRest: isRest);

            // Append the phrase notes to all selected rows
            AppendPhraseNotesToSelectedRows(phrase);
        }
    }
}