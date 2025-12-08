using Music.Designer;
using Music.Domain;
using MusicXml;
using MusicXml.Domain;

namespace Music.Writer
{
    // Event handler logic extracted from WriterForm into a partial class
    public partial class WriterForm
    {
        // This creates a midi document from the selected phrases and plays them (simulaneously)
        public async Task HandlePlayAsync()
        {
            // Check if there are any rows in the grid
            if (dgvPhrase.Rows.Count == 0)
            {
                MessageBox.Show(this, "No pitch events to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if a row is selected
            if (dgvPhrase.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Please select a pitch event to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build list of Phrase from all selected rows
            var phrases = new List<Phrase>();
            foreach (DataGridViewRow selectedRow in dgvPhrase.SelectedRows)
            {
                // Validate instrument cell value first (may be DBNull or null)
                var instrObj = selectedRow.Cells["colInstrument"].Value;
                int programNumber = programNumber = Convert.ToInt32(instrObj);
                if (programNumber == -1)  // -1 = placeholder "Select..." -> treat as missing selection
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBox.Show(
                        this,
                        $"No instrument selected for row #{eventNumber}. Please select an instrument before playing.",
                        "Missing Instrument",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort playback
                }

                // Validate phrase data exists in hidden data column before using it
                var phrase = (Phrase)selectedRow.Cells["colData"].Value;
                if (phrase.PhraseNotes.Count == 0)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBox.Show(
                        this,
                        $"No phrase data for row #{eventNumber}. Please add or assign a phrase before playing.",
                        "Missing Phrase",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort playback
                }

                // Valid program number (0-127 or 255 for drums) — safe to cast now
                phrase.MidiProgramNumber = (int)programNumber;
                phrases.Add(phrase);
            }

//            try
//            {
                // Consolidated conversion: phrases -> midi document
                var midiDoc = PhrasesToMidiDocumentConverter.Convert(
                    phrases,
                    tempo: 112,
                    timeSignatureNumerator: 4,
                    timeSignatureDenominator: 4);

                await Player.PlayMidiFromPhrasesAsync(_midiPlaybackService, midiDoc, this);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(this, $"Error playing MIDI: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        }

        public void HandleUpdateFormFromDesigner()
        {
            // Update the form to take into account any changes to Designer
            Globals.Writer?.UpdateFromDesigner(_designer);
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);

            // Technical this can run upon activation too, but only in initialize phase, just that one time
        }

        //                      T E S T   S C E N A R I O   B U T T O N S

        // This sets design test scenario D1
        public void HandleSetDesignTestScenarioD1()
        {
            _designer ??= new Designer.Designer();
            DesignerTests.SetTestDesignD1(_designer);
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);
            // KEEP. MessageBox.Show("Test Design D1 has been applied to the current design.", "Design Test Scenario D1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // This sets writer test scenario G1
        // Description: Set writer test values using the current design 
        public void HandleSetWriterTestScenarioG1()
        {
            _writer = WriterFormTests.SetTestWriterG1(_designer);
            ApplyFormData(_writer);
            // KEEP. MessageBox.Show("Test Writer G1 has been applied to the current generator settings.", "Generator Test Scenario G1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void HandleChordTest()
        {
            if (_designer?.HarmonicTimeline == null || _designer.HarmonicTimeline.Events.Count == 0)
            {
                MessageBox.Show(this,
                    "No harmonic events available in the current design.",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var harmonicEvent = _designer.HarmonicTimeline.Events[1];

            List<PhraseNote> notes;
            try
            {
                notes = ChordConverter.Convert(
                    harmonicEvent.Key,
                    harmonicEvent.Degree,
                    harmonicEvent.Quality,
                    harmonicEvent.Bass,
                    baseOctave: 4);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Failed to build chord: {ex.Message}",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (notes == null || notes.Count == 0)
            {
                MessageBox.Show(this,
                    "Chord conversion returned no notes.",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var lines = new List<string>();
            foreach (var note in notes)
            {
                var accidental = note.Alter switch
                {
                    1 => "#",
                    -1 => "b",
                    _ => ""
                };
                lines.Add($"{note.Step}{accidental} {note.Octave}");
            }

            var title = $"Chord: {harmonicEvent.Key} (Deg {harmonicEvent.Degree}, {harmonicEvent.Quality})";
            MessageBox.Show(this,
                string.Join(Environment.NewLine, lines),
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        public void HandleExportToNotion()
        {
            // Ensure score list exists and has at least one score
            if (_scoreList == null || _scoreList.Count == 0)
            {
                MessageBox.Show(this, "No score to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var path = Path.Combine("..", "..", "..", "Files", "NotionExchange", "Score.musicxml");
                var fullPath = Path.GetFullPath(path);
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var xml = MusicXmlScoreSerializer.Serialize(_scoreList[0]);
                File.WriteAllText(fullPath, xml, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                MessageBox.Show(this, $"Exported to {fullPath}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error exporting MusicXML:\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void HandleClearPhrases()
        {
            dgvPhrase.Rows.Clear();
        }


        #region "Execute Commands"

        // Adds repeating notes to the phrases selected in the grid
        public void HandleRepeatNote(WriterFormData formData)
        {
            // Validate that phrases are selected before executing
            if (!ValidatePhrasesSelected())
                return;

            var (noteNumber, noteDurationTicks, repeatCount, isRest) =
                GetRepeatingNotesParameters(formData);

            var phrase = CreateRepeatingNotes.Execute(
                noteNumber: noteNumber,
                noteDurationTicks: noteDurationTicks,
                repeatCount: repeatCount,
                noteOnVelocity: 100,
                isRest: isRest);

            // Write the phrase to all selected rows
            WritePhraseToSelectedRows(phrase);
        }

        #endregion

        #region "Helper Methods"

        /// <summary>
        /// Validates that one or more phrase rows are selected in the grid.
        /// Shows a message box if no rows are selected.
        /// </summary>
        /// <returns>True if at least one row is selected, false otherwise.</returns>
        private bool ValidatePhrasesSelected()
        {
            if (dgvPhrase.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    this, 
                    "Please select one or more rows to apply this command.", 
                    "No Selection", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Writes a phrase object to the colData and colPhrase cells of all selected rows.
        /// </summary>
        /// <param name="phrase">The phrase to write to the grid.</param>
        private void WritePhraseToSelectedRows(Phrase phrase)
        {
            foreach (DataGridViewRow selectedRow in dgvPhrase.SelectedRows)
            {
                selectedRow.Cells["colData"].Value = phrase;
                selectedRow.Cells["colPhrase"].Value = "Contains Phrase Data";
            }
        }

        /// <summary>
        /// Extracts all repeating note parameters from form data.
        /// </summary>
        private static (int noteNumber, int noteDurationTicks, int repeatCount, bool isRest)
            GetRepeatingNotesParameters(WriterFormData formData)
        {
            // Extract repeat count
            var repeatCount = formData.NumberOfNotes ?? 1;

            // Extract rest flag
            var isRest = formData.IsRest ?? false;

            // Calculate MIDI note number from step, accidental, and octave
            var noteNumber = CalculateMidiNoteNumber(
                formData.Step,
                formData.OctaveAbsolute ?? 4,
                formData.Accidental);

            // Calculate note duration in ticks
            var noteDurationTicks = CalculateNoteDurationTicks(
                formData.NoteValue,
                formData.Dots,
                formData.TupletNumber,
                formData.TupletCount ?? 0,
                formData.TupletOf ?? 0);

            return (noteNumber, noteDurationTicks, repeatCount, isRest);
        }

        /// <summary>
        /// Calculates MIDI note number from musical note components.
        /// </summary>
        /// <param name="step">The note step (C, D, E, F, G, A, B)</param>
        /// <param name="octave">The octave number</param>
        /// <param name="accidental">The accidental string ("Sharp", "Flat", "Natural", etc.)</param>
        /// <returns>The MIDI note number (0-127)</returns>
        private static int CalculateMidiNoteNumber(char step, int octave, string? accidental)
        {
            // Convert accidental string to alter value
            int alter = accidental switch
            {
                "Sharp" or "#" => 1,
                "Flat" or "b" => -1,
                "Natural" => 0,
                _ => 0
            };

            // Calculate MIDI note number
            int baseNote = step switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => 0
            };

            return (octave + 1) * 12 + baseNote + alter;
        }

        /// <summary>
        /// Calculates note duration in MIDI ticks.
        /// </summary>
        /// <param name="noteValue">The note value string (e.g., "Quarter (1/4)" for quarter note)</param>
        /// <param name="dots">Number of dots to apply</param>
        /// <param name="tupletNumber">Optional tuplet identifier</param>
        /// <param name="tupletCount">Tuplet count (m in m:n tuplet)</param>
        /// <param name="tupletOf">Tuplet basis (n in m:n tuplet)</param>
        /// <returns>Duration in MIDI ticks</returns>
        private static int CalculateNoteDurationTicks(
            string? noteValue,
            int dots,
            string? tupletNumber,
            int tupletCount,
            int tupletOf)
        {
            const int ticksPerQuarterNote = 480;
            
            // Parse the duration from the display string format "Name (1/n)"
            // Examples: "Whole (1)", "Half (1/2)", "Quarter (1/4)", "Eighth (1/8)", "16th (1/16)", "32nd (1/32)"
            int duration = ParseNoteValueDuration(noteValue);

            // Base ticks for this duration (e.g., quarter=480, eighth=240)
            int baseTicks = (MusicConstants.TicksPerQuarterNote * 4) / duration;

            // Apply dots (each dot adds half of the previous value)
            int dottedTicks = ApplyDots(baseTicks, dots);

            // Apply tuplet if specified
            return ApplyTuplet(dottedTicks, tupletNumber, tupletCount, tupletOf);
        }

        /// <summary>
        /// Parses the numeric duration value from a note value display string.
        /// </summary>
        /// <param name="noteValue">Display string like "Quarter (1/4)" or "Eighth (1/8)"</param>
        /// <returns>The numeric duration (1, 2, 4, 8, 16, 32), defaulting to 4 (quarter note)</returns>
        private static int ParseNoteValueDuration(string? noteValue)
        {
            if (string.IsNullOrWhiteSpace(noteValue))
                return 4; // Default to quarter note

            // Extract the denominator from formats like "Quarter (1/4)" or "Whole (1)"
            // For "Whole (1)", the duration is 1
            // For others like "Quarter (1/4)", extract the "4"
            var parenIndex = noteValue.IndexOf('(');
            if (parenIndex < 0)
                return 4;

            var valuesPart = noteValue.Substring(parenIndex + 1).TrimEnd(')').Trim();
            
            // Handle "Whole (1)" case - no slash
            if (!valuesPart.Contains('/'))
            {
                return int.TryParse(valuesPart, out int wholeValue) && wholeValue == 1 ? 1 : 4;
            }

            // Handle fraction format like "1/4", "1/8", etc.
            var parts = valuesPart.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[1], out int denominator))
            {
                return denominator;
            }

            return 4; // Default to quarter note
        }

        /// <summary>
        /// Applies dot duration extensions to a base duration.
        /// </summary>
        /// <param name="baseTicks">The base duration in ticks</param>
        /// <param name="dots">Number of dots to apply</param>
        /// <returns>The dotted duration in ticks</returns>
        private static int ApplyDots(int baseTicks, int dots)
        {
            int dottedTicks = baseTicks;
            int addedValue = baseTicks / 2;
            
            for (int i = 0; i < dots; i++)
            {
                dottedTicks += addedValue;
                addedValue /= 2;
            }

            return dottedTicks;
        }

        /// <summary>
        /// Applies tuplet adjustment to a duration.
        /// </summary>
        /// <param name="dottedTicks">The dotted duration in ticks</param>
        /// <param name="tupletNumber">Optional tuplet identifier</param>
        /// <param name="tupletCount">Tuplet count (m in m:n tuplet)</param>
        /// <param name="tupletOf">Tuplet basis (n in m:n tuplet)</param>
        /// <returns>The tuplet-adjusted duration in ticks</returns>
        private static int ApplyTuplet(
            int dottedTicks,
            string? tupletNumber,
            int tupletCount,
            int tupletOf)
        {
            if (!string.IsNullOrWhiteSpace(tupletNumber) && tupletCount > 0 && tupletOf > 0)
            {
                // Tuplet adjusts duration: e.g., triplet = 2/3 of normal duration
                return (dottedTicks * tupletOf) / tupletCount;
            }

            return dottedTicks;
        }

        /// <summary>
        /// Deletes all selected rows from the phrase grid.
        /// Shows an informational message if nothing is selected.
        /// </summary>
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
            var indices = dgvPhrase.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.Index)
                .OrderByDescending(i => i)
                .ToList();

            foreach (var idx in indices)
            {
                if (idx >= 0 && idx < dgvPhrase.Rows.Count)
                    dgvPhrase.Rows.RemoveAt(idx);
            }
        }
        #endregion
    }
}