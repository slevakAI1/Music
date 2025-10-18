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

        // Designer is wired to this
        private void MusicForm_Load(object sender, EventArgs e)
        {
        }

        // Designer may be wired to this
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

            // Reset dependent sets and current sections
            _sections = null;
            _voiceSet.Reset();
            _chordSet.Reset();
        }

        // Designer may be wired to this (alias to CreateSections)
        private void btnCreateSections_Click(object sender, EventArgs e)
        {
            btnCreateScoreStructure_Click(sender, e);
        }

        private void btnCreateScoreStructure_Click(object sender, EventArgs e)
        {
            _sections = _sectionManager.CreateSections(this, txtSongStructure, txtVoiceSet, txtChordSet, _voiceSet, _chordSet);
        }

        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            _voiceManager.AddDefaultVoices(this, _sections, _voiceSet, txtVoiceSet);
        }

        private void btnAddChords_Click(object sender, EventArgs e)
        {
            _chordManager.AddDefaultChords(this, _sections, _chordSet, txtChordSet);
        }

        // Designer may be wired to this; keep as stub for now
        private void btnCreateMusic_Click(object sender, EventArgs e)
        {
            // Intentionally left empty until music generation is implemented.
            // This method exists to satisfy designer wiring.
        }

        // Designer is wired to this; keep as stub or implement save logic later
        private void btnSave_Click(object sender, EventArgs e)
        {
            // Intentionally left empty until save behavior is defined.
        }
    }
}