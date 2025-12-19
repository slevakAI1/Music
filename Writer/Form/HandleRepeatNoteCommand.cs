using Music.Generator;

namespace Music.Writer
{
    /// <summary>
    /// Static command execution logic for WriterForm.
    /// Each method accepts only the specific dependencies it needs.
    /// </summary>
    public static class HandleRepeatNoteCommand
    {
        /// <summary>
        /// Adds repeating notes to the song tracks selected in the grid
        /// </summary>
        public static void Execute(
            WriterFormData formData,
            DataGridView dgSong,
            Form owner)
        {
            // Validate that song tracks are selected before executing
            if (!ValidateSongTracksSelected(dgSong, owner))
                return;

            var (noteNumber, noteDurationTicks, repeatCount, isRest) =
                MusicCalculations.GetRepeatingNotesParameters(formData);

            var songTrack = CreateRepeatingNotes.Execute(
                noteNumber: noteNumber,
                noteDurationTicks: noteDurationTicks,
                repeatCount: repeatCount,
                noteOnVelocity: 100,
                isRest: isRest);

            // Append the songTrack notes to all selected rows
            AppendSongTrackNoteEventsToSelectedRows(dgSong, songTrack);
        }

        /// <summary>
        /// Validates that phrases are selected in the grid.
        /// </summary>
        private static bool ValidateSongTracksSelected(DataGridView dgSong, Form owner)
        {
            var hasSongTrackSelection = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= SongGridManager.FIXED_ROWS_COUNT);

            if (!hasSongTrackSelection)
            {
                MessageBox.Show(owner,
                    "Please select one or more tracks to apply the command.",
                    "No Tracks Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Appends songTrack notes to all selected songTrack rows in the grid.
        /// </summary>
        private static void AppendSongTrackNoteEventsToSelectedRows(DataGridView dgSong, SongTrack phrase)
        {
            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                // Skip fixed rows
                if (selectedRow.Index < SongGridManager.FIXED_ROWS_COUNT)
                    continue;

                // Get existing songTrack data
                var dataObj = selectedRow.Cells["colData"].Value;
                if (dataObj is not SongTrack existingPhrase)
                    continue;

                // Append the new notes
                existingPhrase.SongTrackNoteEvents.AddRange(phrase.SongTrackNoteEvents);

                // Update the measure cells to reflect the new note counts
                SongGridManager.PopulatePartMeasureNoteCount(dgSong, selectedRow.Index);
            }
        }
    }
}