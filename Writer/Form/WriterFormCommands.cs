using Music.MyMidi;
using static System.Windows.Forms.DataFormats;

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

        public void HandleHarmonySyncTest(WriterFormData formData)
        {
            // TO DO

            // Add 4 phrases to the dgSong grid based on the Harmony Event Timeline in the Harmony fixed row.
            // The parts will be 
            //  Rock Organ
            //      Each measure gets two half notes of the chord for the HarmonyEvent for the measure
            //  Electric Guitar (clean)
            //      Each measure gets 8 eighth notes of the chord for the HarmonyEvent for the measure
            //  Electric Bass (finger)
            //      Each measure gets four quarter notes of the chord for the HarmonyEvent for the measure
            //  Drum Set
            //      Each measure gets a bass drum beat on beats 1 and 3 and a snare drum on every beat.

            // Generate 4 measure of the above. Place all the code here.
        }
    }
}