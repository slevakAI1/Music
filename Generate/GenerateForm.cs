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
        private Score? _score;
        private DesignClass? _design;

        private readonly Dictionary<string, int> _noteValueMap = new()
        {
            ["Whole (1)"] = 1,
            ["Half (1/2)"] = 2,
            ["Quarter (1/4)"] = 4,
            ["Eighth (1/8)"] = 8,
            ["16th (1/16)"] = 16
        };

        public GenerateForm()
        {
            InitializeComponent();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            // Keep pitch-mode radio settable repeatedly (not a list)
            rbPitchAbsolute.Checked = true;

            // Load current global score and design into form-local fields for later use
            // Constructor is the only place that reads Globals per requirement.
            _score = Globals.Score;
            _design = Globals.Design;

            // Note: list-type controls are NOT populated here.
            // They will be populated on activation by the Populate...() methods.
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Ensure design sections have up-to-date starts/derived values before reading totals
            _design?.SectionSet?.RecalculateStarts();

            // Minimal: only call per-control populate helpers (no other logic here).
            PopulatePattern();
            PopulateStep();
            PopulateAccidental();
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

        private void PopulateStep()
        {
            if (cbStep.Items.Count != 0) return;

            cbStep.Items.AddRange(new object[] { "C", "D", "E", "F", "G", "A", "B" });
            cbStep.SelectedIndex = 0;
        }

        private void PopulateAccidental()
        {
            if (cbAccidental.Items.Count != 0) return;

            cbAccidental.Items.AddRange(new object[] { "Natural", "Sharp", "Flat" });
            cbAccidental.SelectedIndex = 0;
        }

        private void PopulatePartsFromDesign()
        {
            // Populate only when empty
            if (cbPart.Items.Count != 0) return;

            cbPart.Items.Clear();
            cbPart.Items.Add("Choose");

            // Use the cached _design set in the constructor (no Globals access here)
            var design = _design;
            if (design?.VoiceSet?.Voices != null)
            {
                foreach (var v in design.VoiceSet.Voices)
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
            foreach (var key in _noteValueMap.Keys)
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
             _score = Globals.Score;
            }

            // Collect selected part(s) - the UI uses a ComboBox (single selection)
            var selectedPartObj = cbPart.SelectedItem;
            if (selectedPartObj == null || string.Equals(selectedPartObj.ToString(), "Choose", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "Please select a part to apply notes to.", "No Part Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var selectedParts = new[] { selectedPartObj.ToString()! };

            // Staff value: try multiple candidate control names (NumericUpDown preferred, then TextBox)
            if (!TryGetIntFromControls(new[] { "txtStaff", "numStaff", "nudStaff", "Staff" }, out var staff))
            {
                MessageBox.Show(this, "Staff must be a valid integer (check Staff control).", "Invalid Staff", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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

            // Octave: accept several candidate control names
            if (!TryGetIntFromControls(new[] { "numOctave", "nudOctave", "OctaveAbsolute", "numOctaveAbsolute" }, out var octave))
            {
                MessageBox.Show(this, "Octave must be a valid integer (check Octave control).", "Invalid Octave", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Base duration - map from the UI string via _noteValueMap
            var noteValueKey = cbNoteValue.SelectedItem?.ToString();
            if (noteValueKey == null || !_noteValueMap.TryGetValue(noteValueKey, out var denom))
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
            if (!TryGetIntFromControls(new[] { "numNumberOfNotes", "numNotes", "nudNumberOfNotes", "NumberOfNotes" }, out var numberOfNotes))
            {
                MessageBox.Show(this, "Number of Notes must be a valid integer (check Number of Notes control).", "Invalid Number", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (numberOfNotes <= 0)
            {
                MessageBox.Show(this, "Number of Notes must be greater than zero.", "Invalid Number", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Call ApplySetNote to mutate the _score in-place. Catch validation errors from Apply.
            try
            {
                ApplySetNote.Apply(
                    _score,
                    selectedParts,
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

        /// <summary>
        /// Attempts to retrieve an integer value from one of the named controls.
        /// Supports NumericUpDown and TextBox controls. Search is recursive (child controls included).
        /// </summary>
        private bool TryGetIntFromControls(string[] candidateNames, out int value)
        {
            value = 0;
            foreach (var name in candidateNames)
            {
                var ctrl = FindControlByName(name);
                if (ctrl == null) continue;

                if (ctrl is NumericUpDown nud)
                {
                    value = (int)nud.Value;
                    return true;
                }

                if (ctrl is TextBox tb && int.TryParse(tb.Text.Trim(), out var v1))
                {
                    value = v1;
                    return true;
                }

                if (ctrl is ComboBox cb && int.TryParse(cb.Text.Trim(), out var v2))
                {
                    value = v2;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds a control by name anywhere in the form's control hierarchy.
        /// </summary>
        private Control? FindControlByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var matches = this.Controls.Find(name, true);
            return matches.FirstOrDefault();
        }

        private void btnSetDefault_Click(object? sender, EventArgs e)
        {
            // 1) Run Design form's Set Default logic if a DesignForm MDI child is available.
            var mdi = this.MdiParent;
            if (mdi != null)
            {
                var designForm = mdi.MdiChildren.FirstOrDefault(f => f.GetType().Name == "DesignForm");
                if (designForm != null)
                {
                    // Invoke the private click handler on DesignForm to reuse its exact behavior.
                    var mi = designForm.GetType().GetMethod("btnSetDefault_Click", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (mi != null)
                    {
                        try
                        {
                            mi.Invoke(designForm, new object[] { designForm, EventArgs.Empty });
                        }
                        catch
                        {
                            // swallow reflection errors; fall back to central defaults below
                        }
                    }
                }
            }

            // If no design exists yet, or reflection call didn't run, ensure central defaults are applied
            Globals.Design ??= new DesignClass();
            DesignDefaults.ApplyDefaultDesign(Globals.Design);

            // Update local cache and set generate-specific defaults
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
        }
               
        //================================  SAVE FOR NOW     ========================================
    }
}