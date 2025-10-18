using Music.Generate;

namespace Music
{
    public partial class GenerateForm : Form
    {
        // Persisted design and data sets for this form/session
        private ScoreDesignClass? _scoreDesign;
        private SectionsClass? _sections;
        private readonly VoiceSetClass _voiceSet = new();
        private readonly ChordSetClass _chordSet = new();

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

        private void btnNewDesign_Click(object sender, EventArgs e)
        {
            // Create and persist a new ScoreDesign instance for this form/session
            _scoreDesign = new ScoreDesignClass();

            // Display-only fields: clear any prior text tied to older objects
            txtSongStructure.Clear();
            txtVoiceSet.Clear();
            txtChordSet.Clear();
        }

        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            _voiceManager.AddDefaultVoices(this, _sections, _voiceSet, txtVoiceSet);
        }

        private void btnAddChords_Click(object sender, EventArgs e)
        {
            _chordManager.AddDefaultChords(this, _sections, _chordSet, txtChordSet);
        }

        private void btnCreateScoreStructure_Click(object sender, EventArgs e)
        {
            _sections = _sectionManager.CreateSections(this, txtSongStructure, txtVoiceSet, txtChordSet, _voiceSet, _chordSet);
        }
    }
}