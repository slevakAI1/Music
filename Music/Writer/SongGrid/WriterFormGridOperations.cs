// AI: purpose=Handlers for grid row operations and playback control in WriterForm; UI-only helpers with predictable side-effects.
// AI: invariants=Grid fixed rows indices and column names (colData,colType,colDescription) are stable contracts used across the app.
// AI: deps=Relies on SongGridManager, GridControlLinesManager, Midi services, and PartTrack DTO; renaming breaks many callers.
// AI: change=If adding new grid behaviors update unit tests and grid population helpers accordingly.

using System.Globalization;
using System.Reflection;
using Music.Generator;
using Music.MyMidi;
using Music.Properties;
using Music.Song.Material;

namespace Music.Writer
{
    public class WriterFormGridOperations
    {
        private static int _nextPhraseDescriptionNumber = 1;
        // AI: HandleAddSongTrack: create an empty PartTrack row and select it; expects SongGridManager.AddNewPartTrack semantics.
        public void HandleAddSongTrack(DataGridView dgSong)
        {
            // Create an empty PartTrack and add it to the grid via the existing helper.
            var emptyTrack = new PartTrack(new List<PartTrackEvent>())
            {
                MidiProgramNumber = -1  // "Select..."
            };

            // Use SongGridManager to initialize the row consistently with other adds.
            SongGridManager.AddNewPartTrack(emptyTrack, dgSong);

            // Select the newly added row (last row)
            if (dgSong.Rows.Count > SongGridManager.FIXED_ROWS_COUNT)
            {
                int newRowIndex = dgSong.Rows.Count - 1;
                dgSong.ClearSelection();
                dgSong.Rows[newRowIndex].Selected = true;

                // Move current cell to an editable cell so the selection is visible and focusable
                var instrumentCol = dgSong.Columns["colInstrument"];
                if (instrumentCol != null && dgSong.Rows[newRowIndex].Cells[instrumentCol.Index] != null)
                {
                    dgSong.CurrentCell = dgSong.Rows[newRowIndex].Cells[instrumentCol.Index];
                }
            }
        }

