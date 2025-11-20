using Music.Designer;
using MusicXml;
using MusicXml.Domain;

namespace Music.Writer
{
    public partial class WriterTestForm : Form
    {
        private List<Score> _scoreList;
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

            // Load current global score list
            _scoreList = Globals.ScoreList;
            if (_scoreList.Count > 0)
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

            cbPattern.SelectedIndex = 0;

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
            // Default movement title to date and time, format something like this: Sunday, Mar 3, 2025 3:05.34 PM
            var now = System.DateTime.Now;
            txtMovementTitle.Text = now.ToString("dddd, MMM d, yyyy h:mm'.'ss tt");

            var newScore = ScoreHelper.NewScore(
                this,
                _designer,
                clbParts,
                _usedDivisionsPerMeasure,
                txtMovementTitle.Text);

            // Reset current Score
            if (newScore != null)
            {
                if (_scoreList.Count > 0)
                    _scoreList[0] = newScore;
                else
                    _scoreList.Add(newScore);
                txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);
            }
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

        private void btnSaveScore_Click(object sender, EventArgs e)
        {
            // Ensure there is a current score to save
            if (_scoreList == null || _scoreList.Count == 0)
            {
                MessageBox.Show(this, "No score to save. Create or load a score first.", "Save Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var currentScore = _scoreList[0];
            var title = currentScore?.MovementTitle ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(this, "Movement title is empty. Set a movement title before saving.", "Save Score", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Prevent duplicate names (case-insensitive) in the list control
            for (int i = 0; i < lstScores.Items.Count; i++)
            {
                if (string.Equals(lstScores.Items[i]?.ToString(), title, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, $"A score named \"{title}\" already exists in the list. Skipping insert.", "Duplicate Score", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            lstScores.Items.Insert(0, title);
            lstScores.SelectedIndex = 0;

            // Insert the current score into the in-memory score list at index 1,
            // pushing existing items down while leaving _scoreList[0] (current) intact.
            int modelInsertIndex = 1;
            if (modelInsertIndex > _scoreList.Count)
                _scoreList.Add(currentScore);
            else
                _scoreList.Insert(modelInsertIndex, currentScore);

            // Persist into globals so other forms see the updated list
            Globals.ScoreList = _scoreList;
        }

        private void btnDeleteScore_Click(object sender, EventArgs e)
        {
            // Ensure a score is selected in the list control
            if (lstScores.SelectedIndex < 0)
            {
                MessageBox.Show(this, "No score selected. Select a score from the list to delete.", "Delete Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int controlIndex = lstScores.SelectedIndex;
            string? selectedTitle = lstScores.Items[controlIndex]?.ToString();

            // Confirm deletion
            var result = MessageBox.Show(this,
                $"Are you sure you want to delete the score \"{selectedTitle}\"?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            // Calculate the corresponding index in _scoreList
            // Control index 0 maps to _scoreList[1], control index 1 maps to _scoreList[2], etc.
            int scoreListIndex = controlIndex + 1;

            // Remove from the list control
            lstScores.Items.RemoveAt(controlIndex);

            // Remove from the in-memory score list (if index is valid)
            if (scoreListIndex >= 0 && scoreListIndex < _scoreList.Count)
            {
                _scoreList.RemoveAt(scoreListIndex);
            }

            // Persist changes to globals
            Globals.ScoreList = _scoreList;

            // Select the next item in the control if available, otherwise select the previous
            if (lstScores.Items.Count > 0)
            {
                int newSelection = Math.Min(controlIndex, lstScores.Items.Count - 1);
                lstScores.SelectedIndex = newSelection;
            }
        }
    }
}