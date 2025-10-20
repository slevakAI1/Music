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
            // Create and persist a new ScoreDesign instance for this app/session
            Globals.ScoreDesign = new ScoreDesignClass();

            // Render combined design space and clear legacy text areas
            txtDesignSpace.Text = DesignTextHelper.BuildCombinedText(Globals.ScoreDesign);
        }

        private void btnCreateSections_Click(object sender, EventArgs e)
        {
            if (Globals.ScoreDesign == null)
            {
                MessageBox.Show(this, "Create a new score design first.", "No Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build sections without touching the UI textboxes
            Globals.SectionManager.CreateTestSections(Globals.ScoreDesign.Sections);

            // Render combined design space and clear legacy text areas
            txtDesignSpace.Text = DesignTextHelper.BuildCombinedText(Globals.ScoreDesign);
        }

        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            if (Globals.ScoreDesign == null)
            {
                MessageBox.Show(this, "Create a new score design first.", "No Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Populate voices without touching the UI textboxes
            Globals.ScoreDesign.VoiceSet.AddDefaultVoices();

            // Render combined design space and clear legacy text areas
            txtDesignSpace.Text = DesignTextHelper.BuildCombinedText(Globals.ScoreDesign);
        }

        private void btnAddChords_Click(object sender, EventArgs e)
        {
            if (Globals.ScoreDesign == null)
            {
                MessageBox.Show(this, "Create a new score design first.", "No Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Populate chords without touching the UI textboxes
            Globals.ScoreDesign.ChordSet.AddDefaultChords();

            // Render combined design space and clear legacy text areas
            txtDesignSpace.Text = DesignTextHelper.BuildCombinedText(Globals.ScoreDesign);
        }

        private void btnCreateMusic_Click(object sender, EventArgs e)
        {
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
        }
    }
}