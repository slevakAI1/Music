using Music.Services;
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Music.Design;

namespace Music
{
    public partial class MusicForm : Form
    {
        //private Label _label;
        //private Button _playButton;
        private readonly IMidiPlaybackService _playbackService = new MidiPlaybackService();

        public MusicForm()
        {
            this.Text = "Music";
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.Manual;

            // Create designer-based controls
            InitializeComponent();

            this.Activated += (s, e) => RefreshFromState();
        }

        public void RefreshFromState()
        {
            var song = AppState.CurrentSong;
            if (song == null)
            {
                lblMidiFilepath.Text = "Music";
                btnPlayMidi.Enabled = false;
            }
            else
            {
                var duration = song.Duration;
                lblMidiFilepath.Text = $"{song.FileName} | Tracks: {song.TrackCount} | " +
                    $"Duration: {duration.Minutes:D2}:{duration.Seconds:D2} | Events: {song.EventCount}";
                btnPlayMidi.Enabled = true;
            }
        }

        private void PlayButton_Click(object? sender, EventArgs e)
        {
        }

        private void scoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string midiFilePath = Path.Combine(Path.GetTempPath(), "MusicApp_Score.mid");
            string scoreExePath = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                "Score.exe"
            );

            var song = AppState.CurrentSong;
            if (song == null)
            {
                MessageBox.Show(
                    this,
                    "No MIDI document is loaded. Please import a MIDI file first.",
                    "No MIDI Document",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                // Export current MIDI document to the standard file
                try
                {
                    song.Raw.Write(midiFilePath, true); // true = overwrite
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this,
                        $"Error attempting to export MIDI file.\n\nDetails:\n{ex.GetType().FullName}: {ex.Message}\nStack Trace:\n{ex.StackTrace}",
                        "Export MIDI Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                if (!File.Exists(scoreExePath))
                {
                    MessageBox.Show(
                        this,
                        $"Could not find Score.exe at:\n{scoreExePath}",
                        "Launch Score Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = scoreExePath,
                    Arguments = $"\"{midiFilePath}\"",
                    WorkingDirectory = Path.GetDirectoryName(scoreExePath) ?? "",
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Error launching Score.exe.\n\nDetails:\n{ex.GetType().FullName}: {ex.Message}\nStack Trace:\n{ex.StackTrace}",
                    "Launch Score Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void btnCreateStructure_Click(object sender, EventArgs e)
        {
            var structure = new SongStructure();
            var summary = structure.CreateStructure();
            txtSongStructure.Text = summary;

        }

        private void MusicForm_Load(object sender, EventArgs e)
        {

        }

        private void btnPlayMidi_Click(object sender, EventArgs e)
        {
            var song = AppState.CurrentSong;
            if (song != null)
            {
                _playbackService.Play(song);
            }

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

        private void txtTestChords_Click(object sender, EventArgs e)
        {

        }

        private void btnTestParser_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MusicXML Files (*.musicxml;*.xml)|*.musicxml;*.xml",
                Title = "Select a MusicXML file"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            var result = new Music.Services.MusicXmlTests().TestParser(ofd.FileName);
            var passed = string.Equals(result, "Passed", StringComparison.OrdinalIgnoreCase);

            MessageBox.Show(this, passed ? "Passed" : result, "MusicXML Parser Test",
                MessageBoxButtons.OK, passed ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
    }
}