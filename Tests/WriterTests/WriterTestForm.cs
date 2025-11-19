using Music.Designer;
using MusicXml;
using MusicXml.Domain;

namespace Music.Writer
{
    public partial class WriterTestForm : Form
    {
        private List<Score>? _scoreList;
        private Designer.Designer? _designer;
        private WriterTestData? _writer;
        private MeasureMeta _usedDivisionsPerMeasure;

        //===========================   I N I T I A L I Z A T I O N   ===========================
        public WriterTestForm()
        {
            InitializeComponent();

            // Initialize UsedDivisionsPerMeasure tracking object
            _usedDivisionsPerMeasure = new MeasureMeta();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            // Load current global score list and design into form-local fields for later use
            // Constructor is the only place that reads Globals per requirement.
            _scoreList = Globals.ScoreList;
            // Update score report display when we load the score
            if (_scoreList != null && _scoreList.Count > 0)
                txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);

            _designer = Globals.Designer;
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);


            // Initialize comboboxes - doesn't seem to be a way to set a default in the designer or form.
            // The changes keep getting discarded. wtf?
            cbChordBase.SelectedIndex = 0; // C
            cbChordQuality.SelectedIndex = 0; // Major
            cbChordKey.SelectedIndex = 0; // C

            // Initialize staff selection - default to staff 1 checked
            if (clbStaffs != null && clbStaffs.Items.Count > 0)
                clbStaffs.SetItemChecked(0, true); // Check staff "1"

            // ====================   T H I S   H A S   T O   B E   L A S T  !   =================

            // Capture form control values manually set in the form designer
            // This will only be done once, at form construction time.
            _writer ??= CaptureFormData();
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

            var config = _writer.ToAppendPitchEventsParams();
            AppendNotes.Execute(_scoreList[0], config, _usedDivisionsPerMeasure);
            txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);
            Globals.ScoreList = _scoreList;  // Note: Do this here for now because File Export MusicXml does not exit this form, so does not trigger Deactivate().
            //MessageBoxHelper.ShowMessage("Pattern has been applied to the score.", "Apply Pattern Set Notes");
        }


        private void btnNewScore_Click(object sender, EventArgs e)
        {
            var newScore = ScoreHelper.NewScore(this, _designer, clbParts, _usedDivisionsPerMeasure);
            if (newScore != null)
            {
                // Initialize score list if null
                if (_scoreList == null)
                    _scoreList = new List<Score>();
                
                // Replace score at index 0 or add if list is empty
                if (_scoreList.Count > 0)
                    _scoreList[0] = newScore;
                else
                    _scoreList.Add(newScore);
                
                txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);
            }
            // KEEP. MessageBox.Show(owner, "New score created from design.", "New Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _writer = WriterTests.SetTestWriterG1(_designer);
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

            List<PitchEvent> notes;
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
    }
}