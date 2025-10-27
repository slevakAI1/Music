// Generate\GenerateForm\GenerateApply.cs
using System;
using System.Linq;
using System.Windows.Forms;
using MusicXml.Domain;
using Music.Design;

namespace Music.Generate
{
    internal static class GenerateApply
    {
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
    }
}