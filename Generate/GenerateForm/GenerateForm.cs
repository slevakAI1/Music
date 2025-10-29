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

        // CORRECT
        private Score? _score;
        private DesignClass? _design;

        public GenerateForm()
        {
            InitializeComponent();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
           
            // CORRECT
            // Load current global score and design into form-local fields for later use
            // Constructor is the only place that reads Globals per requirement.
            _score = Globals.Score;
            _design = Globals.Design;
        }

        // CORRECT
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        // CONFIRMED CORRECT
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Refresh all UI that depends on the current design
            GenerateFormHelper.RefreshFromDesign(cbPart, lblEndBarTotal, cbNoteValue, _design);
        }

        // CONFIRMED CORRECT
        private void btnApply_Click(object sender, EventArgs e)
        {
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
            GenerateFormHelper.RefreshFromDesign(cbPart, lblEndBarTotal, cbNoteValue, _design);
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            _score = GenerateFormHelper.NewScore(this, _design, cbPart, lblEndBarTotal);
        }

        // Data holder object for GenerateForm user-editable values.
        // - All properties are simple data types (strings, ints, bools, List<string>)
        // - Value types are nullable so ApplyToControls() only writes when a property has a value.
        public sealed class FormData
        {
            private readonly GenerateForm _owner;

            public FormData(GenerateForm owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            // General / Pattern
            public string? Pattern { get; set; }

            // Parts / scope
            public List<string>? SelectedParts { get; set; }
            public bool? AllPartsChecked { get; set; }
            public bool? AllStaffChecked { get; set; } // corresponds to `checkBox1` in designer (labeled "All")

            // Staff / sections / bars / beats
            public int? Staff { get; set; }
            public string? SectionsText { get; set; }
            public int? StartBar { get; set; }
            public int? EndBar { get; set; }
            public int? StartBeat { get; set; }
            public int? EndBeat { get; set; }

            // Overwrite existing notes
            public bool? OverwriteExisting { get; set; }

            // Pitch options
            public bool? PitchAbsolute { get; set; } // true = Absolute, false = Key-relative
            public string? Step { get; set; } // e.g., "C"
            public string? Accidental { get; set; } // "Natural"/"Sharp"/"Flat"
            public int? OctaveAbsolute { get; set; }
            public int? DegreeKeyRelative { get; set; }
            public int? OctaveKeyRelative { get; set; }

            // Rhythm options
            public string? NoteValue { get; set; } // selected key from cbNoteValue
            public int? Dots { get; set; }
            public bool? TupletEnabled { get; set; }
            public int? TupletCount { get; set; }
            public int? TupletOf { get; set; }
            public bool? TieAcross { get; set; }
            public bool? Fermata { get; set; }
            public int? NumberOfNotes { get; set; }

            // Gather current values from the form controls into this object's properties
            public void GatherFromControls()
            {
                // Pattern
                Pattern = _owner.cbPattern.SelectedItem?.ToString();

                // Parts
                SelectedParts = _owner.cbPart.CheckedItems
                    .Cast<object?>()
                    .Select(x => x?.ToString() ?? string.Empty)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                AllPartsChecked = _owner.chkAllParts.Checked;
                AllStaffChecked = _owner.checkBox1.Checked;

                // Staff / sections / bars / beats
                Staff = (int)_owner.numStaff.Value;
                SectionsText = _owner.txtSections.Text;
                StartBar = (int)_owner.numStartBar.Value;
                EndBar = (int)_owner.numEndBar.Value;
                StartBeat = (int)_owner.numStartBeat.Value;
                EndBeat = (int)_owner.numericUpDown2.Value;

                OverwriteExisting = _owner.chkOverwrite.Checked;

                // Pitch
                PitchAbsolute = _owner.rbPitchAbsolute.Checked;
                Step = _owner.cbStep.SelectedItem?.ToString();
                Accidental = _owner.cbAccidental.SelectedItem?.ToString();
                OctaveAbsolute = (int)_owner.numOctaveAbs.Value;
                DegreeKeyRelative = (int)_owner.numDegree.Value;
                OctaveKeyRelative = (int)_owner.numOctaveKR.Value;

                // Rhythm
                NoteValue = _owner.cbNoteValue.SelectedItem?.ToString();
                Dots = (int)_owner.numDots.Value;
                TupletEnabled = _owner.chkTupletEnabled.Checked;
                TupletCount = (int)_owner.numTupletCount.Value;
                TupletOf = (int)_owner.numTupletOf.Value;
                TieAcross = _owner.chkTieAcross.Checked;
                Fermata = _owner.chkFermata.Checked;
                NumberOfNotes = (int)_owner.numNumberOfNotes.Value;
            }

            // Apply the current property values back to the form controls.
            // Only properties that are not null are written to controls (so callers can use partial updates).
            public void ApplyToControls()
            {
                // Pattern
                if (Pattern != null && _owner.cbPattern.Items.Contains(Pattern))
                    _owner.cbPattern.SelectedItem = Pattern;

                // Parts - if provided, set checked state for matching items
                if (SelectedParts != null)
                {
                    var set = new HashSet<string>(SelectedParts, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < _owner.cbPart.Items.Count; i++)
                    {
                        var name = _owner.cbPart.Items[i]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(name))
                            _owner.cbPart.SetItemChecked(i, set.Contains(name));
                        else
                            _owner.cbPart.SetItemChecked(i, false);
                    }
                }

                if (AllPartsChecked.HasValue)
                    _owner.chkAllParts.Checked = AllPartsChecked.Value;

                if (AllStaffChecked.HasValue)
                    _owner.checkBox1.Checked = AllStaffChecked.Value;

                // Staff / sections / bars / beats
                if (Staff.HasValue)
                    _owner.numStaff.Value = ClampDecimal(_owner.numStaff, Staff.Value);

                if (SectionsText != null)
                    _owner.txtSections.Text = SectionsText;

                if (StartBar.HasValue)
                    _owner.numStartBar.Value = ClampDecimal(_owner.numStartBar, StartBar.Value);

                if (EndBar.HasValue)
                    _owner.numEndBar.Value = ClampDecimal(_owner.numEndBar, EndBar.Value);

                if (StartBeat.HasValue)
                    _owner.numStartBeat.Value = ClampDecimal(_owner.numStartBeat, StartBeat.Value);

                if (EndBeat.HasValue)
                    _owner.numericUpDown2.Value = ClampDecimal(_owner.numericUpDown2, EndBeat.Value);

                if (OverwriteExisting.HasValue)
                    _owner.chkOverwrite.Checked = OverwriteExisting.Value;

                // Pitch
                if (PitchAbsolute.HasValue)
                {
                    _owner.rbPitchAbsolute.Checked = PitchAbsolute.Value;
                    _owner.rbPitchKeyRelative.Checked = !PitchAbsolute.Value;
                }

                if (Step != null && _owner.cbStep.Items.Contains(Step))
                    _owner.cbStep.SelectedItem = Step;

                if (Accidental != null && _owner.cbAccidental.Items.Contains(Accidental))
                    _owner.cbAccidental.SelectedItem = Accidental;

                if (OctaveAbsolute.HasValue)
                    _owner.numOctaveAbs.Value = ClampDecimal(_owner.numOctaveAbs, OctaveAbsolute.Value);

                if (DegreeKeyRelative.HasValue)
                    _owner.numDegree.Value = ClampDecimal(_owner.numDegree, DegreeKeyRelative.Value);

                if (OctaveKeyRelative.HasValue)
                    _owner.numOctaveKR.Value = ClampDecimal(_owner.numOctaveKR, OctaveKeyRelative.Value);

                // Rhythm
                if (NoteValue != null && _owner.cbNoteValue.Items.Contains(NoteValue))
                    _owner.cbNoteValue.SelectedItem = NoteValue;

                if (Dots.HasValue)
                    _owner.numDots.Value = ClampDecimal(_owner.numDots, Dots.Value);

                if (TupletEnabled.HasValue)
                    _owner.chkTupletEnabled.Checked = TupletEnabled.Value;

                if (TupletCount.HasValue)
                    _owner.numTupletCount.Value = ClampDecimal(_owner.numTupletCount, TupletCount.Value);

                if (TupletOf.HasValue)
                    _owner.numTupletOf.Value = ClampDecimal(_owner.numTupletOf, TupletOf.Value);

                if (TieAcross.HasValue)
                    _owner.chkTieAcross.Checked = TieAcross.Value;

                if (Fermata.HasValue)
                    _owner.chkFermata.Checked = Fermata.Value;

                if (NumberOfNotes.HasValue)
                    _owner.numNumberOfNotes.Value = ClampDecimal(_owner.numNumberOfNotes, NumberOfNotes.Value);
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
}