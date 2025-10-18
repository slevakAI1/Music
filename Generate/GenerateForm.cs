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
            var structure = new SongStructure();
            var summary = structure.CreateStructure();
            txtSongStructure.Text = summary;
        }

        private void btnAddVoices_Click(object sender, EventArgs e)
        {
            var structure = new SongStructure();
            structure.AddVoices();

            var lines = new System.Collections.Generic.List<string>();
            foreach (var v in structure.Voices)
                lines.Add(v.Value);

            // One voice per line in the voiceset textbox
            txtVoiceSet.Lines = lines.ToArray();
        }

        private void btnAddChords_Click(object sender, EventArgs e)
        {
            var structure = new SongStructure();
            var chords = structure.CreateChordSet();

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