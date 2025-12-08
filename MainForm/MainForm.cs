using Music.MyMidi;
using Music.Writer;

namespace Music
{
    public partial class MainForm : Form
    {
        private readonly FileManager _fileManager;
        private readonly MidiIoService _midiIoService;
        private readonly MidiPlaybackService _playbackService;

        public MainForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            this.IsMdiContainer = true;

            _fileManager = new FileManager(ShowStatus);
            _midiIoService = new MidiIoService();
            _playbackService = new MidiPlaybackService();

            // D E F A U L T   F O R M   O N   S T A R T U P

            // Show WriterForm on startup, filling the MDI parent
            ShowChildForm(typeof(Music.Writer.WriterForm));
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        // Show or activate a child form. Let the child manage its own size/state.
        private void ShowChildForm(Type childType)
        {
            var existing = this.MdiChildren.FirstOrDefault(f => f.GetType() == childType);
            if (existing != null)
            {
                if (existing.WindowState != FormWindowState.Maximized)
                    existing.WindowState = FormWindowState.Maximized;
                existing.Activate();
                return;
            }

            Form child = (Form)Activator.CreateInstance(childType)!;
            child.MdiParent = this;
            // Do not force WindowState/FormBorderStyle/Minimize/Maximize here.
            child.Show();
        }

        private void ShowStatus(String message)
        {
            if (statusStrip1 != null && statusStrip1.Items.Count > 0)
                statusStrip1.Items[0].Text = message;
        }

        //                   Tool Strip Methods

        private void MenuImportMusicXml_Click(object sender, EventArgs e)
        {
            _fileManager.ImportMusicXml(this);
        }

        private void MenuExportMusicXml_Click(object sender, EventArgs e)
        {
            _fileManager.ExportMusicXml(this);
        }

        // Your top-level menu item handlers (wired via designer)
        private void designToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowChildForm(typeof(DesignerForm));
        }

        private void generateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Launch the new Writer form.
            ShowChildForm(typeof(Music.Writer.WriterForm));

            // Previous behavior: launch the original Writer form.
            // Commented out per request — do not remove.
            /*
            ShowChildForm(typeof(Music.Generate.WriterForm));
            */
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowChildForm(typeof(SerializerTestForm));
        }

        private void viewMusicXmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Implement MusicXML viewer form
        }

        private void arrangerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowChildForm(typeof(ArrangerForm));
        }

        private void importMidiToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void playMidiFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var song = AppState.CurrentSong;
            if (song == null)
            {
                MessageBox.Show(
                    this,
                    "No MIDI document is loaded. Please import a MIDI file first.",
                    "No MIDI Document",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _playbackService.Play(song);
                ShowStatus($"Playing {song.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Error playing MIDI file:\n{ex.Message}",
                    "Playback Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
