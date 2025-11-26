#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

using Music.Designer;
using Music.Domain;
using Music.Tests;
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

            // Wire up event handlers
            dgvPhrase.CellValueChanged += DgvPhrase_CellValueChanged;
            dgvPhrase.CurrentCellDirtyStateChanged += DgvPhrase_CurrentCellDirtyStateChanged;
        }

        /// <summary>
        /// Commits the combo box edit immediately so CellValueChanged fires.
        /// </summary>
        private void DgvPhrase_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvPhrase.IsCurrentCellDirty && dgvPhrase.CurrentCell is DataGridViewComboBoxCell)
            {
                dgvPhrase.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// Updates the Phrase object's MidiPartName when the user changes the instrument selection.
        /// </summary>
        private void DgvPhrase_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            // Only handle changes to the instrument column
            if (e.RowIndex < 0 || e.ColumnIndex != dgvPhrase.Columns["colInstrument"]?.Index)
                return;

            var row = dgvPhrase.Rows[e.RowIndex];
            var cellValue = row.Cells["colData"].Value;

            // Check if the hidden data cell contains a Phrase object
            if (cellValue is Phrase phrase)
            {
                // Get the selected instrument name
                var selectedInstrumentName = row.Cells["colInstrument"].FormattedValue?.ToString();
                
                if (!string.IsNullOrEmpty(selectedInstrumentName))
                {
                    // Update the Phrase object's MidiPartName property
                    phrase.MidiPartName = selectedInstrumentName;
                    
                    // Optionally update the description column to reflect the change
                    row.Cells["colDescription"].Value = $"Part: {selectedInstrumentName}";
                }
            }
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
            HandleAppendNotes();
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

        private void btnExecute_Click(object sender, EventArgs e)
        {
            HandleExecute();
        }

        private void btnClearPhrases_Click(object sender, EventArgs e)
        {
            HandleClearPhrases();
        }
    }
}