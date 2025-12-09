#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

using Music.Designer;
using Music.MyMidi;
using MusicXml.Domain;

namespace Music.Writer
{
    public partial class WriterForm : Form
    {
        private List<Score> _scoreList;
        private Designer.Designer? _designer;
        private WriterFormData? _writer;
        private MeasureMeta _measureMeta;

        // playback service (reused for multiple play calls)
        private MidiPlaybackService _midiPlaybackService;

        // MIDI I/O service for importing/exporting MIDI files
        private MidiIoService _midiIoService;

        private int phraseNumber = 0;

        // MIDI instrument list for dropdown
        private List<MidiInstrument> _midiInstruments;

        //===========================   I N I T I A L I Z A T I O N   ===========================
        public WriterForm()
        {
            InitializeComponent();

            // Initialize MeasureMeta tracking object
            _measureMeta = new MeasureMeta();

            // create playback service
            _midiPlaybackService = new MidiPlaybackService();

            // Initialize MIDI I/O service
            _midiIoService = new MidiIoService();

            // Initialize MIDI instruments list
            _midiInstruments = MidiInstrument.GetGeneralMidiInstruments();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            // Load current global score list
            _scoreList = Globals.ScoreList;
            if (_scoreList.Count > 0)
                txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);

            _designer = Globals.Designer;
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);

            dgvPhrase.DefaultCellStyle.ForeColor = Color.Black; // had trouble setting this in the forms designer
            dgvPhrase.DefaultCellStyle.BackColor = Color.White;


            // Initialize comboboxes - doesn't seem to be a way to set a default in the designer or form.
            // The changes keep getting discarded. wtf?
            cbChordBase.SelectedIndex = 0; // C
            cbChordQuality.SelectedIndex = 0; // Major
            cbChordKey.SelectedIndex = 0; // C

            // Initialize staff selection - default to staff 1 checked
            if (clbStaffs != null && clbStaffs.Items.Count > 0)
                clbStaffs.SetItemChecked(0, true); // Check staff "1"

            cbCommand.SelectedIndex = 0;

            // Configure dgvPhrases with MIDI instrument dropdown
            PhraseGridManager.ConfigurePhraseDataGridView(
                dgvPhrase,
                _midiInstruments,
                DgvPhrase_CellValueChanged,
                DgvPhrase_CurrentCellDirtyStateChanged);

            // ====================   T H I S   H A S   T O   B E   L A S T  !   =================

