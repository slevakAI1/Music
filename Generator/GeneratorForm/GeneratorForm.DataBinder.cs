using System;
using System.Linq;
using static Music.Helpers;

namespace Music.Generator
{
    // Converted helper into a partial class so it can access designer controls directly
    public partial class GeneratorForm
    {
        // Capture current control values into a class object.
        // No form parameter required because this is now a partial of GeneratorForm.
        public GeneratorData CaptureFormData()
        {
            // Capture parts items and their checked state into a dictionary
            var partsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (clbParts != null)
            {
                for (int i = 0; i < clbParts.Items.Count; i++)
                {
                    var name = clbParts.Items[i]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                        partsState[name] = clbParts.GetItemChecked(i);
                }
            }

            // Determine step selection and Rest handling
            string? stepSelected = cbStep?.SelectedItem?.ToString();
            var isRest = !string.IsNullOrWhiteSpace(stepSelected) && stepSelected.Equals("Rest", StringComparison.OrdinalIgnoreCase);

            // Convert step string to char (use '\0' for Rest)
            char stepChar = '\0';
            if (!isRest && !string.IsNullOrWhiteSpace(stepSelected))
            {
                stepChar = stepSelected[0];
            }

            var data = new GeneratorData
            {
                // Pattern
                Pattern = cbPattern?.SelectedItem?.ToString(),

                // New: store the full items -> checked state map
                PartsState = partsState,

                // Staff / sections / bars / beats
                Staff = (int?)(numStaff?.Value ?? 1),
                SectionsText = txtSections?.Text,
                StartBar = (int?)(numStartBar?.Value ?? 1),
                EndBar = (int?)(numEndBar?.Value ?? 1),
                StartBeat = (int?)(numStartBeat?.Value ?? 1),
                EndBeat = (int?)(numericUpDown2?.Value ?? 1),

                OverwriteExisting = chkOverwrite?.Checked ?? false,

                // Pitch
                PitchAbsolute = rbPitchAbsolute?.Checked ?? true,
                // Step is now char type; '\0' when Rest is selected
                Step = stepChar,
                IsRest = isRest,
                Accidental = cbAccidental?.SelectedItem?.ToString(),
                OctaveAbsolute = (int?)(numOctaveAbs?.Value ?? 4),
                DegreeKeyRelative = (int?)(numDegree?.Value ?? 0),
                OctaveKeyRelative = (int?)(numOctaveKR?.Value ?? 4),

                // Rhythm
                NoteValue = cbNoteValue?.SelectedItem?.ToString(),
                Dots = (int?)(numDots?.Value ?? 0),
                TupletEnabled = chkTupletEnabled?.Checked ?? false,
                TupletCount = (int?)(numTupletCount?.Value ?? 0),
                TupletOf = (int?)(numTupletOf?.Value ?? 0),
                TieAcross = chkTieAcross?.Checked ?? false,
                Fermata = chkFermata?.Checked ?? false,
                NumberOfNotes = (int?)(numNumberOfNotes?.Value ?? 1)
            };

            return data;
        }

