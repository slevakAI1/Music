using System.Reflection;

namespace Music.Writer
{
    // Grid-specific operations for WriterForm
    public partial class WriterForm
    {
        // ========== GRID ROW OPERATIONS ==========

        public void HandleAddPhrase()
        {
            // Create an empty Phrase and add it to the grid via the existing helper.
            var emptyPhrase = new Phrase(new List<PhraseNote>())
            {
                MidiProgramNumber = -1  // "Select..."
            };

            // Use SongGridManager to initialize the row consistently with other adds.
            SongGridManager.AddPhraseToGrid(emptyPhrase, _midiInstruments, dgSong, ref phraseNumber);

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

        public void HandleDeletePhrases()
        {
            if (dgSong.SelectedRows.Count == 0)
            {
                MessageBox.Show(this,
                    "Please select one or more rows to delete.",
                    "Delete Phrases",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Collect selected row indices and remove in descending order to avoid reindex issues
            // Skip fixed rows
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

        public void HandleClearAll()
        {
            // Remove all rows except the fixed rows
            while (dgSong.Rows.Count > SongGridManager.FIXED_ROWS_COUNT)
            {
                dgSong.Rows.RemoveAt(SongGridManager.FIXED_ROWS_COUNT);
            }
        }

        public void HandleClearSelected()
        {
            if (dgSong.SelectedRows == null || dgSong.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Please select one or more phrase rows to clear.", "Clear Phrases", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (DataGridViewRow row in dgSong.SelectedRows)
            {
                // Skip fixed rows
                if (row.Index < SongGridManager.FIXED_ROWS_COUNT)
                    continue;

                // Reset instrument to "Select..." (-1)
                var instrCol = dgSong.Columns["colInstrument"];
                if (instrCol != null)
                    row.Cells[instrCol.Index].Value = -1;

                // Reset data to empty Phrase
                var dataCol = dgSong.Columns["colData"];
                if (dataCol != null)
                    row.Cells[dataCol.Index].Value = new Phrase(new List<PhraseNote>()) { MidiProgramNumber = -1 };

                // Clear the Part description
                var descriptionCol = dgSong.Columns["colDescription"];
                if (descriptionCol != null)
                    row.Cells[descriptionCol.Index].Value = string.Empty;

                // Clear all measure cells
                for (int colIndex = SongGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgSong.Columns.Count; colIndex++)
                {
                    row.Cells[colIndex].Value = string.Empty;
                }
            }

            dgSong.Refresh();
        }

        // ========== GRID VALIDATION HELPERS ==========

        /// <summary>
        /// Validates that one or more phrase rows are selected in the grid.
        /// Shows a message box if no rows are selected.
        /// </summary>
        /// <returns>True if at least one row is selected, false otherwise.</returns>
        private bool ValidatePhrasesSelected()
        {
            // Check if any non-fixed rows are selected
            var hasValidSelection = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= SongGridManager.FIXED_ROWS_COUNT);

            if (!hasValidSelection)
            {
                MessageBox.Show(
                    this,
                    "Please select one or more phrase rows to apply this command.",
                    "No Selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        // ========== GRID DATA MANIPULATION ==========

        // KEEP THIS FOR FUTURE EXPANSION
        /// <summary>
        /// Writes a phrase object to the colData cell of all selected rows and updates measure display.
        /// </summary>
        /// <param name="phrase">The phrase to write to the grid.</param>
        private void WritePhraseToSelectedRows(Phrase phrase)
        {
            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                // Skip fixed rows
                if (selectedRow.Index < SongGridManager.FIXED_ROWS_COUNT)
                    continue;

                selectedRow.Cells["colData"].Value = phrase;
                
                // Update measure cells to show note distribution
                SongGridManager.PopulateMeasureCells(dgSong, selectedRow.Index);
            }
        }

        /// <summary>
        /// Appends phrase notes to the existing Phrase objects in all selected rows.
        /// The appended notes' absolute positions are adjusted to start after the existing phrase ends.
        /// </summary>
        /// <param name="phrase">The phrase containing notes to append to the grid.</param>
        private void AppendPhraseNotesToSelectedRows(Phrase phrase)
        {
            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                // Skip fixed rows
                if (selectedRow.Index < SongGridManager.FIXED_ROWS_COUNT)
                    continue;

                // Get existing phrase or create new one if null
                var existingPhrase = selectedRow.Cells["colData"].Value as Phrase;
                if (existingPhrase == null)
                {
                    // No existing phrase, create new one with the notes (no offset needed)
                    existingPhrase = new Phrase(new List<PhraseNote>(phrase.PhraseNotes));
                    selectedRow.Cells["colData"].Value = existingPhrase;
                }
                else
                {
                    // Calculate offset: find where the existing phrase ends
                    int offset = 0;
                    if (existingPhrase.PhraseNotes.Count > 0)
                    {
                        // Find the last note's end time (absolute position + duration)
                        var lastNote = existingPhrase.PhraseNotes
                            .OrderBy(n => n.AbsolutePositionTicks + n.NoteDurationTicks)
                            .Last();
                        offset = lastNote.AbsolutePositionTicks + lastNote.NoteDurationTicks;
                    }

                    // Append new notes with adjusted absolutePositions
                    foreach (var note in phrase.PhraseNotes)
                    {
                        var adjustedNote = new PhraseNote(
                            noteNumber: note.NoteNumber,
                            absolutePositionTicks: note.AbsolutePositionTicks + offset,
                            noteDurationTicks: note.NoteDurationTicks,
                            noteOnVelocity: note.NoteOnVelocity,
                            isRest: note.IsRest);

                        existingPhrase.PhraseNotes.Add(adjustedNote);
                    }
                }

                // Update measure cell display
                SongGridManager.PopulateMeasureCells(dgSong, selectedRow.Index);
            }
        }

        // ========== PLAYBACK CONTROL ==========

        /// <summary>
        /// Handles Pause/Resume logic for the shared MidiPlaybackService.
        /// Extracted from the form event handler to keep grid/event logic together.
        /// </summary>
        public void HandlePause()
        {
            // If there is no playback service, nothing to do.
            if (_midiPlaybackService == null)
                return;

            try
            {
                // Use the playback service's public API when available.
                if (_midiPlaybackService.IsPlaying)
                {
                    _midiPlaybackService.Pause();
                    return;
                }

                if (_midiPlaybackService.IsPaused)
                {
                    _midiPlaybackService.Resume();
                    return;
                }

                // Not playing and not paused -> nothing to do.
            }
            catch (TargetInvocationException tie)
            {
                MessageBox.Show(this, $"Playback control failed: {tie.InnerException?.Message ?? tie.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Playback control failed: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}