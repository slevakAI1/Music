using Music.Generate;

namespace Music
{
    public partial class GenerateForm : Form
    {
        // Persisted song structure for this form/session
        private SongStructure? _structure;

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
            _structure = new SongStructure();
            var summary = _structure.CreateStructure();
            txtSongStructure.Text = summary;

            // Clear prior outputs tied to an older structure
            txtVoiceSet.Clear();
            txtChordSet.Clear();
        }

        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            if (_structure == null)
            {
                MessageBox.Show(this, "Create the song structure first.", "No Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _structure.AddVoices();

            var lines = new System.Collections.Generic.List<string>();
            foreach (var v in _structure.Voices)
                lines.Add(v.Value);

            // One voice per line in the voiceset textbox
            txtVoiceSet.Lines = lines.ToArray();
        }

        private void btnAddChords_Click(object sender, EventArgs e)
        {
            if (_structure == null)
            {
                MessageBox.Show(this, "Create the song structure first.", "No Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var chords = _structure.CreateChordSet();

            var lines = new System.Collections.Generic.List<string>(chords.Count);
            foreach (var c in chords)
                lines.Add(c.Name);

            // One chord per line in the chord set textbox
            txtChordSet.Lines = lines.ToArray();
        }

        private void btnCreateMusic_Click(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {

        }
    }
}