        // Apply a GenerateFormData object back to the private form controls.
        // No form parameter required because this is a partial of GeneratorForm.
        public void ApplyFormData(GeneratorData data)
        {
            if (data == null) return;

            // Pattern
            if (data.Pattern != null && cbPattern != null && cbPattern.Items.Contains(data.Pattern))
                cbPattern.SelectedItem = data.Pattern;

            // Parts - if provided, set checked state for matching items
            if (data.PartsState != null && data.PartsState.Count > 0 && clbParts != null)
            {
                // Use case-insensitive lookup
                var map = new Dictionary<string, bool>(data.PartsState, StringComparer.OrdinalIgnoreCase);

                // Clear existing items and populate from the map so control contains exactly the map entries
                clbParts.BeginUpdate();
                try
                {
                    clbParts.Items.Clear();
                    foreach (var kv in map)
                    {
                        var name = kv.Key ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        var idx = clbParts.Items.Add(name);
                        clbParts.SetItemChecked(idx, kv.Value);
                    }
                }
                finally
                {
                    clbParts.EndUpdate();
                }
            }

            // Staff / sections / bars / beats
            if (data.Staff.HasValue && numStaff != null)
                numStaff.Value = LimitRange(numStaff, data.Staff.Value);

            if (data.SectionsText != null && txtSections != null)
                txtSections.Text = data.SectionsText;

            if (data.StartBar.HasValue && numStartBar != null)
                numStartBar.Value = LimitRange(numStartBar, data.StartBar.Value);

            if (data.EndBar.HasValue && numEndBar != null)
                numEndBar.Value = LimitRange(numEndBar, data.EndBar.Value);

            if (data.StartBeat.HasValue && numStartBeat != null)
                numStartBeat.Value = LimitRange(numStartBeat, data.StartBeat.Value);

            if (data.EndBeat.HasValue && numericUpDown2 != null)
                numericUpDown2.Value = LimitRange(numericUpDown2, data.EndBeat.Value);

            if (data.OverwriteExisting.HasValue && chkOverwrite != null)
                chkOverwrite.Checked = data.OverwriteExisting.Value;

            // Pitch
            if (data.PitchAbsolute.HasValue && rbPitchAbsolute != null && rbPitchKeyRelative != null)
            {
                rbPitchAbsolute.Checked = data.PitchAbsolute.Value;
                rbPitchKeyRelative.Checked = !data.PitchAbsolute.Value;
            }

            // If IsRest is true (assume "Rest" exists in cbStep items), select it directly.
            // Otherwise, convert the char Step to string for selection.
            if (cbStep != null)
            {
                if (data.IsRest == true)
                {
                    cbStep.SelectedItem = "Rest";
                }
                else if (data.Step != '\0')
                {
                    string stepString = data.Step.ToString();
                    if (cbStep.Items.Contains(stepString))
                        cbStep.SelectedItem = stepString;
                }
            }

            if (data.Accidental != null && cbAccidental != null && cbAccidental.Items.Contains(data.Accidental))
                cbAccidental.SelectedItem = data.Accidental;

            if (data.OctaveAbsolute.HasValue && numOctaveAbs != null)
                numOctaveAbs.Value = LimitRange(numOctaveAbs, data.OctaveAbsolute.Value);

            if (data.DegreeKeyRelative.HasValue && numDegree != null)
                numDegree.Value = LimitRange(numDegree, data.DegreeKeyRelative.Value);

            if (data.OctaveKeyRelative.HasValue && numOctaveKR != null)
                numOctaveKR.Value = LimitRange(numOctaveKR, data.OctaveKeyRelative.Value);

            // Rhythm
            if (data.NoteValue != null && cbNoteValue != null && cbNoteValue.Items.Contains(data.NoteValue))
                cbNoteValue.SelectedItem = data.NoteValue;

            if (data.Dots.HasValue && numDots != null)
                numDots.Value = LimitRange(numDots, data.Dots.Value);

            if (data.TupletEnabled.HasValue && chkTupletEnabled != null)
                chkTupletEnabled.Checked = data.TupletEnabled.Value;

            if (data.TupletCount.HasValue && numTupletCount != null)
                numTupletCount.Value = LimitRange(numTupletCount, data.TupletCount.Value);

            if (data.TupletOf.HasValue && numTupletOf != null)
                numTupletOf.Value = LimitRange(numTupletOf, data.TupletOf.Value);

            if (data.TieAcross.HasValue && chkTieAcross != null)
                chkTieAcross.Checked = data.TieAcross.Value;

            if (data.Fermata.HasValue && chkFermata != null)
                chkFermata.Checked = data.Fermata.Value;

            if (data.NumberOfNotes.HasValue && numNumberOfNotes != null)
                numNumberOfNotes.Value = LimitRange(numNumberOfNotes, data.NumberOfNotes.Value);
        }
    }
}