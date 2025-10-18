using Music.Generate;

namespace Music
{
    public partial class GenerateForm : Form
    {
        // Persisted song structure for this form/session
        private ScoreDesign? _structure;
        private readonly VoiceManager _voiceManager = new VoiceManager();
        private readonly ChordManager _chordManager = new ChordManager();

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

        private void btnCreateStructure_Click(object sender, EventArgs e)
        {
            _structure = new ScoreDesign();
            var summary = _structure.CreateTestStructure();
            txtSongStructure.Text = summary;

            // Clear prior outputs tied to an older structure
            txtVoiceSet.Clear();
            txtChordSet.Clear();
        }

        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            _voiceManager.AddDefaultVoicesAndRender(this, _structure, txtVoiceSet);
        }

        private void btnAddChords_Click(object sender, EventArgs e)
        {
            _chordManager.AddDefaultChordsAndRender(this, _structure, txtChordSet);
        }

        private void btnCreateMusic_Click(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {

        }
    }
}