        // AI: HandleDeleteSongTracks: removes selected non-fixed rows safely by deleting in descending order to avoid reindexing.
        public void HandleDeleteSongTracks(DataGridView dgSong)
        {
            if (dgSong.SelectedRows.Count == 0)
            {
                MessageBoxHelper.Show(
                    "Please select one or more rows to delete.",
                    "Delete Tracks",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var indices = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.Index)
                .Where(i => i >= SongGridManager.FIXED_ROWS_COUNT)
                .OrderByDescending(i => i)
                .ToList();

            foreach (var idx in indices)
            {
                if (idx >= SongGridManager.FIXED_ROWS_COUNT && idx < dgSong.Rows.Count)
                    dgSong.Rows.RemoveAt(idx);
            }
        }

        // AI: HandleClearAll: clears all dynamic track rows and resets fixed-row data objects and measure displays.
        public void HandleClearAll(DataGridView dgSong)
        {
            // Remove all design and music track data
            while (dgSong.Rows.Count > SongGridManager.FIXED_ROWS_COUNT)
            {
                dgSong.Rows.RemoveAt(SongGridManager.FIXED_ROWS_COUNT);
            }

            // Clear measure columns and data objects from all fixed rows
            for (int rowIndex = 0; rowIndex < SongGridManager.FIXED_ROWS_COUNT; rowIndex++)
            {
                SongGridManager.ClearMeasureCellsForRow(dgSong, rowIndex);
                
                // Clear the data object
                var dataCol = dgSong.Columns["colData"];
                if (dataCol != null)
                    dgSong.Rows[rowIndex].Cells[dataCol.Index].Value = null;
            }
            
            // Reset the track number counter since we've cleared all tracks
            SongGridManager.ResetTrackNumber();
        }

        // AI: HandleClearSelected: clears selected rows; fixed rows clear their data, track rows reset to empty PartTrack and Select...
        public void HandleClearSelected(DataGridView dgSong)
        {
            if (dgSong.SelectedRows == null || dgSong.SelectedRows.Count == 0)
            {
                MessageBoxHelper.Show("Please select one or more rows to clear.", "Clear Rows", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (DataGridViewRow row in dgSong.SelectedRows)
            {
                // Handle fixed rows
                if (row.Index < SongGridManager.FIXED_ROWS_COUNT)
                {
                    // Clear measure columns for fixed row
                    SongGridManager.ClearMeasureCellsForRow(dgSong, row.Index);
                    
                    // Clear the data object
                    var dataCol = dgSong.Columns["colData"];
                    if (dataCol != null)
                        row.Cells[dataCol.Index].Value = null;
                    
                    continue;
                }

                // Handle Tracks
                // Reset instrument to "Select..." (-1)
                var instrCol = dgSong.Columns["colType"];
                if (instrCol != null)
                    row.Cells[instrCol.Index].Value = -1;

                // Reset data to empty PartTrack
                var trackDataCol = dgSong.Columns["colData"];
                if (trackDataCol != null)
                    row.Cells[trackDataCol.Index].Value = new PartTrack(new List<PartTrackEvent>()) { MidiProgramNumber = -1 };

                // Clear the Part description
                var descriptionCol = dgSong.Columns["colDescription"];
                if (descriptionCol != null)
                    row.Cells[descriptionCol.Index].Value = string.Empty;

                // Clear all measure cells for track
                SongGridManager.ClearMeasureCellsForRow(dgSong, row.Index);
            }

            dgSong.Refresh();
        }

        // AI: HandlePause: toggles play/pause using MidiPlaybackService public API. Catches invocation and general exceptions.
        public void HandlePause(MidiPlaybackService midiPlaybackService)
        {
            if (midiPlaybackService == null)
                return;

            try
            {
                if (midiPlaybackService.IsPlaying)
                {
                    midiPlaybackService.Pause();
                    return;
                }

                if (midiPlaybackService.IsPaused)
                {
                    midiPlaybackService.Resume();
                    return;
                }

                // Not playing and not paused -> do nothing .
            }
            catch (TargetInvocationException tie)
            {
                MessageBoxHelper.Show($"Playback control failed: {tie.InnerException?.Message ?? tie.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show($"Playback control failed: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void HandleSaveSelectedPhrases(DataGridView dgSong, SongContext? songContext)
        {
            ArgumentNullException.ThrowIfNull(dgSong);

            if (songContext == null)
            {
                ShowNoSongContextError();
                return;
            }

            var hasTrackSelection = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= SongGridManager.FIXED_ROWS_COUNT);

            if (!hasTrackSelection)
            {
                ShowNoSelectionError();
                return;
            }

            // ========================================================================================
            // This is to ensure bartrack is current - don't know if this is necessary. leave it for now, doesn't hurt.
            var timeSignatureRow = dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE];
            var timeSignatureTrack = timeSignatureRow.Cells["colData"].Value as Timingtrack;
            if (timeSignatureTrack == null || timeSignatureTrack.Events.Count == 0)
            {
                ShowMissingTimingError();
                return;
            }

            if (songContext.BarTrack.Bars.Count == 0)
            {
                songContext.BarTrack.RebuildFromTimingTrack(
                    timeSignatureTrack,
                    songContext.SectionTrack);
            }
            // ========================================================================================

            int savedCount = 0;

            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                if (selectedRow.Index < SongGridManager.FIXED_ROWS_COUNT)
                    continue;

                var dataObj = selectedRow.Cells["colData"].Value;
                if (dataObj is not PartTrack track || track.PartTrackNoteEvents.Count == 0)
                    continue;

                var instrObj = selectedRow.Cells["colType"].Value;
                int programNumber = (instrObj != null && instrObj != DBNull.Value) ? Convert.ToInt32(instrObj) : 255;
                if (programNumber == -1)  // what is this? TO DO
                    programNumber = 255;

                track.MidiProgramNumber = programNumber;

                var instrumentName = MidiVoices.MidiVoiceList()
                    .FirstOrDefault(voice => voice.ProgramNumber == programNumber)?.Name
                    ?? "Unknown Instrument";

                int phraseNumber = songContext.MaterialBank.GetPhrasesByMidiProgram(programNumber).Count + 1;
                string phraseId = Guid.NewGuid().ToString("N");
                string phraseName = $"{instrumentName} Phrase {phraseNumber}";
                string description = $"Phrase {_nextPhraseDescriptionNumber++}";
                int barCount = GetPhraseBarCount(songContext.BarTrack, track);
                int seed = track.Meta.Provenance?.BaseSeed ?? track.Meta.Provenance?.DerivedSeed ?? 0;

                var phrase = MaterialPhrase.FromPartTrack(
                    track,
                    phraseNumber,
                    phraseId,
                    phraseName,
                    description,
                    barCount,
                    seed);

                songContext.MaterialBank.AddPhrase(phrase);
                savedCount++;
            }

            if (savedCount > 0)
            {
                ShowSuccessMessage(savedCount);
            }
        }

        private static int GetPhraseBarCount(BarTrack barTrack, PartTrack track)
        {
            if (track.PartTrackNoteEvents.Count == 0 || barTrack.Bars.Count == 0)
                return 1;

            long firstTick = track.PartTrackNoteEvents.Min(e => e.AbsoluteTimeTicks);
            long lastTick = track.PartTrackNoteEvents.Max(e => e.AbsoluteTimeTicks + e.NoteDurationTicks);
            long phraseDuration = lastTick - firstTick;

            if (!barTrack.TryGetBar(1, out var bar1))
                return 1;

            long ticksPerBar = bar1.TicksPerMeasure;
            int barCount = (int)Math.Ceiling((double)phraseDuration / ticksPerBar);

            return Math.Max(1, barCount);
        }

        #region Error Messages

        private static void ShowNoSongContextError()
        {
            MessageBoxHelper.Show(
                "Song context is not available. Please load or create a design first.",
                "Save Phrase",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static void ShowNoSelectionError()
        {
            MessageBoxHelper.Show(
                "Please select one or more tracks to save as phrases.",
                "Save Phrase",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static void ShowMissingTimingError()
        {
            MessageBoxHelper.Show(
                "No time signature events defined. Please add at least one time signature event.",
                "Missing Time Signature",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private static void ShowSuccessMessage(int count)
        {
            MessageBoxHelper.Show(
                $"Successfully saved {count} phrase(s) to the material bank.",
                "Save Phrase",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        #endregion
    }
}
