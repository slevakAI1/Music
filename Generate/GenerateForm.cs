using Music.Generate;

namespace Music
{
    public partial class GenerateForm : Form
    {
        // Persisted song structure for this form/session
        private ScoreDesign? _scoreDesign;
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

            // Create designer-based controls
            InitializeComponent();
        }

        private void MusicForm_Load(object sender, EventArgs e)
        {

        }

        private void btnCreateScoreStructure_Click(object sender, EventArgs e)
        {
            _scoreDesign = _sectionManager.CreateAndRenderStructure(this, txtSongStructure, txtVoiceSet, txtChordSet);
        }

        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            _voiceManager.AddDefaultVoicesAndRender(this, _scoreDesign, txtVoiceSet);
        }

        private void btnAddChords_Click(object sender, EventArgs e)
        {
            _chordManager.AddDefaultChordsAndRender(this, _scoreDesign, txtChordSet);
        }

        private void btnCreateMusic_Click(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {

        }
    }
}