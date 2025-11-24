using Music.Designer;
using MusicXml;
using MusicXml.Domain;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Music.Tests;

namespace Music.Writer
{
    public partial class ConsoleForm : Form
    {
        private List<Score> _scoreList;
        private Designer.Designer? _designer;
        private ConsoleData? _writer;
        private MeasureMeta _measureMeta;

        // playback service (reused for multiple play calls)
        private IMidiPlaybackService _midiPlaybackService;

        private int pitchEventNumber = 0;

        //===========================   I N I T I A L I Z A T I O N   ===========================
        public ConsoleForm()
        {
            InitializeComponent();

            // Initialize MeasureMeta tracking object
            _measureMeta = new MeasureMeta();

            // create playback service
            _midiPlaybackService = new MidiPlaybackService();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            // Load current global score list
            _scoreList = Globals.ScoreList;
            if (_scoreList.Count > 0)
                txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);

            _designer = Globals.Designer;
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);


            // Initialize comboboxes - doesn't seem to be a way to set a default in the designer or form.
            // The changes keep getting discarded. wtf?
            cbChordBase.SelectedIndex = 0; // C
            cbChordQuality.SelectedIndex = 0; // Major
            cbChordKey.SelectedIndex = 0; // C

            // Initialize staff selection - default to staff 1 checked
            if (clbStaffs != null && clbStaffs.Items.Count > 0)
                clbStaffs.SetItemChecked(0, true); // Check staff "1"

            cbPattern.SelectedIndex = 0;

            // Configure dgvPhrases: disable placeholder row so programmatic adds are visible immediately
            dgvPhrase.AllowUserToAddRows = false;

            // ====================   T H I S   H A S   T O   B E   L A S T  !   =================

            // Capture form control values manually set in the form designer
            // This will only be done once, at form construction time.
            _writer ??= CaptureFormData();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        // The Writer form is activated each time it gains focus.
        // The initialization of controls is controlled entirely by the current Design and persisted Writer.
        // It does not depend on the prior state of the controls.
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Get from globals on the way in but not if null, would overwrite current state

