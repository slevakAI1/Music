using System;
using System.Drawing.Imaging;
using System.Linq;
using static Music.Helpers;

namespace Music.Writer
{
    // Converted helper into a partial class so it can access designer controls directly
    public partial class ConsoleForm
    {
        // Capture current control values into a class object.
        // No form parameter required because this is now a partial of Writer.
        public ConsoleData CaptureFormData()
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

            // Capture sections items and their checked state into a dictionary
            var sectionsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (clbSections != null)
            {
                for (int i = 0; i < clbSections.Items.Count; i++)
                {
                    var name = clbSections.Items[i]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                        sectionsState[name] = clbSections.GetItemChecked(i);
                }
            }

            // Capture staffs checked state into a list
            var selectedStaffs = new List<int>();
            if (clbStaffs != null)
            {
                for (int i = 0; i < clbStaffs.Items.Count; i++)
                {
                    if (clbStaffs.GetItemChecked(i))
                    {
                        var staffText = clbStaffs.Items[i]?.ToString() ?? string.Empty;
                        if (int.TryParse(staffText, out int staffNum))
                            selectedStaffs.Add(staffNum);
                    }
                }
            }

            // Determine step selection and Rest handling
            string? stepSelected = cbStep?.SelectedItem?.ToString();
            // Assume IsRest radiobutton exists; read its checked state directly.
            var isRest = rbIsRest.Checked;
            // Capture chord radio button state
            var isChord = rbChord.Checked;

            // Convert step string to char (use '\0' for Rest)
            char stepChar = '\0';
            if (!isRest && !string.IsNullOrWhiteSpace(stepSelected))
            {
                stepChar = stepSelected[0];
            }

            // Capture tuplet number from textbox; map empty string to null
            string? tupletNumber = txtTupletNumber?.Text;
            if (string.IsNullOrWhiteSpace(tupletNumber))
                tupletNumber = null;

            var data = new ConsoleData
            {
                // Pattern
                Pattern = cbPattern?.SelectedItem?.ToString(),

                // New: store the full items -> checked state map
                PartsState = partsState,
                SectionsState = sectionsState,

                // Staff / sections / bars / beats
                SelectedStaffs = selectedStaffs,
                StartBar = (int?)(numStartBar?.Value ?? 1),
                EndBar = 48,    //  This is deprecated; kept for backward compatibility
                StartBeat = (int?)(numStartBeat?.Value ?? 1),

                // Pitch
                PitchAbsolute = rbPitchAbsolute?.Checked ?? true,
                // Step is now char type; '\0' when Rest is selected
                Step = stepChar,
                IsRest = isRest,
                IsChord = isChord,
                Accidental = cbAccidental?.SelectedItem?.ToString(),
                OctaveAbsolute = (int?)(numOctaveAbs?.Value ?? 4),
                DegreeKeyRelative = (int?)(numDegree?.Value ?? 0),

                // Chord
                ChordKey = cbChordKey?.SelectedItem?.ToString(),
                ChordDegree = (int?)(numChordDegree?.Value ?? 1),
                ChordQuality = cbChordQuality?.SelectedItem?.ToString(),
                ChordBase = cbChordBase?.SelectedItem?.ToString(),

                // Rhythm
                NoteValue = cbNoteValue?.SelectedItem?.ToString(),
                Dots = (int)numDots.Value,
                TupletNumber = tupletNumber,
                TupletCount = (int?)(numTupletCount?.Value ?? 0),
                TupletOf = (int?)(numTupletOf?.Value ?? 0),
                NumberOfNotes = (int?)(numNumberOfNotes?.Value ?? 1)
            };

            return data;
        }

        // Apply a WriterData object back to the private form controls.
        // No form parameter required because this is a partial of Writer.
        public void ApplyFormData(ConsoleData data)
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

