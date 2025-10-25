using System;
using System.Linq;
using System.Reflection;
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
                MessageBox.Show(this, "No Score is loaded. Use SetScore(Score) to provide a MusicXML Score.", "No Score", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // TODO - implement apply logic 
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