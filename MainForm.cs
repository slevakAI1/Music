using Music.Tests;
using MusicXml;
using MusicXml.Domain;

namespace Music
{
    public partial class MainForm : Form
    {
        private readonly MidiIoService _midiIoService = new MidiIoService();
        private readonly IMusicXmlService _musicXmlService = new MusicXmlService();

        // Persist the last imported MusicXML score at the Form level
        private Score? _currentScore;

        public MainForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            this.IsMdiContainer = true;

            // Show MusicForm on startup, filling the MDI parent
            ShowChildForm(typeof(GenerateForm));
        }

        // New: Show or activate a child form, and make it fill the MDI parent
        private void ShowChildForm(Type childType)
        {
            var existing = this.MdiChildren.FirstOrDefault(f => f.GetType() == childType);
            if (existing != null)
            {
                existing.WindowState = FormWindowState.Maximized;
                existing.Activate();
                return;
            }

            Form child = (Form)Activator.CreateInstance(childType);
            child.MdiParent = this;
            child.WindowState = FormWindowState.Maximized;
            child.FormBorderStyle = FormBorderStyle.Sizable;
            child.MaximizeBox = false;
            child.MinimizeBox = false;
            child.Show();
        }

        private void ShowStatus(String message)
        {
            if (statusStrip1 != null && statusStrip1.Items.Count > 0)
                statusStrip1.Items[0].Text = message;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }


        //                             Menu Item Methods


        private void MenuImportMusicXml_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MusicXML files (*.musicxml;*.xml)|*.musicxml;*.xml",
                Title = "Import MusicXML File"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Parse and persist the Score at the Form level
                    _currentScore = _musicXmlService.ImportFromMusicXml(ofd.FileName);

                    var movement = _currentScore?.MovementTitle ?? "Unknown";
                    ShowStatus($"Loaded MusicXML: {Path.GetFileName(ofd.FileName)} (Movement: {movement})");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error importing MusicXML file:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                        "Import Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void MenuExportMusicXml_Click(object sender, EventArgs e)
        {
            if (_currentScore == null)
            {
                MessageBox.Show("No MusicXML score loaded.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "MusicXML files (*.musicxml;*.xml)|*.musicxml;*.xml",
                Title = "Export MusicXML File",
                FileName = "score.musicxml"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Export the last imported MusicXML score to the selected path
                    _musicXmlService.ExportLastImportedScore(sfd.FileName);
                    ShowStatus($"Exported to {Path.GetFileName(sfd.FileName)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error exporting MusicXML file:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                        "Export Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }


        private void MenuImportMidi_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MIDI files (*.mid;*.midi)|*.mid;*.midi",
                Title = "Import MIDI File"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var doc = _midiIoService.ImportFromFile(ofd.FileName);
                    AppState.CurrentSong = doc;
                    ShowStatus($"Loaded {doc.FileName}");

                    // Refresh MusicForm if open
                    var musicForm = this.MdiChildren.OfType<GenerateForm>().FirstOrDefault();
                    musicForm?.RefreshFromState();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error importing MIDI file:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                        "Import Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void MenuFormMusic_Click(object sender, EventArgs e)
        {
            ShowChildForm(typeof(GenerateForm));
        }

    }
}
