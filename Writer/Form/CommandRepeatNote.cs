using Music.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Music.Writer
{
    /// <summary>
    /// Static command execution logic for WriterForm.
    /// Each method accepts only the specific dependencies it needs.
    /// </summary>
    public static class CommandRepeatNote
    {
        /// <summary>
        /// Adds repeating notes to the phrases selected in the grid
        /// </summary>
        public static void HandleRepeatNote(
            WriterFormData formData,
            DataGridView dgSong,
            Form owner)
        {
            // Validate that phrases are selected before executing
            if (!ValidatePhrasesSelected(dgSong, owner))
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
            AppendPhraseNotesToSelectedRows(dgSong, phrase);
        }

        /// <summary>
        /// Validates that phrases are selected in the grid.
        /// </summary>
        private static bool ValidatePhrasesSelected(DataGridView dgSong, Form owner)
        {
            var hasPhraseSelection = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= SongGridManager.FIXED_ROWS_COUNT);

            if (!hasPhraseSelection)
            {
                MessageBox.Show(owner,
                    "Please select one or more phrase rows to apply the command.",
                    "No Phrases Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Appends phrase notes to all selected phrase rows in the grid.
        /// </summary>
        private static void AppendPhraseNotesToSelectedRows(DataGridView dgSong, Phrase phrase)
        {
            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                // Skip fixed rows
                if (selectedRow.Index < SongGridManager.FIXED_ROWS_COUNT)
                    continue;

                // Get existing phrase data
                var dataObj = selectedRow.Cells["colData"].Value;
                if (dataObj is not Phrase existingPhrase)
                    continue;

                // Append the new notes
                existingPhrase.PhraseNotes.AddRange(phrase.PhraseNotes);
            }
        }
    }
}