using Music.Designer;
using MusicXml.Domain;

namespace Music.Generator
{
    public partial class GeneratorForm : Form
    {
        private Score? _score;
        private Designer.Designer? _design;
        private Generator? _generatorData;

        //===========================   I N I T I A L I Z A T I O N   ===========================
        public GeneratorForm()
        {
            InitializeComponent();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            // Load current global score and design into form-local fields for later use
            // Constructor is the only place that reads Globals per requirement.
            _score = Globals.Score;
            _design = Globals.Designer;

            // Initialize comboboxes - doesn't seem to be a way to set a default in the designer or form.
            // The changes keep getting discarded. wtf?
            cbChordBase.SelectedIndex = 0; // C
            cbChordQuality.SelectedIndex = 0; // Major
            cbChordKey.SelectedIndex = 0; // C

            // Initialize staff selection - default to staff 1 checked
            if (clbStaffs != null && clbStaffs.Items.Count > 0)
                clbStaffs.SetItemChecked(0, true); // Check staff "1"

            // ====================   T H I S   H A S   T O   B E   L A S T  !   =================

            // Capture form control values manually set in the form designer
            // This will only be done once, at form construction time.
            _generatorData ??= CaptureFormData();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        // The Generator form is activated each time it gains focus.
        // The initialization of controls is controlled entirely by the current Design and persisted GenerationData.
        // It does not depend on the prior state of the controls.
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Get from globals on the way in but not if null, would overwrite current state

            if (Globals.Score != null)
                _score = Globals.Score;
            if (Globals.Designer != null)
                _design = Globals.Designer;
            if (Globals.Generator != null)
                _generatorData = Globals.Generator;

            ApplyFormData(_generatorData);
        }

        // Persist current control state whenever the form loses activation (user switches to another MDI child)
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);

            // Save on the way out
            Globals.Score = _score;
            Globals.Designer = _design;
            _generatorData = Globals.Generator = CaptureFormData();
            Globals.Generator = _generatorData;
        }

        //===============================   E V E N T S   ==============================

        private void btnApplySetNotes_Click(object sender, EventArgs e)
        {
            // Persist current control state and pass the captured DTO to PatternSetNotes.
            // All control-to-primitive mapping/logic is handled inside PatternSetNotes.Apply(Score, GenerationData).
            _generatorData = CaptureFormData();
            if (_generatorData != null)
            {
                var config = _generatorData.ToPatternConfiguration();
                SetNotes.Apply(_score!, config);
                Globals.Score = _score;  // Note: Do this here for now because File Export MusicXml does not exit this form, so does not trigger Deactivate().
            }
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            _score = ScoreHelper.NewScore(this, _design, clbParts, lblEndBarTotal);
        }


        //===========================================================================================
        //                      T E S T   S C E N A R I O   B U T T O N S
        //  

        // This sets design test scenario D1
        private void btnSetDesignTestScenarioD1_Click(object sender, EventArgs e)
        {
            // Ensure design exists and apply design defaults
            _design ??= new Designer.Designer();
            DesignerTests.SetTestDesignD1(_design);
            MessageBox.Show("Test Design D1 has been applied to the current design.", "Design Test Scenario D1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // This sets generator test scenario G1
        // Description: Set generator test values using the current design 
        private void btnSetGeneratorTestScenarioG1_Click(object sender, EventArgs e)
        {
            // Merge in any design changes
            //_generatorData?.UpdateFromDesigner(_design);
            _generatorData = GeneratorTests.SetTestGeneratorG1(_design);
            ApplyFormData(_generatorData);
            MessageBox.Show("Test Generator G1 has been applied to the current generator settings.", "Generator Test Scenario G1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnChordTest_Click(object sender, EventArgs e)
        {
            if (_design?.HarmonicTimeline == null || _design.HarmonicTimeline.Events.Count == 0)
            {
                MessageBox.Show(this,
                    "No harmonic events available in the current design.",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var harmonicEvent = _design.HarmonicTimeline.Events[1];

            List<ChordConverter.ChordNote> notes;
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
                lines.Add($"{note.Step}{note.Accidental} {note.Octave}");   //  THIS IS PERFECT OUTPUT I NEED!

            var title = $"Chord: {harmonicEvent.Key} (Deg {harmonicEvent.Degree}, {harmonicEvent.Quality})";
            MessageBox.Show(this,
                string.Join(Environment.NewLine, lines),
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnUpdateFormFromDesigner_Click(object sender, EventArgs e)
        {
            // Update the form to take into account any changes to Designer
            Globals.Generator?.UpdateFromDesigner(_design);

            // Technical this can run upon activation too, but only in initialize phase, just that one time
        }
    }
}