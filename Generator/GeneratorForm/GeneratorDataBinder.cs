using Melanchall.DryWetMidi.Composing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using Music.Generator;
using static Music.Helpers;
using static Music.MusicConstants;

namespace Music.Generator
{
    internal class GeneratorDataBinder
    {
        //========================   F O R M   D A T A   M A N A G E M E N T   ========================
        // TODO Move this to owner/helper class

        // Capture current control values into a GenerateFormData DTO.
        // Note: we accept the form instance and access its controls via reflection to avoid
        // passing individual controls or changing the form's access modifiers.
        public GeneratorData CaptureFormData(GeneratorForm form)
        {
            // Helper to fetch private fields by name on the form (searching base types as needed)
            T GetField<T>(string name) where T : class
            {
                var t = form.GetType();
                while (t != null)
                {
                    var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (fi != null)
                        return fi.GetValue(form) as T;
                    t = t.BaseType;
                }
                return null;
            }

            // Capture parts items and their checked state into a dictionary
            var partsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var clbParts = GetField<CheckedListBox>("clbParts");
            if (clbParts != null)
            {
                for (int i = 0; i < clbParts.Items.Count; i++)
                {
                    var name = clbParts.Items[i]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                        partsState[name] = clbParts.GetItemChecked(i);
                }
            }

            var cbPattern = GetField<ComboBox>("cbPattern");
            var chkAllParts = GetField<CheckBox>("chkAllParts");
            var checkBox1 = GetField<CheckBox>("checkBox1");

            var numStaff = GetField<NumericUpDown>("numStaff");
            var txtSections = GetField<TextBox>("txtSections");
            var numStartBar = GetField<NumericUpDown>("numStartBar");
            var numEndBar = GetField<NumericUpDown>("numEndBar");
            var numStartBeat = GetField<NumericUpDown>("numStartBeat");
            var numericUpDown2 = GetField<NumericUpDown>("numericUpDown2");

            var chkOverwrite = GetField<CheckBox>("chkOverwrite");

            var rbPitchAbsolute = GetField<RadioButton>("rbPitchAbsolute");
            var cbStep = GetField<ComboBox>("cbStep");
            var cbAccidental = GetField<ComboBox>("cbAccidental");
            var numOctaveAbs = GetField<NumericUpDown>("numOctaveAbs");
            var numDegree = GetField<NumericUpDown>("numDegree");
            var numOctaveKR = GetField<NumericUpDown>("numOctaveKR");

            var cbNoteValue = GetField<ComboBox>("cbNoteValue");
            var numDots = GetField<NumericUpDown>("numDots");
            var chkTupletEnabled = GetField<CheckBox>("chkTupletEnabled");
            var numTupletCount = GetField<NumericUpDown>("numTupletCount");
            var numTupletOf = GetField<NumericUpDown>("numTupletOf");
            var chkTieAcross = GetField<CheckBox>("chkTieAcross");
            var chkFermata = GetField<CheckBox>("chkFermata");
            var numNumberOfNotes = GetField<NumericUpDown>("numNumberOfNotes");

            var data = new GeneratorData
            {
                // Pattern
                Pattern = cbPattern?.SelectedItem?.ToString(),

                // New: store the full items -> checked state map
                PartsState = partsState,

                AllPartsChecked = chkAllParts?.Checked ?? false,
                AllStaffChecked = checkBox1?.Checked ?? false,

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
                Step = cbStep?.SelectedItem?.ToString(),
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

        // Apply a GenerateFormData DTO back to the private form controls.
        public void ApplyFormData(GeneratorForm form, GeneratorData data)
        {
            if (data == null || form == null) return;

            T GetField<T>(string name) where T : class
            {
                var t = form.GetType();
                while (t != null)
                {
                    var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (fi != null)
                        return fi.GetValue(form) as T;
                    t = t.BaseType;
                }
                return null;
            }

            // Pattern
            var cbPattern = GetField<ComboBox>("cbPattern");
            if (data.Pattern != null && cbPattern != null && cbPattern.Items.Contains(data.Pattern))
                cbPattern.SelectedItem = data.Pattern;

            // Parts - if provided, set checked state for matching items
            var clbParts = GetField<CheckedListBox>("clbParts");
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

            var chkAllParts = GetField<CheckBox>("chkAllParts");
            var checkBox1 = GetField<CheckBox>("checkBox1");

            if (data.AllPartsChecked.HasValue && chkAllParts != null)
                chkAllParts.Checked = data.AllPartsChecked.Value;

            if (data.AllStaffChecked.HasValue && checkBox1 != null)
                checkBox1.Checked = data.AllStaffChecked.Value;

            // Staff / sections / bars / beats
            var numStaff = GetField<NumericUpDown>("numStaff");
            var txtSections = GetField<TextBox>("txtSections");
            var numStartBar = GetField<NumericUpDown>("numStartBar");
            var numEndBar = GetField<NumericUpDown>("numEndBar");
            var numStartBeat = GetField<NumericUpDown>("numStartBeat");
            var numericUpDown2 = GetField<NumericUpDown>("numericUpDown2");
            var chkOverwrite = GetField<CheckBox>("chkOverwrite");

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
            var rbPitchAbsolute = GetField<RadioButton>("rbPitchAbsolute");
            var rbPitchKeyRelative = GetField<RadioButton>("rbPitchKeyRelative");
            var cbStep = GetField<ComboBox>("cbStep");
            var cbAccidental = GetField<ComboBox>("cbAccidental");
            var numOctaveAbs = GetField<NumericUpDown>("numOctaveAbs");
            var numDegree = GetField<NumericUpDown>("numDegree");
            var numOctaveKR = GetField<NumericUpDown>("numOctaveKR");

            if (data.PitchAbsolute.HasValue && rbPitchAbsolute != null && rbPitchKeyRelative != null)
            {
                rbPitchAbsolute.Checked = data.PitchAbsolute.Value;
                rbPitchKeyRelative.Checked = !data.PitchAbsolute.Value;
            }

            if (data.Step != null && cbStep != null && cbStep.Items.Contains(data.Step))
                cbStep.SelectedItem = data.Step;

            if (data.Accidental != null && cbAccidental != null && cbAccidental.Items.Contains(data.Accidental))
                cbAccidental.SelectedItem = data.Accidental;

            if (data.OctaveAbsolute.HasValue && numOctaveAbs != null)
                numOctaveAbs.Value = LimitRange(numOctaveAbs, data.OctaveAbsolute.Value);

            if (data.DegreeKeyRelative.HasValue && numDegree != null)
                numDegree.Value = LimitRange(numDegree, data.DegreeKeyRelative.Value);

            if (data.OctaveKeyRelative.HasValue && numOctaveKR != null)
                numOctaveKR.Value = LimitRange(numOctaveKR, data.OctaveKeyRelative.Value);

            // Rhythm
            var cbNoteValue = GetField<ComboBox>("cbNoteValue");
            var numDots = GetField<NumericUpDown>("numDots");
            var chkTupletEnabled = GetField<CheckBox>("chkTupletEnabled");
            var numTupletCount = GetField<NumericUpDown>("numTupletCount");
            var numTupletOf = GetField<NumericUpDown>("numTupletOf");
            var chkTieAcross = GetField<CheckBox>("chkTieAcross");
            var chkFermata = GetField<CheckBox>("chkFermata");
            var numNumberOfNotes = GetField<NumericUpDown>("numNumberOfNotes");

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