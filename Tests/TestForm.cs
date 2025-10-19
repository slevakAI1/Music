using Music.Generate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Music
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();

            // Design-time size can be set in the designer; at runtime, we maximize on show.
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Maximize once when shown as an MDI child; avoids double-application.
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        private void btnTestParser_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MusicXML Files (*.musicxml;*.xml)|*.musicxml;*.xml",
                Title = "Select a MusicXML file"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            var result = new Music.Tests.MusicXmlParserTests().TestParser(ofd.FileName);
            var passed = string.Equals(result, "Passed", StringComparison.OrdinalIgnoreCase);

            MessageBox.Show(this, passed ? "Passed" : result, "MusicXML Parser Test",
                MessageBoxButtons.OK, passed ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }


        // Import raw MusicXML file, serialize it back out, re-import, and compare the two object graphs
        private void btnTestSerializer_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "MusicXML Files (*.musicxml;*.xml)|*.musicxml;*.xml",
                Title = "Select a MusicXML file"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            var result = new Music.Tests.MusicXmlSerializerTests().TestSerializer(ofd.FileName);
            var passed = string.Equals(result, "Passed", StringComparison.OrdinalIgnoreCase);

            MessageBox.Show(this, passed ? "Passed" : result, "MusicXML Serializer Test",
                MessageBoxButtons.OK, passed ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void btnCreateTestMusicXmlFile_Click(object sender, EventArgs e)
        {
            try
            {
                var score = Music.Tests.MusicXmlCreateValidFileTests.CreateSingleMeasureCChordKeyboardScore();
                var xml = MusicXml.MusicXmlScoreSerializer.Serialize(score);

                var targetDir = @"C:\temp";
                Directory.CreateDirectory(targetDir);

                var fileName = $"TestScore_{DateTime.Now:yyyyMMdd_HHmmss}.musicxml";
                var fullPath = Path.Combine(targetDir, fileName);

                File.WriteAllText(fullPath, xml, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                MessageBox.Show(this,
                    $"Saved MusicXML to:\n{fullPath}",
                    "MusicXML Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Failed to create/save test MusicXML file.\n\n{ex.GetType().FullName}: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void TestForm_Load(object sender, EventArgs e)
        {

        }
    }
}

/*

     public void RefreshFromState()
        {
            var song = AppState.CurrentSong;
            if (song == null)
            {
                lblFilepath.Text = "Music";
                btnSave.Enabled = false;
            }
            else
            {
                var duration = song.Duration;
                lblFilepath.Text = $"{song.FileName} | Tracks: {song.TrackCount} | " +
                    $"Duration: {duration.Minutes:D2}:{duration.Seconds:D2} | Events: {song.EventCount}";
                btnSave.Enabled = true;
            }
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
 
 
 * 
 *         private void btnPlayMidi_Click(object sender, EventArgs e)
        {
            var song = AppState.CurrentSong;
            if (song != null)
            {
                _playbackService.Play(song);
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
*/