// AI: purpose=Map between WriterForm controls and WriterFormData DTO; capture/apply UI state consistently.
// AI: invariants=Capture/Apply must remain symmetrical; control names and expected item values are stable contracts.
// AI: deps=Called by WriterForm to persist UI prefs; changing properties requires updating WriterForm and tests.
// AI: change=If adding new controls, update CaptureFormData, ApplyFormData, WriterFormData, and tests in tandem.

namespace Music.Writer
{
    // AI: Pure transform class: keep UI logic here, avoid business logic; methods accept control refs to aid unit testing.
    public class WriterFormTransform
    {
        // AI: CaptureFormData: read control values into a WriterFormData; null-safe for optional controls, uses defaults.
        public WriterFormData CaptureFormData(
            ComboBox? cbCommand,
            CheckedListBox? clbParts,
            CheckedListBox? clbStaffs,
            RadioButton rbChord,
            ComboBox? cbStep,
            RadioButton? rbPitchAbsolute,
            RadioButton? rbPitchKeyRelative,
            ComboBox? cbAccidental,
            NumericUpDown? numOctaveAbs,
            NumericUpDown? numDegree,
            ComboBox? cbChordKey,
            NumericUpDown? numChordDegree,
            ComboBox? cbChordQuality,
            ComboBox? cbChordBase,
            ComboBox? cbNoteValue,
            NumericUpDown numDots,
            TextBox? txtTupletNumber,
            NumericUpDown? numTupletCount,
            NumericUpDown? numTupletOf,
            NumericUpDown? numNumberOfNotes)
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

            // Determine step selection
            string? stepSelected = cbStep?.SelectedItem?.ToString();
            // Capture chord radio button state
            var isChord = rbChord.Checked;

            // TO DO - LOW - stepchar = " " at program startup. is there a better way?
            // I think this whole class needs to go.

            // Execute step string to char (use '\0' for Rest)
            char stepChar = ' ';
            if (!string.IsNullOrWhiteSpace(stepSelected))
                stepChar = stepSelected[0];

            // Capture tuplet number from textbox; map empty string to null
            string? tupletNumber = txtTupletNumber?.Text;
            if (string.IsNullOrWhiteSpace(tupletNumber))
                tupletNumber = null;

            var data = new WriterFormData
            {
                // ProposedPattern
                Pattern = cbCommand?.SelectedItem?.ToString(),

                // New: store the full items -> checked state map
                PartsState = partsState,

                // Staff / sections / bars / beats
                SelectedStaffs = selectedStaffs,
                EndBar = 48,    //  This is deprecated; kept for backward compatibility

                // Pitch
                PitchAbsolute = rbPitchAbsolute?.Checked ?? true,
                // Step is now char type; '\0' when Rest is selected
                Step = stepChar,
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

        // AI: ApplyFormData: sets control states from WriterFormData; operations are idempotent and safe to call repeatedly.
        // AI: note=Clears and repopulates clbParts to match data.PartsState exactly; preserves numeric control bounds via LimitRange.
        public void ApplyFormData(
            WriterFormData? data,
            ComboBox? cbCommand,
            CheckedListBox? clbParts,
            CheckedListBox? clbStaffs,
            RadioButton rbChord,
            ComboBox? cbStep,
            RadioButton? rbPitchAbsolute,
            RadioButton? rbPitchKeyRelative,
            ComboBox? cbAccidental,
            NumericUpDown? numOctaveAbs,
            NumericUpDown? numDegree,
            ComboBox? cbChordKey,
            NumericUpDown? numChordDegree,
            ComboBox? cbChordQuality,
            ComboBox? cbChordBase,
            ComboBox? cbNoteValue,
            NumericUpDown numDots,
            TextBox? txtTupletNumber,
            NumericUpDown? numTupletCount,
            NumericUpDown? numTupletOf,
            NumericUpDown? numNumberOfNotes)
        {
            if (data == null) return;

            // ProposedPattern
            if (data.Pattern != null && cbCommand != null && cbCommand.Items.Contains(data.Pattern))
                cbCommand.SelectedItem = data.Pattern;

            // Voices - if provided, set checked state for matching items
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

            // Pitch
            if (data.PitchAbsolute.HasValue && rbPitchAbsolute != null && rbPitchKeyRelative != null)
            {
                rbPitchAbsolute.Checked = data.PitchAbsolute.Value;
                rbPitchKeyRelative.Checked = !data.PitchAbsolute.Value;
            }

            // Restore chord radiobutton state from data
            if (data.IsChord.HasValue)
                rbChord.Checked = data.IsChord.Value;
            //  convert the char Step to string for selection.

            string stepString = data.Step.ToString();
            if (cbStep.Items.Contains(stepString))
                cbStep.SelectedItem = stepString;

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
        // Forces the value for a numeric up-down control to an integer within its min/max range.
        // AI: LimitRange clamps integer value into control bounds and returns decimal for assignment to NumericUpDown.Value.
        public static decimal LimitRange(NumericUpDown control, int value)
        {
            var min = (int)control.Minimum;
            var max = (int)control.Maximum;
            return (decimal)Math.Max(min, Math.Min(max, value));
        }
    }
}