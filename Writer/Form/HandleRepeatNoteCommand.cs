// AI: purpose=UI command: append repeating notes to selected PartTrack rows in the song grid.
// AI: invariants=Selected rows must map to PartTrack instances (colData). Skips top FIXED_ROWS_COUNT rows.
// AI: deps=Uses MusicCalculations, CreateRepeatingNotes, SongGridManager, GridControlLinesManager; changing those APIs breaks this handler.
// AI: change=If PartTrack/Event model changes update AppendSongTrackNoteEventsToSelectedRows and grid population logic.

using Music.Generator;

namespace Music.Writer
{
    // AI: Command group for repeating-note actions; methods accept minimal deps (formData + grid) to simplify testing.
    public static class HandleRepeatNoteCommand
    {
        // AI: Execute validates selection, builds a repeating PartTrack, appends events to selected PartTracks and updates grid counts.
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

        // AI: returns false + shows informational dialog when no eligible part-track rows are selected.
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

        // AI: Appends events to existing PartTrack.PartTrackNoteEvents (mutates in-place) and updates measure note counts in the grid.
        // AI: note=Assumes cell "colData" contains a PartTrack; ignores rows that don't meet this contract.
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