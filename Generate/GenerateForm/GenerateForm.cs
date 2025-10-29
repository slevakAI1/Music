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
    }
}