using Music.Generate;

namespace Music
{
    public partial class GenerateForm : Form
    {
        // Persisted design for this form/session (now owns Sections, VoiceSet, and ChordSet)
        private ScoreDesignClass? _scoreDesign;

        private readonly VoiceManagerClass _voiceManager = new();
        private readonly ChordManagerClass _chordManager = new();
        private readonly SectionManagerClass _sectionManager = new();

        public GenerateForm()
        {
            this.Text = "Music";
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.Manual;

            InitializeComponent();
        }

        private void MusicForm_Load(object sender, EventArgs e)
        {
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            btnNewDesign_Click(sender, e);
        }

        private void btnNewDesign_Click(object sender, EventArgs e)
        {
            // Create and persist a new ScoreDesign instance for this form/session
            _scoreDesign = new ScoreDesignClass();

            // Display-only fields: clear any prior text tied to older objects
            txtSongStructure.Clear();
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
                _scoreDesign.Sections,   // Sections now persisted on the design
                txtSongStructure,
                txtVoiceSet,
                txtChordSet,
                _scoreDesign.VoiceSet,   // VoiceSet persisted on the design
                _scoreDesign.ChordSet);  // ChordSet persisted on the design
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