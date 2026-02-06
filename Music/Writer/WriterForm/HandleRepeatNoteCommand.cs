// AI: purpose=Append repeating note events to selected PartTrack rows in the song grid.
// AI: invariants=Rows with Index < SongGridManager.FIXED_ROWS_COUNT are non-editable; cell 'colData' must hold PartTrack.
// AI: deps=MusicCalculations.CreateRepeatingNotes; SongGridManager.PopulatePartMeasureNoteCount; GridControlLinesManager.GetTimeSignatureTrack
// AI: perf=Mutates PartTrack.PartTrackNoteEvents in-place; may trigger expensive UI updates for large selections.

using Music.Generator;

namespace Music.Writer
{
    // AI: purpose=UI command group to construct repeating notes and apply them to selected song tracks.
    public static class HandleRepeatNoteCommand
    {
        // AI: entry=Validate selection, build repeating PartTrack, append events to selected rows, update measure counts in grid.
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

        // AI: purpose=Return false and show info dialog when no editable part-track rows are selected.
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

        // AI: effects=Mutates existing PartTrack.PartTrackNoteEvents by adding events from provided PartTrack.
        // AI: invariants=Skips rows < FIXED_ROWS_COUNT; ignores rows where 'colData' is not a PartTrack; updates measure counts.
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

                // Get TimeSignatureTrack from the grid
                var timeSignatureTrack = GridControlLinesManager.GetTimeSignatureTrack(dgSong);

                // Update the measure cells to reflect the new note counts
                SongGridManager.PopulatePartMeasureNoteCount(dgSong, selectedRow.Index, timeSignatureTrack);
            }
        }
    }
}
