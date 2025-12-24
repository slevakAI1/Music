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
            DataGridView dgSong)
        {
            // Validate that song tracks are selected before executing
            if (!ValidateSongTracksSelected(dgSong))
                return;

            var (noteNumber, noteDurationTicks, repeatCount) =
                MusicCalculations.GetRepeatingNotesParameters(formData);

            var partTrack = CreateRepeatingNotes.Execute(
                noteNumber: noteNumber,
                noteDurationTicks: noteDurationTicks,
                repeatCount: repeatCount,
                noteOnVelocity: 100);

            // Append the partTrack notes to all selected rows
            AppendSongTrackNoteEventsToSelectedRows(dgSong, partTrack);
        }

        /// <summary>
        /// Validates that song tracks are selected in the grid.
        /// </summary>
        private static bool ValidateSongTracksSelected(DataGridView dgSong)
        {
            var hasSongTrackSelection = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= SongGridManager.FIXED_ROWS_COUNT);

            if (!hasSongTrackSelection)
            {
                MessageBoxHelper.Show(
                    "Please select one or more tracks to apply the command.",
                    "No Tracks Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Appends partTrack notes to all selected partTrack rows in the grid.
        /// </summary>
        private static void AppendSongTrackNoteEventsToSelectedRows(DataGridView dgSong, PartTrack songTrack)
        {
            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                // Skip fixed rows
                if (selectedRow.Index < SongGridManager.FIXED_ROWS_COUNT)
                    continue;

                // Get existing partTrack data
                var dataObj = selectedRow.Cells["colData"].Value;
                if (dataObj is not PartTrack existingSongTrack)
                    continue;

                // Append the new notes
                existingSongTrack.PartTrackNoteEvents.AddRange(songTrack.PartTrackNoteEvents);

                // Update the measure cells to reflect the new note counts
                SongGridManager.PopulatePartMeasureNoteCount(dgSong, selectedRow.Index);
            }
        }
    }
}