            if (Globals.ScoreList != null && Globals.ScoreList.Count > 0)
            {
                _scoreList = Globals.ScoreList;
                // Update score report display when score is refreshed from globals
                txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);
            }
            if (Globals.Designer != null)
            {
                _designer = Globals.Designer;
                txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);
            }
            if (Globals.Writer != null)
                _writer = Globals.Writer;

            ApplyFormData(_writer);
        }

        // Persist current control state whenever the form loses activation (user switches to another MDI child)
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);

            // Save on the way out
            Globals.ScoreList = _scoreList;
            Globals.Designer = _designer;
            _writer = Globals.Writer = CaptureFormData();
            Globals.Writer = _writer;
        }

        //===============================   E V E N T S   ==============================


        // Inserts notes based on the "number of notes" parameter from the writer form
        private void btnAppendNotes_Click(object sender, EventArgs e)
        {
            _writer = CaptureFormData();

            // Ensure score list exists and has at least one score
            if (_scoreList == null || _scoreList.Count == 0)
            {
                MessageBox.Show(this, "No score available. Please create a new score first.",
                    "No Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // THIS SHOULD NOW BE "CREATE PITCH EVENT" and and SHOW IN THE NEW CONTROL 

            var config = _writer.ToAppendPitchEventsParams();

            // THIS SHOULD APPEND FROM WHATS SELECTED IN THE PHRASE  CONTROL

            AppendNotes.Execute(_scoreList[0], config, _measureMeta);

            txtScoreReport.Text = ScoreReport.Run(_scoreList[0]);
            Globals.ScoreList = _scoreList;  // Note: Do this here for now because File Export MusicXml does not exit this form, so does not trigger Deactivate().
        }

        // THIS SHOULD CREATE PITCH EVENT and add to THE pitchevent datagridview CONTROL 
        private void ExecuteCommandRepeatNoteChordRest()
        {
            dgvPhrase.DefaultCellStyle.ForeColor = Color.Black;
            dgvPhrase.DefaultCellStyle.BackColor = Color.White;

            // Get params for this command
            _writer = CaptureFormData();
            var phrase = _writer.ToAppendPitchEventsParams();

            // Set phrase name/number
            pitchEventNumber++;
            var pitchEventName = pitchEventNumber.ToString();

            // Add new row with explicit cell values
            int newRowIndex = dgvPhrase.Rows.Add(phrase, pitchEventName, "tbd");
        }

        /// <summary>
        /// Converts the provided AppendPitchEventsParams to MIDI and plays it back.
        /// Stops and releases the MIDI device after playback completes.
        /// </summary>
        private async Task PlayMidiFromPitchEventsAsync(AppendPitchEventsParams config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            // Always stop any existing playback first
            _midiPlaybackService.Stop();

            // Convert to MIDI
            var midiDoc = PitchEventsToMidiConverter.Convert(config);

            // Select first available output device
            var devices = _midiPlaybackService.EnumerateOutputDevices().ToList();
            if (devices.Count == 0)
            {
                MessageBox.Show(this, "No MIDI output device found.", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _midiPlaybackService.SelectOutput(devices[0]);
            _midiPlaybackService.Play(midiDoc);

            // Wait for playback duration plus buffer
            var duration = midiDoc?.Duration ?? TimeSpan.Zero;
            var totalDelay = duration.TotalMilliseconds + 250;
            
            if (totalDelay > 0)
                await Task.Delay((int)Math.Min(totalDelay, int.MaxValue));

            // Always stop to release resources
            _midiPlaybackService.Stop();
        }


        private void btnUpdateFormFromDesigner_Click(object sender, EventArgs e)
        {
            // Update the form to take into account any changes to Designer
            Globals.Writer?.UpdateFromDesigner(_designer);
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);

            // Technical this can run upon activation too, but only in initialize phase, just that one time
        }

        //===========================================================================================
        //                      T E S T   S C E N A R I O   B U T T O N S
        //  

        // This sets design test scenario D1
        private void btnSetDesignTestScenarioD1_Click(object sender, EventArgs e)
        {
            _designer ??= new Designer.Designer();
            DesignerTests.SetTestDesignD1(_designer);
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);
            // KEEP. MessageBox.Show("Test Design D1 has been applied to the current design.", "Design Test Scenario D1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // This sets writer test scenario G1
        // Description: Set writer test values using the current design 
        private void btnSetWriterTestScenarioG1_Click(object sender, EventArgs e)
        {
            _writer = ConsoleTests.SetTestWriterG1(_designer);
            ApplyFormData(_writer);
            // KEEP. MessageBox.Show("Test Writer G1 has been applied to the current generator settings.", "Generator Test Scenario G1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnChordTest_Click(object sender, EventArgs e)
        {
            if (_designer?.HarmonicTimeline == null || _designer.HarmonicTimeline.Events.Count == 0)
            {
                MessageBox.Show(this,
                    "No harmonic events available in the current design.",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var harmonicEvent = _designer.HarmonicTimeline.Events[1];

            List<PitchEvent> notes;
            try
            {
                notes = ChordConverter.Convert(
                    harmonicEvent.Key,
                    harmonicEvent.Degree,
                    harmonicEvent.Quality,
                    harmonicEvent.Bass,
                    baseOctave: 4);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Failed to build chord: {ex.Message}",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (notes == null || notes.Count == 0)
            {
                MessageBox.Show(this,
                    "Chord conversion returned no notes.",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var lines = new List<string>();
            foreach (var note in notes)
            {
                var accidental = note.Alter switch
                {
                    1 => "#",
                    -1 => "b",
                    _ => ""
                };
                lines.Add($"{note.Step}{accidental} {note.Octave}");
            }

            var title = $"Chord: {harmonicEvent.Key} (Deg {harmonicEvent.Degree}, {harmonicEvent.Quality})";
            MessageBox.Show(this,
                string.Join(Environment.NewLine, lines),
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnExportToNotion_Click(object sender, EventArgs e)
        {
            // Ensure score list exists and has at least one score
            if (_scoreList == null || _scoreList.Count == 0)
            {
                MessageBox.Show(this, "No score to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var path = Path.Combine("..", "..", "..", "Files", "NotionExchange", "Score.musicxml");
                var fullPath = Path.GetFullPath(path);
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var xml = MusicXmlScoreSerializer.Serialize(_scoreList[0]);
                File.WriteAllText(fullPath, xml, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                MessageBox.Show(this, $"Exported to {fullPath}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error exporting MusicXML:\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            var pattern = cbPattern?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(pattern))
                return;

            switch (pattern)
            {
                case "Repeat Note/Chord/Rest":
                    ExecuteCommandRepeatNoteChordRest();
                    break;

                // Add additional cases for other patterns as needed
                default:
                    // No-op for unrecognized patterns
                    break;
            }
        }

        private async void btnPlay_Click(object sender, EventArgs e)
        {
            // Check if there are any rows in the grid
            if (dgvPhrase.Rows.Count == 0)
            {
                MessageBox.Show(this, "No pitch events to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if a row is selected
            if (dgvPhrase.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Please select a pitch event to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Get the AppendPitchEventsParams from the selected row, first column (index 0)
            var selectedRow = dgvPhrase.SelectedRows[0];
            var cellValue = selectedRow.Cells[0].Value;

            if (cellValue is not AppendPitchEventsParams config)
            {
                MessageBox.Show(this, "Invalid data in the selected row.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                await PlayMidiFromPitchEventsAsync(config);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error playing MIDI: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}