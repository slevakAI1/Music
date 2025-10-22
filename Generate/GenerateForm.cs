using Music.Generate;

namespace Music
{
    public partial class GenerateForm : Form
    {
        public GenerateForm()
        {
            this.Text = "Music";
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Maximize once when shown as an MDI child; preserves design-time size.
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        private void MusicForm_Load(object sender, EventArgs e)
        {
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            Globals.ScoreDesign = new ScoreDesignClass();
            // Clear any previously created harmonic timeline for a fresh design
            Globals.HarmonicTimeline = null;
            RefreshDesignSpaceIfReady();
        }

        // Launch the Section Editor and apply results back to the design
        private void btnCreateSections_Click(object sender, EventArgs e)
        {
            if (!EnsureScoreDesignOrNotify()) return;

            using var dlg = new SectionEditor(Globals.ScoreDesign!.Sections);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // Copy back into the existing Sections instance to preserve references
                var target = Globals.ScoreDesign!.Sections;
                target.Reset();
                foreach (var s in dlg.ResultSections.Sections)
                {
                    target.Add(s.SectionType, s.BarCount, s.Name);
                }

                RefreshDesignSpaceIfReady();
            }
        }

        /*   keep for now
                    if (!EnsureScoreDesignOrNotify()) return;
                    Globals.SectionManager.CreateTestSections(Globals.ScoreDesign!.Sections);
                    RefreshDesignSpaceIfReady();
         */

        // Populate voices via popup selector
        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            if (!EnsureScoreDesignOrNotify()) return;

            using var dlg = new VoiceSelector();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var score = Globals.ScoreDesign!;
                var existing = new HashSet<string>(score.VoiceSet.Voices.Select(v => v.VoiceName),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var name in dlg.SelectedVoices)
                {
                    if (!existing.Contains(name))
                    {
                        score.VoiceSet.AddVoice(name);
                        existing.Add(name);
                    }
                }

                RefreshDesignSpaceIfReady();
            }
        }

/* All code for adding default - keep for now
 * if (!EnsureScoreDesignOrNotify()) return;
            Globals.ScoreDesign!.VoiceSet.AddDefaultVoices();
            RefreshDesignSpaceIfReady();
*/

        // Populate chords without touching the UI textboxes
        private void btnAddChords_Click(object sender, EventArgs e)
        {
            if (!EnsureScoreDesignOrNotify()) return;
            Globals.ScoreDesign!.ChordSet.AddDefaultChords();
            RefreshDesignSpaceIfReady();
        }

        private void btnCreateMusic_Click(object sender, EventArgs e)
        {
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
        }

        private void btnAddHarmonicTimeline_Click(object sender, EventArgs e)
        {
            var timeline = HarmonicDefault.BuildDefaultTimeline();
            Globals.HarmonicTimeline = timeline;
            RefreshDesignSpaceIfReady();
        }

        // --------- Helpers ---------

        private bool EnsureScoreDesignOrNotify()
        {
            if (Globals.ScoreDesign != null) return true;

            MessageBox.Show(this,
                "Create a new score design first.",
                "No Design",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private void RefreshDesignSpaceIfReady()
        {
            if (Globals.ScoreDesign == null) return;
            txtDesignSpace.Text = DesignTextHelper.BuildCombinedText(Globals.ScoreDesign);
        }
    }
}