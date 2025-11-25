using Music.Designer;
using Music.Domain;
using Music.Tests;
using MusicXml;
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
        private IMidiPlaybackService _midiPlaybackService;

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
            ConfigurePhraseDataGridView();

            // ====================   T H I S   H A S   T O   B E   L A S T  !   =================

            // Capture form control values manually set in the form designer
            // This will only be done once, at form construction time.
            _writer ??= CaptureFormData();
        }

        /// <summary>
        /// Configures the dgvPhrase DataGridView with proper columns including MIDI instrument dropdown.
        /// </summary>
        private void ConfigurePhraseDataGridView()
        {
            dgvPhrase.AllowUserToAddRows = false;
            dgvPhrase.AllowUserToResizeColumns = true;
            dgvPhrase.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPhrase.MultiSelect = true; // Changed from false to true
            dgvPhrase.ReadOnly = false; // Allow editing the combo box column
            dgvPhrase.Columns.Clear();

            // Column 0: Hidden column containing the AppendNoteEventsToScoreParams object
            var colData = new DataGridViewTextBoxColumn
            {
                Name = "colData",
                HeaderText = "Data",
                Visible = false,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colData);

            // Column 1: MIDI Instrument dropdown (editable)
            var colInstrument = new DataGridViewComboBoxColumn
            {
                Name = "colInstrument",
                HeaderText = "Instrument",
                Width = 200,
                DataSource = new List<MidiInstrument>(_midiInstruments),
                DisplayMember = "Name",
                ValueMember = "ProgramNumber",
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = false
            };
            dgvPhrase.Columns.Add(colInstrument);

            // Column 2: Event number (read-only)
            var colEventNumber = new DataGridViewTextBoxColumn
            {
                Name = "colEventNumber",
                HeaderText = "#",
                Width = 50,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colEventNumber);

            // Column 3: Description (read-only for now)
            var colDescription = new DataGridViewTextBoxColumn
            {
                Name = "colDescription",
                HeaderText = "Description",
                Width = 300,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colDescription);

            // Column 4: Phrase details (fills remaining space)
            var colPhrase = new DataGridViewTextBoxColumn
            {
                Name = "colPhrase",
                HeaderText = "Phrase",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dgvPhrase.Columns.Add(colPhrase);
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


        // Inserts notes based on the "number of notes" parameter from the writer form
        private void btnAppendNotes_Click(object sender, EventArgs e)
        {
            _writer = CaptureFormData();

            // Ensure score list exists and has at least one score
            if (_scoreList == null || _scoreList.Count == 0)
            {
                MessageBox.Show(this, "No score available. Please create a new score first.",
                    "No Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // THIS SHOULD NOW BE "CREATE PITCH EVENT" and and SHOW IN THE NEW CONTROL 

            var config = _writer.ToAppendNoteEventsParams();

            // THIS SHOULD APPEND FROM WHATS SELECTED IN THE PHRASE  CONTROL

            AppendNotes.Execute(_scoreList[0], config, _measureMeta);

            txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);
            Globals.ScoreList = _scoreList;  // Note: Do this here for now because File Export MusicXml does not exit this form, so does not trigger Deactivate().
        }

        //  Adds to THE dgvPhrase datagridview CONTROL 
        private void ExecuteCommandRepeatNoteChordRest(WriterFormData _writer)
        {



            // THIS NEEDS TO BE TOPHRASE. KEEP THE CURRENT ONE FOR XML MAY BE NEEDED
            var phrase = _writer.ToAppendNoteEventsParams();




            // Set phrase name/number
            phraseNumber++;
            var phraseName = phraseNumber.ToString();

            // Get part name from the phrase (assuming first part)
            var partName = phrase.Parts?.FirstOrDefault() ?? "Unknown";

            // Add new row
            int newRowIndex = dgvPhrase.Rows.Add();
            var row = dgvPhrase.Rows[newRowIndex];

            // Column 0: Hidden data (AppendNoteEventsToScoreParams object)
            row.Cells["colData"].Value = phrase;

            // Column 1: MIDI Instrument dropdown - set to first instrument (Acoustic Grand Piano) as default
            // User can change this by clicking the dropdown
            row.Cells["colInstrument"].Value = _midiInstruments[0].ProgramNumber;

            // Column 2: Event number
            row.Cells["colEventNumber"].Value = phraseName;

            // Column 3: Description
            row.Cells["colDescription"].Value = $"Part: {partName}";

            // Column 4: Phrase details (placeholder)
            row.Cells["colPhrase"].Value = "tbd";
        }

        /// <summary>
        /// Converts the provided list of AppendNoteEventsToScoreParams to MIDI and plays it back.
        /// Stops and releases the MIDI device after playback completes.
        /// </summary>
        private async Task PlayMidiFromPhrasesAsync(List<AppendNoteEventsToScoreParams> configs)
        {
            if (configs == null || configs.Count == 0) 
                throw new ArgumentNullException(nameof(configs));

            // Always stop any existing playback first
            _midiPlaybackService.Stop();

            // Convert to MIDI (using the list overload that creates multiple tracks)
            var midiDoc = NoteEventsToMidiConverter.Convert(configs);

            // Select first available output device
            var devices = _midiPlaybackService.EnumerateOutputDevices().ToList();
            if (devices.Count == 0)
            {
                MessageBox.Show(this, "No MIDI output device found.", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _midiPlaybackService.SelectOutput(devices[0]);
            _midiPlaybackService.Play(midiDoc);

            // Wait for playback duration plus buffer
            var duration = midiDoc?.Duration ?? TimeSpan.Zero;
            var totalDelay = duration.TotalMilliseconds + 250;
            
            if (totalDelay > 0)
                await Task.Delay((int)Math.Min(totalDelay, int.MaxValue));

            // Always stop to release resources
            _midiPlaybackService.Stop();
        }


        private void btnUpdateFormFromDesigner_Click(object sender, EventArgs e)
        {
            // Update the form to take into account any changes to Designer
            Globals.Writer?.UpdateFromDesigner(_designer);
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);

            // Technical this can run upon activation too, but only in initialize phase, just that one time
        }

        //===========================================================================================
        //                      T E S T   S C E N A R I O   B U T T O N S
        //  

        // This sets design test scenario D1
        private void btnSetDesignTestScenarioD1_Click(object sender, EventArgs e)
        {
            _designer ??= new Designer.Designer();
            DesignerTests.SetTestDesignD1(_designer);
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);
            // KEEP. MessageBox.Show("Test Design D1 has been applied to the current design.", "Design Test Scenario D1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // This sets writer test scenario G1
        // Description: Set writer test values using the current design 
        private void btnSetWriterTestScenarioG1_Click(object sender, EventArgs e)
        {
            _writer = WriterFormTests.SetTestWriterG1(_designer);
            ApplyFormData(_writer);
            // KEEP. MessageBox.Show("Test Writer G1 has been applied to the current generator settings.", "Generator Test Scenario G1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnChordTest_Click(object sender, EventArgs e)
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

            List<NoteEvent> notes;
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

        private void btnExportToNotion_Click(object sender, EventArgs e)
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

        // Branch on Command
        private void btnExecute_Click(object sender, EventArgs e)
        {
            var pattern = cbCommand?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(pattern))
                return;

            switch (pattern)
            {
                case "Repeat Note/Chord/Rest":
                    var formData = CaptureFormData();
                    ExecuteCommandRepeatNoteChordRest(formData);
                    break;

                // Add additional cases for other patterns as needed
                default:
                    // No-op for unrecognized patterns
                    break;
            }
        }

        private async void btnPlay_Click(object sender, EventArgs e)
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

            // Build list of AppendNoteEventsToScoreParams from all selected rows
            var configList = new List<AppendNoteEventsToScoreParams>();

            foreach (DataGridViewRow selectedRow in dgvPhrase.SelectedRows)
            {
                var cellValue = selectedRow.Cells["colData"].Value;

                if (cellValue is not AppendNoteEventsToScoreParams config)
                {
                    MessageBox.Show(this, $"Invalid data in row {selectedRow.Index}.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get the selected instrument name for this row
                var selectedInstrumentName = selectedRow.Cells["colInstrument"].FormattedValue?.ToString() ?? "Acoustic Grand Piano";

                // Update the part name with the selected instrument name
                config.Parts.Clear();
                config.Parts.Add(selectedInstrumentName);

                configList.Add(config);
            }

            try
            {
                await PlayMidiFromPhrasesAsync(configList);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error playing MIDI: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Gets the selected MIDI instrument program number for a given row.
        /// Returns null if no valid selection exists.
        /// </summary>
        private byte? GetSelectedMidiProgram(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvPhrase.Rows.Count)
                return null;

            var cell = dgvPhrase.Rows[rowIndex].Cells["colInstrument"];
            if (cell.Value is byte programNumber)
                return programNumber;

            return null;
        }

        /// <summary>
        /// Gets the selected MIDI instrument for a given row.
        /// Returns null if no valid selection exists.
        /// </summary>
        private MidiInstrument? GetSelectedMidiInstrument(int rowIndex)
        {
            var programNumber = GetSelectedMidiProgram(rowIndex);
            if (programNumber == null)
                return null;

            return _midiInstruments.FirstOrDefault(i => i.ProgramNumber == programNumber.Value);
        }
    }
}