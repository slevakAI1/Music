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
            RefreshDesignSpaceIfReady();
        }

        // Build sections without touching the UI textboxes
        private void btnCreateSections_Click(object sender, EventArgs e)
        {
            if (!EnsureScoreDesignOrNotify()) return;
            Globals.SectionManager.CreateTestSections(Globals.ScoreDesign!.Sections);
            RefreshDesignSpaceIfReady();
        }

        // Populate voices without touching the UI textboxes
        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            if (!EnsureScoreDesignOrNotify()) return;
            Globals.ScoreDesign!.VoiceSet.AddDefaultVoices();
            RefreshDesignSpaceIfReady();
        }

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