            // Capture form control values manually set in the form designer
            // This will only be done once, at form construction time.
            _writer ??= CaptureFormData();
        }

        /// <summary>
        /// Commits the combo box edit immediately so CellValueChanged fires.
        /// </summary>
        private void DgvPhrase_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            PhraseGridManager.HandleCurrentCellDirtyStateChanged(dgvPhrase, sender, e);
        }

        /// <summary>
        /// Updates the Phrase object's MidiProgramName when the user changes the instrument selection.
        /// </summary>
        private void DgvPhrase_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            PhraseGridManager.HandleCellValueChanged(dgvPhrase, sender, e);
        }

        /// <summary>
        /// Opens a JSON viewer when the user double-clicks on the Phrase column.
        /// </summary>
        private void DgvPhrase_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            // Only handle double-clicks on the Phrase column (not header row)
            if (e.RowIndex < 0)
                return;

            var phraseColumnIndex = dgvPhrase.Columns["colPhrase"]?.Index;
            if (phraseColumnIndex == null || e.ColumnIndex != phraseColumnIndex.Value)
                return;

            var row = dgvPhrase.Rows[e.RowIndex];
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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        // The Writer form is activated each time it gains focus.
        // The initialization of controls is controlled entirely by the current Design and persisted Writer.
        // It does not depend on the prior state of the controls.
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Get from globals on the way in but not if null, would overwrite current state

            if (Globals.ScoreList != null && Globals.ScoreList.Count > 0)
            {
                _scoreList = Globals.ScoreList;
                // Update score report display when score is refreshed from globals
                txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);
            }
            if (Globals.Designer != null)
            {
                _designer = Globals.Designer;
                txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);
            }
            if (Globals.Writer != null)
                _writer = Globals.Writer;

            ApplyFormData(_writer);
        }

        // Persist current control state whenever the form loses activation (user switches to another MDI child)
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);

            // Save on the way out
            Globals.ScoreList = _scoreList;
            Globals.Designer = _designer;
            _writer = Globals.Writer = CaptureFormData();
            Globals.Writer = _writer;
        }

        //===============================   E V E N T S   ==============================


        private void btnAppendNotes_Click(object sender, EventArgs e)
        {
            //HandleAppendNotes();
        }

        private async void btnPlay_Click(object sender, EventArgs e)
        {
            await HandlePlayAsync();
        }

        private void btnUpdateFormFromDesigner_Click(object sender, EventArgs e)
        {
            HandleUpdateFormFromDesigner();
        }

        private void btnSetDesignTestScenarioD1_Click(object sender, EventArgs e)
        {
            HandleSetDesignTestScenarioD1();
        }

        private void btnSetWriterTestScenarioG1_Click(object sender, EventArgs e)
        {
            HandleSetWriterTestScenarioG1();
        }

        private void btnChordTest_Click(object sender, EventArgs e)
        {
            HandleChordTest();
        }

        private void btnExportToNotion_Click(object sender, EventArgs e)
        {
            HandleExportToNotion();
        }

        // TO DO  -  Maybe the writing out to the grid should happen here and
        //           each command returns the data to write out ??? what data would this require?


        // TO DO - ALWAYS APPEND TO SELECTED ROWS - this should cover everything - even
        // add an added row.

        private void btnExecute_Click(object sender, EventArgs e)
        {
            var command = cbCommand?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(command))
                return;

            // Capture form data once at the higher level and pass to command handlers
            var formData = CaptureFormData();

            switch (command)
            {
                case "Repeat Note":
                    HandleRepeatNote(formData);
                    break;

                // Other cases will be added here later.

                default:
                    // Do nothing - do not change this branch code ever
                    break;
            }
        }

        private void btnClearPhrases_Click(object sender, EventArgs e)
        {
            HandleClearPhrases();
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            // Resolve Movement Title to use for the new score
            var movementTitle = txtMovementTitle.Text;
            if (movementTitle == "")
            {
                var now = System.DateTime.Now;
                movementTitle = now.ToString("dddd, MMM d, yyyy h:mm'.'ss tt");
            }

            var newScore = ScoreHelper.CreateNewScore(

                _designer,
                ref _measureMeta,
                movementTitle);

            // Set current score to newly created score and update 
            if (newScore != null)
            {
                if (_scoreList.Count > 0)
                    _scoreList[0] = newScore;
                else
                    _scoreList.Add(newScore);
                txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);
            }

            // Clear the movement title textbox
            txtMovementTitle.Text = "";
        }

        // New Add button handler: add an empty phrase row and select it.
        private void btnAddPhrase_Click(object? sender, EventArgs e)
        {
            // Create an empty Phrase and add it to the grid via the existing helper.
            var emptyPhrase = new Phrase(new List<PhraseNote>())
            {
                MidiProgramNumber = -1  // "Select..."
            };

            // Use PhraseGridManager to initialize the row consistently with other adds.
            PhraseGridManager.AddPhraseToGrid(emptyPhrase, _midiInstruments, dgvPhrase, ref phraseNumber);

            // Select the newly added row (last row)
            if (dgvPhrase.Rows.Count > 0)
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

        private void btnDeletePhrases_Click(object sender, EventArgs e)
        {
            HandleDeletePhrases();
        }

        private void btnImport_Click(object sender, EventArgs e)
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

                // Write first debug JSON
                //var json1 = Helpers.DebugObject(midiDoc) ?? string.Empty;
                //File.WriteAllText(Path.Combine(debugDir, "json1.json"), json1);

                // Convert MIDI document to lists of MetaMidiEvent objects
                List<List<MetaMidiEvent>> midiEventLists;
                try
                {
                    midiEventLists = ConvertMidiSongDocumentToMidiEventLists.Convert(midiDoc);

                    //var json2 = Helpers.DebugObject(midiEventLists) ?? string.Empty;
                    //File.WriteAllText(Path.Combine(debugDir, "json2.json"), json2);
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

                //var json3 = Helpers.DebugObject(phrases) ?? string.Empty;
                //File.WriteAllText(Path.Combine(debugDir, "json3.json"), json3);

                // Add each phrase to the grid
                foreach (var phrase in phrases)
                {
                    PhraseGridManager.AddPhraseToGrid(
                        phrase,
                        _midiInstruments,
                        dgvPhrase,
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

        private void btnExport_Click(object sender, EventArgs e)
        {
            // Check if there are any rows in the grid
            if (dgvPhrase.Rows.Count == 0)
            {
                MessageBox.Show(this, "No pitch events to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if a row is selected
            if (dgvPhrase.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Please select a pitch event to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build list of Phrase from all selected rows
            var phrases = new List<Phrase>();
            foreach (DataGridViewRow selectedRow in dgvPhrase.SelectedRows)
            {
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

        private void btnStop_Click(object sender, EventArgs e)
        {
            _midiPlaybackService.Stop();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (dgvPhrase.SelectedRows == null || dgvPhrase.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Please select one or more phrase rows to clear.", "Clear Phrases", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (DataGridViewRow row in dgvPhrase.SelectedRows)
            {
                // Reset instrument to "Select..." (-1)
                var instrCol = dgvPhrase.Columns["colInstrument"];
                if (instrCol != null)
                    row.Cells[instrCol.Index].Value = -1;

                // Reset data to empty Phrase
                var dataCol = dgvPhrase.Columns["colData"];
                if (dataCol != null)
                    row.Cells[dataCol.Index].Value = new Phrase(new List<PhraseNote>()) { MidiProgramNumber = -1 };

                // Clear the Part description (should be empty, not "Part: Select...")
                var descriptionCol = dgvPhrase.Columns["colDescription"];
                if (descriptionCol != null)
                    row.Cells[descriptionCol.Index].Value = string.Empty;

                // Set Phrase column to "Empty phrase"
                var phraseCol = dgvPhrase.Columns["colPhrase"];
                if (phraseCol != null)
                    row.Cells[phraseCol.Index].Value = "Empty phrase";
            }

            dgvPhrase.Refresh();
        }
    }
}