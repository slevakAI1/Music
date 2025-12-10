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

            // Use PhraseGridManager to initialize the row consistently with other adds.
            PhraseGridManager.AddPhraseToGrid(emptyPhrase, _midiInstruments, dgvPhrase, ref phraseNumber);

            // Select the newly added row (last row)
            if (dgvPhrase.Rows.Count > PhraseGridManager.FIXED_ROWS_COUNT)
            {
                int newRowIndex = dgvPhrase.Rows.Count - 1;
                dgvPhrase.ClearSelection();
                dgvPhrase.Rows[newRowIndex].Selected = true;

                // Move current cell to an editable cell so the selection is visible and focusable
                var instrumentCol = dgvPhrase.Columns["colInstrument"];
                if (instrumentCol != null && dgvPhrase.Rows[newRowIndex].Cells[instrumentCol.Index] != null)
                {
                    dgvPhrase.CurrentCell = dgvPhrase.Rows[newRowIndex].Cells[instrumentCol.Index];
                }
            }
        }

        public void HandleDeletePhrases()
        {
            if (dgvPhrase.SelectedRows.Count == 0)
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
            var indices = dgvPhrase.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.Index)
                .Where(i => i >= PhraseGridManager.FIXED_ROWS_COUNT)
                .OrderByDescending(i => i)
                .ToList();

            foreach (var idx in indices)
            {
                if (idx >= PhraseGridManager.FIXED_ROWS_COUNT && idx < dgvPhrase.Rows.Count)
                    dgvPhrase.Rows.RemoveAt(idx);
            }
        }

        public void HandleClearPhrases()
        {
            // Remove all rows except the fixed rows
            while (dgvPhrase.Rows.Count > PhraseGridManager.FIXED_ROWS_COUNT)
            {
                dgvPhrase.Rows.RemoveAt(PhraseGridManager.FIXED_ROWS_COUNT);
            }
        }

        public void HandleClear()
        {
            if (dgvPhrase.SelectedRows == null || dgvPhrase.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Please select one or more phrase rows to clear.", "Clear Phrases", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (DataGridViewRow row in dgvPhrase.SelectedRows)
            {
                // Skip fixed rows
                if (row.Index < PhraseGridManager.FIXED_ROWS_COUNT)
                    continue;

                // Reset instrument to "Select..." (-1)
                var instrCol = dgvPhrase.Columns["colInstrument"];
                if (instrCol != null)
                    row.Cells[instrCol.Index].Value = -1;

                // Reset data to empty Phrase
                var dataCol = dgvPhrase.Columns["colData"];
                if (dataCol != null)
                    row.Cells[dataCol.Index].Value = new Phrase(new List<PhraseNote>()) { MidiProgramNumber = -1 };

                // Clear the Part description
                var descriptionCol = dgvPhrase.Columns["colDescription"];
                if (descriptionCol != null)
                    row.Cells[descriptionCol.Index].Value = string.Empty;

                // Clear all measure cells
                for (int colIndex = PhraseGridManager.MEASURE_START_COLUMN_INDEX; colIndex < dgvPhrase.Columns.Count; colIndex++)
                {
                    row.Cells[colIndex].Value = string.Empty;
                }
            }

            dgvPhrase.Refresh();
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
            var hasValidSelection = dgvPhrase.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= PhraseGridManager.FIXED_ROWS_COUNT);

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
            foreach (DataGridViewRow selectedRow in dgvPhrase.SelectedRows)
            {
                // Skip fixed rows
                if (selectedRow.Index < PhraseGridManager.FIXED_ROWS_COUNT)
                    continue;

                selectedRow.Cells["colData"].Value = phrase;
                
                // Update measure cells to show note distribution
                PhraseGridManager.PopulateMeasureCells(dgvPhrase, selectedRow.Index);
            }
        }

        /// <summary>
        /// Appends phrase notes to the existing Phrase objects in all selected rows.
        /// The appended notes' absolute positions are adjusted to start after the existing phrase ends.
        /// </summary>
        /// <param name="phrase">The phrase containing notes to append to the grid.</param>
        private void AppendPhraseNotesToSelectedRows(Phrase phrase)
        {
            foreach (DataGridViewRow selectedRow in dgvPhrase.SelectedRows)
            {
                // Skip fixed rows
                if (selectedRow.Index < PhraseGridManager.FIXED_ROWS_COUNT)
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
                PhraseGridManager.PopulateMeasureCells(dgvPhrase, selectedRow.Index);
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