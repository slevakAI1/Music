using Music.Generate;

namespace Music
{
    public partial class GenerateForm : Form
    {
        // Persisted design for this form/session (now owns Sections, VoiceSet, and ChordSet)
        private ScoreDesignClass? _scoreDesign;

        private readonly VoiceManagerClass _voiceManager = new();
        private readonly ChordManagerClass _chordManager = new();
        private readonly SectionSetManagerClass _sectionManager = new();

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
            // Create and persist a new ScoreDesign instance for this form/session
            _scoreDesign = new ScoreDesignClass();

            // Display-only fields: clear any prior text tied to older objects
            txtSections.Clear();
            txtVoiceSet.Clear();
            txtChordSet.Clear();
        }

        private void btnCreateSections_Click(object sender, EventArgs e)
        {
            if (_scoreDesign == null)
            {
                MessageBox.Show(this, "Create a new score design first.", "No Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _sectionManager.CreateSections(
                this,
                _scoreDesign.Sections,   
                _scoreDesign.VoiceSet,   
                _scoreDesign.ChordSet,
                txtSections,
                txtVoiceSet,
                txtChordSet);
        }

        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            if (_scoreDesign == null)
            {
                MessageBox.Show(this, "Create a new score design first.", "No Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _voiceManager.AddDefaultVoices(
                this,
                _scoreDesign.Sections,   // pass persisted sections
                _scoreDesign.VoiceSet,   // pass persisted voice set
                txtVoiceSet);
        }

        private void btnAddChords_Click(object sender, EventArgs e)
        {
            if (_scoreDesign == null)
            {
                MessageBox.Show(this, "Create a new score design first.", "No Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _chordManager.AddDefaultChords(
                this,
                _scoreDesign.Sections,   // pass persisted sections
                _scoreDesign.ChordSet,   // pass persisted chord set
                txtChordSet);
        }

        private void btnCreateMusic_Click(object sender, EventArgs e)
        {
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
        }
    }
}