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

        // TO DO - should have a popup to view a phrase on clicking the phrase column. can display in JSON maybe
        // although that could be large for long phrases. This is a must though.

        private void btnExecute_Click(object sender, EventArgs e)
        {
            var command = cbCommand?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(command))
                return;

            switch (command)
            {
                case "Repeat Note":
                    HandleRepeatNote();
                    break;

                // Other cases will be added here later.

                default:
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
            var emptyPhrase = new Phrase(new List<PhraseNote>());

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
    }
}