            // Sections - if provided, set checked state for matching items
            if (data.SectionsState != null && data.SectionsState.Count > 0 && clbSections != null)
            {
                // Use case-insensitive lookup
                var map = new Dictionary<string, bool>(data.SectionsState, StringComparer.OrdinalIgnoreCase);

                // Clear existing items and populate from the map so control contains exactly the map entries
                clbSections.BeginUpdate();
                try
                {
                    clbSections.Items.Clear();
                    foreach (var kv in map)
                    {
                        var name = kv.Key ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        var idx = clbSections.Items.Add(name);
                        clbSections.SetItemChecked(idx, kv.Value);
                    }
                }
                finally
                {
                    clbSections.EndUpdate();
                }
            }

            // Staffs - set checked state for matching staff numbers
            if (data.SelectedStaffs != null && data.SelectedStaffs.Count > 0 && clbStaffs != null)
            {
                clbStaffs.BeginUpdate();
                try
                {
                    // First, uncheck all items
                    for (int i = 0; i < clbStaffs.Items.Count; i++)
                    {
                        clbStaffs.SetItemChecked(i, false);
                    }

                    // Then check items that match selected staffs
                    for (int i = 0; i < clbStaffs.Items.Count; i++)
                    {
                        var staffText = clbStaffs.Items[i]?.ToString() ?? string.Empty;
                        if (int.TryParse(staffText, out int staffNum))
                        {
                            if (data.SelectedStaffs.Contains(staffNum))
                                clbStaffs.SetItemChecked(i, true);
                        }
                    }
                }
                finally
                {
                    clbStaffs.EndUpdate();
                }
            }

            // Staff / sections / bars / beats
            if (data.StartBar.HasValue && numStartBar != null)
                numStartBar.Value = LimitRange(numStartBar, data.StartBar.Value);

            if (data.StartBeat.HasValue && numStartBeat != null)
                numStartBeat.Value = LimitRange(numStartBeat, data.StartBeat.Value);

            // Pitch
            if (data.PitchAbsolute.HasValue && rbPitchAbsolute != null && rbPitchKeyRelative != null)
            {
                rbPitchAbsolute.Checked = data.PitchAbsolute.Value;
                rbPitchKeyRelative.Checked = !data.PitchAbsolute.Value;
            }

            // Set IsRest radiobutton state from data
            if (data.IsRest.HasValue)
                rbIsRest.Checked = data.IsRest.Value;

            // Restore chord radiobutton state from data
            if (data.IsChord.HasValue)
                rbChord.Checked = data.IsChord.Value;

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

            // Rhythm
            if (data.NoteValue != null && cbNoteValue != null && cbNoteValue.Items.Contains(data.NoteValue))
                cbNoteValue.SelectedItem = data.NoteValue;

            numDots.Value = LimitRange(numDots, data.Dots);

            if (data.TupletNumber != null && txtTupletNumber != null)
                txtTupletNumber.Text = data.TupletNumber;

            if (data.TupletCount.HasValue && numTupletCount != null)
                numTupletCount.Value = LimitRange(numTupletCount, data.TupletCount.Value);

            if (data.TupletOf.HasValue && numTupletOf != null)
                numTupletOf.Value = LimitRange(numTupletOf, data.TupletOf.Value);


            if (data.NumberOfNotes.HasValue && numNumberOfNotes != null)
                numNumberOfNotes.Value = LimitRange(numNumberOfNotes, data.NumberOfNotes.Value);

            // Chord
            if (data.ChordKey != null && cbChordKey != null && cbChordKey.Items.Contains(data.ChordKey))
                cbChordKey.SelectedItem = data.ChordKey;

            if (data.ChordDegree.HasValue && numChordDegree != null)
                numChordDegree.Value = LimitRange(numChordDegree, data.ChordDegree.Value);

            if (data.ChordQuality != null && cbChordQuality != null && cbChordQuality.Items.Contains(data.ChordQuality))
                cbChordQuality.SelectedItem = data.ChordQuality;

            if (data.ChordBase != null && cbChordBase != null && cbChordBase.Items.Contains(data.ChordBase))
                cbChordBase.SelectedItem = data.ChordBase;
        }
    }
}