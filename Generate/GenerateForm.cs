using Music.Generate;

namespace Music
{
    public partial class GenerateForm : Form
    {
        // Persisted objects for this form/session
        private SectionsClass _sections = new();
        private readonly VoiceSetClass _voiceSet = new();
        private readonly ChordSetClass _chordSet = new();

        private readonly VoiceManagerClass _voiceManager = new VoiceManagerClass();
        private readonly ChordManagerClass _chordManager = new ChordManagerClass();
        private readonly SectionManagerClass _sectionManager = new SectionManagerClass();

        public GenerateForm()
        {
            this.Text = "Generate Score";
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

        private void btnCreateSections_Click(object sender, EventArgs e)
        {
            _sections = _sectionManager.CreateSections(this, txtSongStructure, txtVoiceSet, txtChordSet, _voiceSet, _chordSet);
        }

        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            _voiceManager.AddDefaultVoicesAndRender(this, _sections, _voiceSet, txtVoiceSet);
        }

        private void btnAddChords_Click(object sender, EventArgs e)
        {
            _chordManager.AddDefaultChords(this, _sections, _chordSet, txtChordSet);
        }

        private void btnCreateMusic_Click(object sender, EventArgs e)
        {
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {

        }
    }
}