using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MusicXml.Domain;
using Music.Design;

namespace Music.Generate
{
    internal static class GenerateFormHelper
    {
        public static void PopulatePartsFromDesign(CheckedListBox cbPart, DesignClass? design)
        {
            // Preserve currently checked part names so we can re-apply them after repopulating.
            var previouslyChecked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in cbPart.CheckedItems.Cast<object?>())
            {
                var name = item?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                    previouslyChecked.Add(name!);
            }

            // Always rebuild the list from the design (clear then add).
            cbPart.Items.Clear();

            // Use the provided design instance (no Globals access here)
            if (design?.VoiceSet?.Voices != null)
            {
                foreach (var v in design.VoiceSet.Voices)
                {
                    var name = v?.VoiceName ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        cbPart.Items.Add(name);
                        // Re-apply previously checked state (or any state set by callers before this refresh)
                        if (previouslyChecked.Contains(name))
                            cbPart.SetItemChecked(cbPart.Items.Count - 1, true);
                    }
                }
            }

            // no automatic checks here beyond re-applying preserved checked names;
            // callers (e.g., SetDefaults) can explicitly check the Keyboard item if required.
        }

        public static void LoadNoteValues(ComboBox cbNoteValue)
        {
            if (cbNoteValue.Items.Count != 0) return;

            cbNoteValue.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var key in Music.MusicConstants.NoteValueMap.Keys)
                cbNoteValue.Items.Add(key);

            cbNoteValue.SelectedItem = "Quarter (1/4)";
        }

        public static void LoadEndBarTotalFromDesign(Label lblEndBarTotal, DesignClass? design)
        {
            // Always refresh the label when called (caller ensures this runs on activate)
            var total = design?.SectionSet?.TotalBars ?? 0;
            if (total > 0)
                // show as a simple slash + total (appears right of the End Bar control)
                lblEndBarTotal.Text = $"/ {total}";
            else
                lblEndBarTotal.Text = string.Empty;
        }

        // New helper to refresh all UI that depends on the current design
        public static void RefreshFromDesign(CheckedListBox cbPart, Label lblEndBarTotal, ComboBox cbNoteValue, DesignClass? design)
        {
            // Ensure note value list is loaded
            LoadNoteValues(cbNoteValue);

            // Populate parts and update end-bar total based on the provided design
            PopulatePartsFromDesign(cbPart, design);
            LoadEndBarTotalFromDesign(lblEndBarTotal, design);
        }

        public static void SetDefaultsForGenerate(DesignClass? design, CheckedListBox cbPart, NumericUpDown numEndBar, NumericUpDown numNumberOfNotes, RadioButton rbPitchAbsolute, ComboBox cbStep, ComboBox cbAccidental, ComboBox cbPattern)
        {
            // Ensure parts are populated
            PopulatePartsFromDesign(cbPart, design);

            // Ensure "Keyboard" voice exists and select it
            var idx = cbPart.Items.IndexOf("Keyboard");
            if (idx == -1)
            {
                cbPart.Items.Add("Keyboard");
                idx = cbPart.Items.Count - 1;
            }
            // check the first matching item (CheckedListBox uses SetItemChecked)
            if (idx >= 0)
                cbPart.SetItemChecked(idx, true);

            // Set End Bar to design total (clamped inside numEndBar range)
            var total = design?.SectionSet?.TotalBars ?? Globals.Design?.SectionSet?.TotalBars ?? 0;
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
        public static DesignClass? SetDefaults(CheckedListBox cbPart, NumericUpDown numEndBar, NumericUpDown numNumberOfNotes, RadioButton rbPitchAbsolute, ComboBox cbStep, ComboBox cbAccidental, ComboBox cbPattern, Label lblEndBarTotal)
        {
            Globals.Design ??= new DesignClass();
            DesignDefaults.ApplyDefaultDesign(Globals.Design);

            // Call into helper SetDefaultsForGenerate using the global design (mirrors original)
            SetDefaultsForGenerate(Globals.Design, cbPart, numEndBar, numNumberOfNotes, rbPitchAbsolute, cbStep, cbAccidental, cbPattern);

            // Refresh parts and end-total UI - keep same sequence as original method
            PopulatePartsFromDesign(cbPart, Globals.Design);
            LoadEndBarTotalFromDesign(lblEndBarTotal, Globals.Design);

            // Return the design so the form can update its local cache
            return Globals.Design;
        }

        public static Score? NewScore(
            Form owner, 
            DesignClass? 
            design, 
            CheckedListBox cbPart, 
            Label lblEndBarTotal)
        {
            // Create a fresh Score instance and assign to the local cache (returned to caller)
            var score = new Score
            {
                MovementTitle = string.Empty,
                Identification = new Identification(),
                Parts = new List<Part>()
            };

            // Prefer the passed design, fall back to Globals if missing
            var usedDesign = design ?? Globals.Design;
            if (usedDesign == null)
            {
                MessageBox.Show(owner, "No design available. Create or set a design before creating a new score.", "No Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            // Collect part names from design voices (same logic as PopulatePartsFromDesign)
            var partNames = new List<string>();
            if (usedDesign.VoiceSet?.Voices != null)
            {
                foreach (var v in usedDesign.VoiceSet.Voices)
                {
                    var name = v?.VoiceName;
                    if (!string.IsNullOrWhiteSpace(name))
                        partNames.Add(name);
                }
            }

            if (partNames.Count == 0)
            {
                MessageBox.Show(owner, "Design contains no voices to create parts from.", "No Parts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            // Ensure parts exist in the new score (this will add Part entries for each name)
            try
            {
                ScorePartsHelper.EnsurePartsExist(score, partNames);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, $"Error creating parts: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            // Determine how many measures to create from the design's section set
            var totalBars = usedDesign.SectionSet?.TotalBars ?? 0;

            // Ensure each part has a Measures list and enough Measure entries
            foreach (var part in score.Parts ?? Enumerable.Empty<Part>())
            {
                part.Measures ??= new List<Measure>();
                while (part.Measures.Count < totalBars)
                {
                    part.Measures.Add(new Measure());
                }
            }

            // Refresh UI that depends on design/parts
            PopulatePartsFromDesign(cbPart, usedDesign);
            LoadEndBarTotalFromDesign(lblEndBarTotal, usedDesign);

            MessageBox.Show(owner, "New score created from design.", "New Score", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return score;
        }
    }
}