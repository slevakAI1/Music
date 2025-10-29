using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Music.Design;
using MusicXml.Domain;

namespace Music.Generate
{
    public partial class GenerateForm : Form
    {
        private Score? _score;
        private DesignClass? _design;
        private GenerationData? _GenerationData;

        // CORRECT
        public GenerateForm()
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

            // Load static note-value choices once (they do not change during runtime).
            // InitializeComponent has created `cbNoteValue`, so it's safe to call here.
            GenerateFormHelper.LoadNoteValues(cbNoteValue);

            // Initialize local FormData and capture the initial control state
            // - this will get any control default values
            _GenerationData = CaptureFormData();
        }

        // CORRECT
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        // Updated: avoid overwriting design-driven UI when re-applying persisted data.
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // NOTE!: This will need to be revisited when allowed to edit voices, measures later
            // Refresh all UI that depends on the current design (parts, voices, total bar count)
            // Note: LoadNoteValues is intentionally NOT called here — it's static and loaded once at construction.
            GenerateFormHelper.RefreshFromDesign(cbPart, lblEndBarTotal, _design);

            // Re-apply any persisted form data after design-driven refresh
            // DONT THINK THIS IS NEEDEDYET. KEEP AS COMMENTED FOR NOW.
            // if (_GenerationData != null)
            //    ApplyFormData(_GenerationData);
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            // Persist current control state before applying pattern
            _GenerationData = CaptureFormData();

            // Collect selected parts from the CheckedListBox as strings
            var parts = cbPart.CheckedItems
                .Cast<object?>()
                .Select(x => x?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            // Gather scalar values from controls
            var staff = (int)numStaff.Value;
            var startBar = (int)numStartBar.Value;
            var endBar = (int)numEndBar.Value;
            var step = cbStep.SelectedItem?.ToString();
            var accidental = cbAccidental.SelectedItem?.ToString() ?? "Natural";
            var octave = (int)numOctaveAbs.Value;
            var noteValueKey = cbNoteValue.SelectedItem?.ToString();
            var numberOfNotes = (int)numNumberOfNotes.Value;

            // This updates the score based on the pattern
            PatternSetNotes.Apply(
                _score!,
                parts,
                staff,
                startBar,
                endBar,
                step,
                accidental,
                octave,
                noteValueKey,
                numberOfNotes);
            Globals.Score = _score;
        }

        // SET DESIGN AND GENERATE DEFAULTS
        private void btnSetDefault_Click(object? sender, EventArgs e)
        {
            _design = GenerateFormHelper.SetDefaults(
                cbPart, 
                numEndBar, 
                numNumberOfNotes, 
                rbPitchAbsolute, 
                cbStep, 
                cbAccidental, 
                cbPattern, 
                lblEndBarTotal);

            // Refresh UI elements that depend on the design
            GenerateFormHelper.RefreshFromDesign(cbPart, lblEndBarTotal, _design);

            // Persist the control state after defaults are applied
            //_GenerationData = CaptureFormData();
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            _score = GenerateFormHelper.NewScore(this, _design, cbPart, lblEndBarTotal);
        }

        // Capture current control values into a GenerateFormData DTO.
        public GenerationData CaptureFormData()
        {
            var data = new GenerationData
            {
                // Pattern
                Pattern = cbPattern.SelectedItem?.ToString(),

                // Parts
                SelectedParts = cbPart.CheckedItems
                    .Cast<object?>()
                    .Select(x => x?.ToString() ?? string.Empty)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList(),

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
        public void ApplyFormData(GenerationData data)
        {
            if (data == null) return;

            // Pattern
            if (data.Pattern != null && cbPattern.Items.Contains(data.Pattern))
                cbPattern.SelectedItem = data.Pattern;

            // Parts - if provided, set checked state for matching items
            if (data.SelectedParts != null)
            {
                var set = new HashSet<string>(data.SelectedParts, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < cbPart.Items.Count; i++)
                {
                    var name = cbPart.Items[i]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                        cbPart.SetItemChecked(i, set.Contains(name));
                    else
                        cbPart.SetItemChecked(i, false);
                }
            }

            if (data.AllPartsChecked.HasValue)
                chkAllParts.Checked = data.AllPartsChecked.Value;

            if (data.AllStaffChecked.HasValue)
                checkBox1.Checked = data.AllStaffChecked.Value;

            // Staff / sections / bars / beats
            if (data.Staff.HasValue)
                numStaff.Value = ClampDecimal(numStaff, data.Staff.Value);

            if (data.SectionsText != null)
                txtSections.Text = data.SectionsText;

            if (data.StartBar.HasValue)
                numStartBar.Value = ClampDecimal(numStartBar, data.StartBar.Value);

            if (data.EndBar.HasValue)
                numEndBar.Value = ClampDecimal(numEndBar, data.EndBar.Value);

            if (data.StartBeat.HasValue)
                numStartBeat.Value = ClampDecimal(numStartBeat, data.StartBeat.Value);

            if (data.EndBeat.HasValue)
                numericUpDown2.Value = ClampDecimal(numericUpDown2, data.EndBeat.Value);

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
                numOctaveAbs.Value = ClampDecimal(numOctaveAbs, data.OctaveAbsolute.Value);

            if (data.DegreeKeyRelative.HasValue)
                numDegree.Value = ClampDecimal(numDegree, data.DegreeKeyRelative.Value);

            if (data.OctaveKeyRelative.HasValue)
                numOctaveKR.Value = ClampDecimal(numOctaveKR, data.OctaveKeyRelative.Value);

            // Rhythm
            if (data.NoteValue != null && cbNoteValue.Items.Contains(data.NoteValue))
                cbNoteValue.SelectedItem = data.NoteValue;

            if (data.Dots.HasValue)
                numDots.Value = ClampDecimal(numDots, data.Dots.Value);

            if (data.TupletEnabled.HasValue)
                chkTupletEnabled.Checked = data.TupletEnabled.Value;

            if (data.TupletCount.HasValue)
                numTupletCount.Value = ClampDecimal(numTupletCount, data.TupletCount.Value);

            if (data.TupletOf.HasValue)
                numTupletOf.Value = ClampDecimal(numTupletOf, data.TupletOf.Value);

            if (data.TieAcross.HasValue)
                chkTieAcross.Checked = data.TieAcross.Value;

            if (data.Fermata.HasValue)
                chkFermata.Checked = data.Fermata.Value;

            if (data.NumberOfNotes.HasValue)
                numNumberOfNotes.Value = ClampDecimal(numNumberOfNotes, data.NumberOfNotes.Value);
        }

        private static decimal ClampDecimal(NumericUpDown control, int value)
        {
            var min = (int)control.Minimum;
            var max = (int)control.Maximum;
            var clamped = Math.Max(min, Math.Min(max, value));
            return (decimal)clamped;
        }
    }
}