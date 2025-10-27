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
        public static void Apply(Form owner, Score? score, string[] parts, int staff, int startBar, int endBar, string? stepStr, string accidental, int octave, string? noteValueKey, int numberOfNotes)
        {
            if (score == null)
            {
                throw new Exception("Cannnot apply to a null score");
            }
            
            // Validate parts
            if (parts == null || parts.Length == 0)
            {
                MessageBox.Show(owner, "Please select a part to apply notes to.", "No Part Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Staff value already provided

            // Start/End bars
            if (startBar < 1 || endBar < startBar)
            {
                MessageBox.Show(owner, "Start and End bars must be valid (Start >= 1 and End >= Start).", "Invalid Bar Range", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Step (absolute)
            if (string.IsNullOrWhiteSpace(stepStr) || stepStr.Length == 0)
            {
                MessageBox.Show(owner, "Please select a step (A-G).", "Invalid Step", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var stepChar = stepStr![0];

            // Accidental already provided

            // Octave: value provided

            // Base duration - map from the UI string via _noteValueMap
            if (noteValueKey == null || !Music.MusicConstants.NoteValueMap.TryGetValue(noteValueKey, out var noteValue))
            {
                MessageBox.Show(owner, "Please select a valid base duration.", "Invalid Duration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Pass the denominator directly to ApplySetNote to avoid unnecessary remapping to an enum and back.

            // Number of notes already provided

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
                    noteValue,
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