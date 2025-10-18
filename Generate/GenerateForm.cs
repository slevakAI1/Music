using Music.Generate;

namespace Music
{
    public partial class GenerateForm : Form
    {
        // Persisted objects for this form/session
        private SongStructure? _sections;
        private readonly VoiceSet _voiceSet = new();
        private readonly ChordSet _chordSet = new();

        private readonly VoiceManager _voiceManager = new VoiceManager();
        private readonly ChordManager _chordManager = new ChordManager();
        private readonly SectionManagerClass _sectionManager = new SectionManagerClass();

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

        private void btnCreateScoreStructure_Click(object sender, EventArgs e)
        {
            _sections = _sectionManager.CreateSections(this, txtSongStructure, txtVoiceSet, txtChordSet, _voiceSet, _chordSet);
        }

        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            _voiceManager.AddDefaultVoicesAndRender(this, _sections, _voiceSet, txtVoiceSet);
        }

        private void btnAddChords_Click(object sender, EventArgs e)
        {
            _chordManager.AddDefaultChordsAndRender(this, _sections, _chordSet, txtChordSet);
        }

        private void btnCreateMusic_Click(object sender, EventArgs e)
        {
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
        }
    }
}