using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MusicXml.Domain;
using Music.Design;

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

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Minimal: only call per-control populate helpers (no other logic here).
            //PopulatePattern();

            // CONFIRMED CORRECT - DATA INITIALIZATION - POPULATION OF PARTS DATA FROM (LOCAL) DESIGN
            PopulatePartsFromDesign();
            PopulateNoteValue();
            PopulateEndBarTotal(); // populate the total bars label (uses cached _design)
        }

        private void PopulatePattern()
        {
            if (cbPattern.Items.Count != 0) return;

            cbPattern.Items.Add("Set Notes");
            cbPattern.SelectedIndex = 0;
        }

        private void PopulatePartsFromDesign()
        {
            // Populate only when empty
            if (cbPart.Items.Count != 0) return;

            cbPart.Items.Clear();
            cbPart.Items.Add("Choose");

            // Use the cached _design set in the constructor (no Globals access here)
             if (_design?.VoiceSet?.Voices != null)
            {
                foreach (var v in _design.VoiceSet.Voices)
                {
                    var name = v?.VoiceName ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                        cbPart.Items.Add(name);
                }
            }

            if (cbPart.Items.Count > 0)
                cbPart.SelectedIndex = 0;
        }

        private void PopulateNoteValue()
        {
            if (cbNoteValue.Items.Count != 0) return;

            cbNoteValue.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var key in Music.MusicConstants.NoteValueMap.Keys)
                cbNoteValue.Items.Add(key);

            cbNoteValue.SelectedItem = "Quarter (1/4)";
        }

        private void PopulateEndBarTotal()
        {
            // Always refresh the label when called (caller ensures this runs on activate)
            var total = _design?.SectionSet?.TotalBars ?? 0;
            if (total > 0)
                // show as a simple slash + total (appears right of the End Bar control)
                lblEndBarTotal.Text = $"/ {total}";
            else
                lblEndBarTotal.Text = string.Empty;
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_score == null)
            {
                throw new Exception("Cannnot apply to a null score");
            }

            // APPLY TO DESIGN OBJECT FIRST



            // THEN 




            // Collect selected part(s) - the UI uses a ComboBox (single selection)
            var partObj = cbPart.SelectedItem;
            if (partObj == null || string.Equals(partObj.ToString(), "Choose", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "Please select a part to apply notes to.", "No Part Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var parts = new[] { partObj.ToString()! };  // TODO There are too many variables for parts! Fix.

            // Staff value
            var staff = (int)numStaff.Value;

            // Start/End bars (NumericUpDown controls expected)
            var startBar = (int)numStartBar.Value;
            var endBar = (int)numEndBar.Value;
            if (startBar < 1 || endBar < startBar)
            {
                MessageBox.Show(this, "Start and End bars must be valid (Start >= 1 and End >= Start).", "Invalid Bar Range", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Step (absolute)
            var stepStr = cbStep.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(stepStr) || stepStr.Length == 0)
            {
                MessageBox.Show(this, "Please select a step (A-G).", "Invalid Step", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var stepChar = stepStr![0];

            // Accidental
            var accidental = cbAccidental.SelectedItem?.ToString() ?? "Natural";

            // Octave: use only the specific control named "numOcataveAbs"
            var octave = (int)numOctaveAbs.Value;

            // Base duration - map from the UI string via _noteValueMap
            var noteValueKey = cbNoteValue.SelectedItem?.ToString();
            if (noteValueKey == null || !Music.MusicConstants.NoteValueMap.TryGetValue(noteValueKey, out var denom))
            {
                MessageBox.Show(this, "Please select a valid base duration.", "Invalid Duration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ApplySetNote.BaseDuration baseDuration = denom switch
            {
                1 => ApplySetNote.BaseDuration.Whole,
                2 => ApplySetNote.BaseDuration.Half,
                4 => ApplySetNote.BaseDuration.Quarter,
                8 => ApplySetNote.BaseDuration.Eighth,
                16 => ApplySetNote.BaseDuration.Sixteenth,
                _ => ApplySetNote.BaseDuration.Quarter
            };

            // Number of notes: accept multiple candidate control names
            var numberOfNotes = (int)numNumberOfNotes.Value;

            // Call ApplySetNote to mutate the _score in-place. Catch validation errors from Apply.
            try
            {
                ApplySetNote.Apply(
                    _score,
                    parts,
                    staff,
                    startBar,
                    endBar,
                    stepChar,
                    accidental,
                    octave,
                    baseDuration,
                    numberOfNotes);

                // _score has been updated in-place by ApplySetNote.
                MessageBox.Show(this, "Notes applied successfully.", "Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error applying notes:\n{ex.Message}", "Apply Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // SET DESIGN AND GENERATE DEFAULTS
        private void btnSetDefault_Click(object? sender, EventArgs e)
        {
            Globals.Design ??= new DesignClass();
            DesignDefaults.ApplyDefaultDesign(Globals.Design);
            _design = Globals.Design;

            SetDefaultsForGenerate();

            // Refresh parts and end-total UI
            PopulatePartsFromDesign();
            PopulateEndBarTotal();
        }

        private void SetDefaultsForGenerate()
        {
            // Ensure parts are populated
            PopulatePartsFromDesign();

            // Ensure "Keyboard" voice exists and select it
            var idx = cbPart.Items.IndexOf("Keyboard");
            if (idx == -1)
            {
                cbPart.Items.Add("Keyboard");
                idx = cbPart.Items.Count - 1;
            }
            if (idx >= 0)
                cbPart.SelectedIndex = idx;

            // Set End Bar to design total (clamped inside numEndBar range)
            var total = _design?.SectionSet?.TotalBars ?? Globals.Design?.SectionSet?.TotalBars ?? 0;
            if (total > 0)
            {
                var clamped = Math.Max((int)numEndBar.Minimum, Math.Min((int)numEndBar.Maximum, total));
                numEndBar.Value = clamped;
            }

            // Other control defaults
            numNumberOfNotes.Value = 4;
            rbPitchAbsolute.Checked = true;
            cbStep.SelectedIndex = 0;       // C
            cbAccidental.SelectedIndex = 0; // Natural
            cbPattern.SelectedIndex = 0;    // Set Notes                                            
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            // Create a fresh Score instance and assign to the local cache
            _score = new Score
            {
                MovementTitle = string.Empty,
                Identification = new Identification(),
                Parts = new List<Part>()
            };

            // Prefer the form-local design, fall back to Globals if missing
            var design = _design ?? Globals.Design;
            if (design == null)
            {
                MessageBox.Show(this, "No design available. Create or set a design before creating a new score.", "No Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Collect part names from design voices (same logic as PopulatePartsFromDesign)
            var partNames = new List<string>();
            if (design.VoiceSet?.Voices != null)
            {
                foreach (var v in design.VoiceSet.Voices)
                {
                    var name = v?.VoiceName;
                    if (!string.IsNullOrWhiteSpace(name))
                        partNames.Add(name);
                }
            }

            if (partNames.Count == 0)
            {
                MessageBox.Show(this, "Design contains no voices to create parts from.", "No Parts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ensure parts exist in the new score (this will add Part entries for each name)
            try
            {
                ScorePartsHelper.EnsurePartsExist(_score, partNames);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error creating parts: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Determine how many measures to create from the design's section set
            var totalBars = design.SectionSet?.TotalBars ?? 0;

            // Ensure each part has a Measures list and enough Measure entries
            foreach (var part in _score.Parts ?? Enumerable.Empty<Part>())
            {
                part.Measures ??= new List<Measure>();
                while (part.Measures.Count < totalBars)
                {
                    part.Measures.Add(new Measure());
                }
            }

            // Refresh UI that depends on design/parts
            PopulatePartsFromDesign();
            PopulateEndBarTotal();

            MessageBox.Show(this, "New score created from design.", "New Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //================================  SAVE FOR NOW     ========================================
    }
}