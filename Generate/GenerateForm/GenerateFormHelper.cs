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
            // Populate only when empty
            if (cbPart.Items.Count != 0) return;

            cbPart.Items.Clear();

            // Use the provided design instance (no Globals access here except fallback where original code had it)
            if (design?.VoiceSet?.Voices != null)
            {
                foreach (var v in design.VoiceSet.Voices)
                {
                    var name = v?.VoiceName ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                        cbPart.Items.Add(name);
                }
            }

            // no automatic checks here; UI starts with none checked
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

        public static void Apply(Form owner, Score? score, CheckedListBox cbPart, NumericUpDown numStaff, NumericUpDown numStartBar, NumericUpDown numEndBar, ComboBox cbStep, ComboBox cbAccidental, NumericUpDown numOctaveAbs, ComboBox cbNoteValue, NumericUpDown numNumberOfNotes)
        {
            if (score == null)
            {
                throw new Exception("Cannnot apply to a null score");
            }

            // APPLY TO DESIGN OBJECT FIRST



            // THEN 






            // Collect selected part(s) - the UI uses a CheckedListBox (multiple selection)
            string? chosenPart = null;
            if (cbPart.CheckedItems.Count > 0)
            {
                chosenPart = cbPart.CheckedItems[0]?.ToString();
            }

            if (string.IsNullOrWhiteSpace(chosenPart))
            {
                MessageBox.Show(owner, "Please select a part to apply notes to.", "No Part Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var parts = new[] { chosenPart };

            // Staff value
            var staff = (int)numStaff.Value;

            // Start/End bars (NumericUpDown controls expected)
            var startBar = (int)numStartBar.Value;
            var endBar = (int)numEndBar.Value;
            if (startBar < 1 || endBar < startBar)
            {
                MessageBox.Show(owner, "Start and End bars must be valid (Start >= 1 and End >= Start).", "Invalid Bar Range", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Step (absolute)
            var stepStr = cbStep.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(stepStr) || stepStr.Length == 0)
            {
                MessageBox.Show(owner, "Please select a step (A-G).", "Invalid Step", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show(owner, "Please select a valid base duration.", "Invalid Duration", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // Call ApplySetNote to mutate the score in-place. Catch validation errors from Apply.
            try
            {
                ApplySetNote.Apply(
                    score,
                    parts,
                    staff,
                    startBar,
                    endBar,
                    stepChar,
                    accidental,
                    octave,
                    baseDuration,
                    numberOfNotes);

                // score has been updated in-place by ApplySetNote.
                MessageBox.Show(owner, "Notes applied successfully.", "Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, $"Error applying notes:\n{ex.Message}", "Apply Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static Score? NewScore(Form owner, DesignClass? design, CheckedListBox cbPart, Label lblEndBarTotal)
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