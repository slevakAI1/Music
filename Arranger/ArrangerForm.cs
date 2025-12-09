using System;
using System.IO;
using System.Windows.Forms;
using Music.Designer;
using MusicXml;
using MusicXml.Domain;

namespace Music.Writer
{
    public partial class ArrangerForm : Form
    {
        private List<Score> _scoreList;
        private Designer.Designer? _designer;
        private WriterFormData? _writer;
        private MeasureMeta _measureMeta;

        //===========================   I N I T I A L I Z A T I O N   ===========================
        public ArrangerForm()
        {
            InitializeComponent();

            // Initialize MeasureMeta tracking object
            _measureMeta = new MeasureMeta();

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

            // Initialize staff selection - default to staff 1 checked
            if (clbStaffs != null && clbStaffs.Items.Count > 0)
                clbStaffs.SetItemChecked(0, true); // Check staff "1"

            // ====================   T H I S   H A S   T O   B E   L A S T  !   =================

            // Capture form control values manually set in the form designer
            // This will only be done once, at form construction time.
            // _writer ??= CaptureFormData();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        // Activated each time it gains focus.
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

            // ApplyFormData(_writer);
        }

        // Persist current control state whenever the form loses activation (user switches to another MDI child)
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);

            // Save on the way out
            Globals.ScoreList = _scoreList;
            Globals.Designer = _designer;
            // _writer = Globals.Writer = CaptureFormData();
            Globals.Writer = _writer;
        }

        //===============================   E V E N T S   ==============================

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

        private void btnUpdateFormFromDesigner_Click(object sender, EventArgs e)
        {
            // Update the form to take into account any changes to Designer
            Globals.Writer?.Update(_designer);
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);
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

        private void btnAddScore_Click(object sender, EventArgs e)
        {
            var currentScore = _scoreList[0];

            // Prevent duplicate names (case-insensitive) in the list control
            for (int i = 0; i < lstScores.Items.Count; i++)
            {
                if (string.Equals(lstScores.Items[i]?.ToString(), currentScore.MovementTitle, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, $"A score named \"{currentScore.MovementTitle}\" already exists in the list. Skipping insert.", "Duplicate Score", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Add to list control at index 0
            lstScores.Items.Insert(0, currentScore.MovementTitle);
            lstScores.SelectedIndex = 0;

            // Add the score to the score list using the helper method
            ScoreHelper.AddScoreToScoreList(currentScore, _scoreList);

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

        private void btnUpdateScore_Click(object sender, EventArgs e)
        {
            // Ensure there is a current score to update from
            if (_scoreList == null || _scoreList.Count == 0)
            {
                MessageBox.Show(this, "No current score available. Create or load a score first.", "Update Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ensure a score is selected in the list control
            if (lstScores.SelectedIndex < 0)
            {
                MessageBox.Show(this, "No score selected. Select a score from the list to update.", "Update Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int controlIndex = lstScores.SelectedIndex;
            string? selectedTitle = lstScores.Items[controlIndex]?.ToString();
            var currentScore = _scoreList[0];

            // Confirm update
            var result = MessageBox.Show(this,
                $"Are you sure you want to overwrite the score \"{selectedTitle}\" with the current score?",
                "Confirm Update",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            // Calculate the corresponding index in _scoreList
            // Control index 0 maps to _scoreList[1], control index 1 maps to _scoreList[2], etc.
            int scoreListIndex = controlIndex + 1;

            // Update only the Parts of the selected score, preserving its MovementTitle
            if (scoreListIndex >= 0 && scoreListIndex < _scoreList.Count)
            {
                var targetScore = _scoreList[scoreListIndex];

                // Copy only the Parts from current score to target score
                // Keep the target's original MovementTitle
                targetScore.Parts = currentScore.Parts;
            }

            // Persist changes to globals
            Globals.ScoreList = _scoreList;

            // Keep the same item selected
            lstScores.SelectedIndex = controlIndex;
        }

        private void btnLoadScore_Click(object sender, EventArgs e)
        {
            // Ensure score list exists and has at least one score
            if (_scoreList == null || _scoreList.Count == 0)
            {
                MessageBox.Show(this, "No current score available. Create a score first.", "Load Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ensure a score is selected in the list control
            if (lstScores.SelectedIndex < 0)
            {
                MessageBox.Show(this, "No score selected. Select a score from the list to load.", "Load Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int controlIndex = lstScores.SelectedIndex;
            string? selectedTitle = lstScores.Items[controlIndex]?.ToString();

            // Confirm load
            var result = MessageBox.Show(this,
                $"Are you sure you want to load the score \"{selectedTitle}\" into the current score?",
                "Confirm Load",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            // Calculate the corresponding index in _scoreList
            // Control index 0 maps to _scoreList[1], control index 1 maps to _scoreList[2], etc.
            int scoreListIndex = controlIndex + 1;

            // Copy only the Parts from the selected score to the current score
            if (scoreListIndex >= 0 && scoreListIndex < _scoreList.Count)
            {
                var sourceScore = _scoreList[scoreListIndex];
                var currentScore = _scoreList[0];

                // Copy Parts from selected score to current score
                // Keep the current score's MovementTitle
                currentScore.Parts = sourceScore.Parts;

                // Update the score report to show the loaded score
                txtScoreReport.Text = ScoreReport.Run(currentScore);

                // Persist changes to globals
                Globals.ScoreList = _scoreList;
            }
        }
    }
}