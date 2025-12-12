using Music.Designer;
using Music.MyMidi;

namespace Music.Writer
{
    // Event handler logic extracted from WriterForm into a partial class
    // This file contains only direct UI event handlers
    public partial class WriterForm
    {
        // ========== PLAYBACK & EXPORT EVENT HANDLERS ==========

        // This creates a midi document from the selected phrases and plays them (simultaneously)
        public async Task HandlePlayAsync()
        {
            // Check if there are any phrase rows in the grid (excluding fixed rows)
            if (dgSong.Rows.Count <= PhraseGridManager.FIXED_ROWS_COUNT)
            {
                MessageBox.Show(this, "No pitch events to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if a phrase row is selected
            var hasPhraseSelection = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= PhraseGridManager.FIXED_ROWS_COUNT);

            if (!hasPhraseSelection)
            {
                MessageBox.Show(this, "Please select a pitch event to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build list of Phrase from all selected phrase rows (skip fixed rows)
            var phrases = new List<Phrase>();
            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                // Skip fixed rows
                if (selectedRow.Index < PhraseGridManager.FIXED_ROWS_COUNT)
                    continue;

                // Validate instrument cell value first (may be DBNull or null)
                var instrObj = selectedRow.Cells["colInstrument"].Value;
                int programNumber = Convert.ToInt32(instrObj);
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

                // Valid program number (0-127 or 255 for drums) – safe to cast now
                phrase.MidiProgramNumber = (int)programNumber;
                phrases.Add(phrase);
            }

            // Consolidated conversion: phrases -> midi document
            var midiDoc = ConvertListOfPhrasesToMidiSongDocument.Convert(
                phrases,
                tempo: 112,
                timeSignatureNumerator: 4,
                timeSignatureDenominator: 4);

            await Player.PlayMidiFromPhrasesAsync(_midiPlaybackService, midiDoc, this);
        }

        public void HandleExport()
        {
            // Check if there are any phrase rows in the grid (excluding fixed rows)
            if (dgSong.Rows.Count <= PhraseGridManager.FIXED_ROWS_COUNT)
            {
                MessageBox.Show(this, "No pitch events to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if a phrase row is selected
            var hasPhraseSelection = dgSong.SelectedRows
                .Cast<DataGridViewRow>()
                .Any(r => r.Index >= PhraseGridManager.FIXED_ROWS_COUNT);

            if (!hasPhraseSelection)
            {
                MessageBox.Show(this, "Please select a pitch event to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build list of Phrase from all selected phrase rows (skip fixed rows)
            var phrases = new List<Phrase>();
            foreach (DataGridViewRow selectedRow in dgSong.SelectedRows)
            {
                // Skip fixed rows
                if (selectedRow.Index < PhraseGridManager.FIXED_ROWS_COUNT)
                    continue;

                var instrObj = selectedRow.Cells["colInstrument"].Value;
                int programNumber = Convert.ToInt32(instrObj);

                if (programNumber == -1)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBox.Show(
                        this,
                        $"No instrument selected for row #{eventNumber}. Please select an instrument before exporting.",
                        "Missing Instrument",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort export
                }

                var phrase = (Phrase)selectedRow.Cells["colData"].Value;
                if (phrase.PhraseNotes.Count == 0)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBox.Show(
                        this,
                        $"No phrase data for row #{eventNumber}. Please add or assign a phrase before exporting.",
                        "Missing Phrase",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort export
                }

                // Preserve drum track indicator (255) or use selected program number
                phrase.MidiProgramNumber = (byte)programNumber;
                phrases.Add(phrase);
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "MIDI files (*.mid)|*.mid|All files (*.*)|*.*",
                Title = "Export MIDI File",
                DefaultExt = "mid",
                AddExtension = true
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                // Consolidated conversion: phrases -> midi document
                var midiDoc = ConvertListOfPhrasesToMidiSongDocument.Convert(
                    phrases,
                    tempo: 112,
                    timeSignatureNumerator: 4,
                    timeSignatureDenominator: 4);

                // Export to file
                _midiIoService.ExportToFile(sfd.FileName, midiDoc);

                MessageBox.Show(
                    this,
                    $"Successfully exported to:\n{Path.GetFileName(sfd.FileName)}",
                    "Export Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error exporting MIDI: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void HandleImport()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MIDI files (*.mid;*.midi)|*.mid;*.midi|All files (*.*)|*.*",
                Title = "Import MIDI File",
                CheckFileExists = true
            };

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                // Import the MIDI file using the existing service
                var midiDoc = _midiIoService.ImportFromFile(ofd.FileName);

                // Resolve project root (same approach used elsewhere in the solution)
                var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
                var debugDir = Path.Combine(projectRoot, "Files", "Debug");
                Directory.CreateDirectory(debugDir);

                // Convert MIDI document to lists of MetaMidiEvent objects
                List<List<MetaMidiEvent>> midiEventLists;
                try
                {
                    midiEventLists = ConvertMidiSongDocumentToMidiEventLists.Convert(midiDoc);
                }
                catch (NotSupportedException ex)
                {
                    // Show detailed error about unsupported MIDI event
                    MessageBox.Show(
                        this,
                        ex.Message,
                        "Unsupported MIDI Event",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Extract ticks per quarter note from the MIDI file
                short ticksPerQuarterNote = MusicConstants.TicksPerQuarterNote; // Default
                if (midiDoc.Raw.TimeDivision is Melanchall.DryWetMidi.Core.TicksPerQuarterNoteTimeDivision tpqn)
                {
                    ticksPerQuarterNote = tpqn.TicksPerQuarterNote;
                }

                // Convert MetaMidiEvent lists to Phrase objects, passing the source ticks per quarter note
                var phrases = ConvertMidiEventListsToPhraseLists.ConvertMidiEventListsToPhraseList(
                    midiEventLists,
                    _midiInstruments,
                    ticksPerQuarterNote);

                // Add each phrase to the grid
                foreach (var phrase in phrases)
                {
                    PhraseGridManager.AddPhraseToGrid(
                        phrase,
                        _midiInstruments,
                        dgSong,
                        ref phraseNumber);
                }

                MessageBox.Show(
                    this,
                    $"Successfully imported {phrases.Count} track(s) from:\n{Path.GetFileName(ofd.FileName)}",
                    "Import Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Error importing MIDI file:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ========== DESIGNER & FORM SYNC EVENT HANDLERS ==========

        public void HandleUpdateFormFromDesigner()
        {
            // Update the form to take into account any changes to Designer
            UpdateWriterFormDataFromDesigner.Update(_writer, _designer);
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);
        }

        // ========== TEST SCENARIO EVENT HANDLERS ==========

        public void HandleSetDesignTestScenarioD1()
        {
            _designer ??= new Designer.Designer();
            DesignerTests.SetTestDesignD1(_designer);


            // TO DO - Populate all 4 fixed control lines


            GridControlLinesManager.AttachTempoTimeline(dgSong, _designer.TempoTimeline);



            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);
        }

        public void HandleSetWriterTestScenarioG1()
        {
            _writer = WriterFormTests.SetTestWriterG1(_designer);
            ApplyFormData(_writer);
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
                notes = ConvertHarmonicEventToListOfPhraseNotes.Convert(
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

        // ========== SCORE EVENT HANDLERS ==========

        public void HandleExportToNotion()
        {
        //    // Ensure score list exists and has at least one score
        //    if (_scoreList == null || _scoreList.Count == 0)
        //    {
        //        MessageBox.Show(this, "No score to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        return;
        //    }

        //    try
        //    {
        //        var path = Path.Combine("..", "..", "..", "Files", "NotionExchange", "Score.musicxml");
        //        var fullPath = Path.GetFullPath(path);
        //        var dir = Path.GetDirectoryName(fullPath);
        //        if (!string.IsNullOrEmpty(dir))
        //            Directory.CreateDirectory(dir);

        //        var xml = MusicXmlScoreSerializer.Serialize(_scoreList[0]);
        //        File.WriteAllText(fullPath, xml, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        //        MessageBox.Show(this, $"Exported to {fullPath}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(this, $"Error exporting MusicXML:\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        }

        public void HandleNewScore()
        {
            //// Resolve Movement Title to use for the new score
            //var movementTitle = txtMovementTitle.Text;
            //if (movementTitle == "")
            //{
            //    var now = System.DateTime.Now;
            //    movementTitle = now.ToString("dddd, MMM d, yyyy h:mm'.'ss tt");
            //}

            //var newScore = ScoreHelper.CreateNewScore(
            //    _designer,
            //    ref _measureMeta,
            //    movementTitle);

            //// Set current score to newly created score and update 
            //if (newScore != null)
            //{
            //    if (_scoreList.Count > 0)
            //        _scoreList[0] = newScore;
            //    else
            //        _scoreList.Add(newScore);
            //    txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);
            //}

            //// Clear the movement title textbox
            //txtMovementTitle.Text = "";
        }

        // ========== GRID CELL EVENT HANDLERS ==========

        public void HandlePhraseDoubleClick(DataGridViewCellEventArgs e)
        {
            // Skip fixed rows
            if (e.RowIndex < PhraseGridManager.FIXED_ROWS_COUNT)
                return;

            // Allow double-click on any measure column (starting from MEASURE_START_COLUMN_INDEX)
            if (e.ColumnIndex < PhraseGridManager.MEASURE_START_COLUMN_INDEX)
                return;

            var row = dgSong.Rows[e.RowIndex];
            var phraseData = row.Cells["colData"].Value;

            // Validate that we have a Phrase object
            if (phraseData is not Phrase phrase)
            {
                MessageBox.Show(this,
                    "No phrase data available for this row.",
                    "View Phrase",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Get the phrase number for the dialog title
            var phraseNumber = row.Cells["colEventNumber"].Value?.ToString() ?? (e.RowIndex + 1).ToString();

            // Open the JSON viewer dialog
            using var viewer = new PhraseJsonViewer(phrase, phraseNumber);
            viewer.ShowDialog(this);
        }
    }
}