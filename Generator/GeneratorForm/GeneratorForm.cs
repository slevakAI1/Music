using Music.Design;
using Music.Generator;
using MusicXml.Domain;
using static Music.Helpers;

namespace Music.Generate
{
    public partial class GeneratorForm : Form
    {
        private Score? _score;
        private DesignerData? _design;

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
            _design = Globals.Design;

            // Capture form control values manually set in the form designer
            // This will only be done once, at form construction time.
            Globals.GenerationData ??= CaptureFormData();
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

            // Merge in any design changes that may have happened while outside this form
            GeneratorFormHelper.UpdateGeneratorDataFromDesignData(Globals.GenerationData, _design);

            // Update the form to take into account any design changes
            ApplyFormData(Globals.GenerationData);
        }

        // Persist current control state whenever the form loses activation (user switches to another MDI child)
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            GeneratorFormHelper.UpdateGeneratorDataFromDesignData(Globals.GenerationData, _design);
            ApplyFormData(Globals.GenerationData);
        }

        //===============================   E V E N T S   ==============================

        private void btnApplySetNotes_Click(object sender, EventArgs e)
        {
            // Persist current control state and pass the captured DTO to PatternSetNotes.
            // All control-to-primitive mapping/logic is handled inside PatternSetNotes.Apply(Score, GenerationData).
            Globals.GenerationData = CaptureFormData();
            if (Globals.GenerationData != null)
            {
                PatternSetNotes.Apply(_score!, Globals.GenerationData);
                Globals.Score = _score;
            }
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            _score = GeneratorFormHelper.NewScore(this, _design, clbParts, lblEndBarTotal);
        }

        //========================   F O R M   D A T A   M A N A G E M E N T   ========================
        // TODO Move this to owner/helper class

        // Capture current control values into a GenerateFormData DTO.
        public GeneratorData CaptureFormData()
        {
            // Capture parts items and their checked state into a dictionary
            var partsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < clbParts.Items.Count; i++)
            {
                var name = clbParts.Items[i]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(name))
                    partsState[name] = clbParts.GetItemChecked(i);
            }

            var data = new GeneratorData
            {
                // Pattern
                Pattern = cbPattern.SelectedItem?.ToString(),

                // New: store the full items -> checked state map
                PartsState = partsState,

                AllPartsChecked = chkAllParts.Checked,
                AllStaffChecked = checkBox1.Checked,

                // Staff / sections / bars / beats
                Staff = (int)numStaff.Value,
                SectionsText = txtSections.Text,
                StartBar = (int)numStartBar.Value,
                EndBar = (int)numEndBar.Value,
                StartBeat = (int)numStartBeat.Value,
                EndBeat = (int)numericUpDown2.Value,

                OverwriteExisting = chkOverwrite.Checked,

                // Pitch
                PitchAbsolute = rbPitchAbsolute.Checked,
                Step = cbStep.SelectedItem?.ToString(),
                Accidental = cbAccidental.SelectedItem?.ToString(),
                OctaveAbsolute = (int)numOctaveAbs.Value,
                DegreeKeyRelative = (int)numDegree.Value,
                OctaveKeyRelative = (int)numOctaveKR.Value,

                // Rhythm
                NoteValue = cbNoteValue.SelectedItem?.ToString(),
                Dots = (int)numDots.Value,
                TupletEnabled = chkTupletEnabled.Checked,
                TupletCount = (int)numTupletCount.Value,
                TupletOf = (int)numTupletOf.Value,
                TieAcross = chkTieAcross.Checked,
                Fermata = chkFermata.Checked,
                NumberOfNotes = (int)numNumberOfNotes.Value
            };

            return data;
        }

        // Apply a GenerateFormData DTO back to the private form controls.
        public void ApplyFormData(GeneratorData data)
        {
            if (data == null) return;

            // Pattern
            if (data.Pattern != null && cbPattern.Items.Contains(data.Pattern))
                cbPattern.SelectedItem = data.Pattern;

            // Parts - if provided, set checked state for matching items
            if (data.PartsState != null && data.PartsState.Count > 0)
            {
                // Use case-insensitive lookup
                var map = new Dictionary<string, bool>(data.PartsState, StringComparer.OrdinalIgnoreCase);

                // Clear existing items and populate from the map so control contains exactly the map entries
                clbParts.BeginUpdate();
                try
                {
                    clbParts.Items.Clear();
                    foreach (var kv in map)
                    {
                        var name = kv.Key ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        var idx = clbParts.Items.Add(name);
                        clbParts.SetItemChecked(idx, kv.Value);
                    }
                }
                finally
                {
                    clbParts.EndUpdate();
                }
            }

            if (data.AllPartsChecked.HasValue)
                chkAllParts.Checked = data.AllPartsChecked.Value;

            if (data.AllStaffChecked.HasValue)
                checkBox1.Checked = data.AllStaffChecked.Value;

            // Staff / sections / bars / beats
            if (data.Staff.HasValue)
                numStaff.Value = LimitRange(numStaff, data.Staff.Value);

            if (data.SectionsText != null)
                txtSections.Text = data.SectionsText;

            if (data.StartBar.HasValue)
                numStartBar.Value = LimitRange(numStartBar, data.StartBar.Value);

            if (data.EndBar.HasValue)
                numEndBar.Value = LimitRange(numEndBar, data.EndBar.Value);

            if (data.StartBeat.HasValue)
                numStartBeat.Value = LimitRange(numStartBeat, data.StartBeat.Value);

            if (data.EndBeat.HasValue)
                numericUpDown2.Value = LimitRange(numericUpDown2, data.EndBeat.Value);

            if (data.OverwriteExisting.HasValue)
                chkOverwrite.Checked = data.OverwriteExisting.Value;

            // Pitch
            if (data.PitchAbsolute.HasValue)
            {
                rbPitchAbsolute.Checked = data.PitchAbsolute.Value;
                rbPitchKeyRelative.Checked = !data.PitchAbsolute.Value;
            }

            if (data.Step != null && cbStep.Items.Contains(data.Step))
                cbStep.SelectedItem = data.Step;

            if (data.Accidental != null && cbAccidental.Items.Contains(data.Accidental))
                cbAccidental.SelectedItem = data.Accidental;

            if (data.OctaveAbsolute.HasValue)
                numOctaveAbs.Value = LimitRange(numOctaveAbs, data.OctaveAbsolute.Value);

            if (data.DegreeKeyRelative.HasValue)
                numDegree.Value = LimitRange(numDegree, data.DegreeKeyRelative.Value);

            if (data.OctaveKeyRelative.HasValue)
                numOctaveKR.Value = LimitRange(numOctaveKR, data.OctaveKeyRelative.Value);

            // Rhythm
            if (data.NoteValue != null && cbNoteValue.Items.Contains(data.NoteValue))
                cbNoteValue.SelectedItem = data.NoteValue;

            if (data.Dots.HasValue)
                numDots.Value = LimitRange(numDots, data.Dots.Value);

            if (data.TupletEnabled.HasValue)
                chkTupletEnabled.Checked = data.TupletEnabled.Value;

            if (data.TupletCount.HasValue)
                numTupletCount.Value = LimitRange(numTupletCount, data.TupletCount.Value);

            if (data.TupletOf.HasValue)
                numTupletOf.Value = LimitRange(numTupletOf, data.TupletOf.Value);

            if (data.TieAcross.HasValue)
                chkTieAcross.Checked = data.TieAcross.Value;

            if (data.Fermata.HasValue)
                chkFermata.Checked = data.Fermata.Value;

            if (data.NumberOfNotes.HasValue)
                numNumberOfNotes.Value = LimitRange(numNumberOfNotes, data.NumberOfNotes.Value);
        }

        //===========================================================================================
        //                      T E S T   S C E N A R I O   B U T T O N S
        //  

        // This sets design test scenario D1
        private void btnSetDesignTestScenarioD1_Click(object sender, EventArgs e)
        {
            // Ensure design exists and apply design defaults
            Globals.Design ??= new DesignerData();
            DesignerTests.SetTestDesignD1(Globals.Design);
        }

        // This sets generator test scenario G1
        // Description: Set generator test values using the current design (in Globals)
        private void btnSetGeneratorTestScenarioG1_Click(object sender, EventArgs e)
        {
            // Merge in any design changes
            GeneratorFormHelper.UpdateGeneratorDataFromDesignData(Globals.GenerationData, _design);

            // Get GenerationData defaults from helper (no UI controls passed)
            Globals.GenerationData = GeneratorTestHelpers.SetTestGeneratorG1(Globals.Design);

            // Apply the generated defaults into the form controls via the existing method
            ApplyFormData(Globals.GenerationData);
        }
    }
}