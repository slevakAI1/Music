using Music.Designer;
using Music.Domain;
using MusicXml;
using MusicXml.Domain;

namespace Music.Writer
{
    // Event handler logic extracted from WriterForm into a partial class
    public partial class WriterForm
    {
        // This plays all of the selected phrases simulaneously as a midi document
        public async Task HandlePlayAsync()
        {
            // Check if there are any rows in the grid
            if (dgvPhrase.Rows.Count == 0)
            {
                MessageBox.Show(this, "No pitch events to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if a row is selected
            if (dgvPhrase.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Please select a pitch event to play.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build list of Phrase from all selected rows
            var phrases = new List<Phrase>();
            foreach (DataGridViewRow selectedRow in dgvPhrase.SelectedRows)
            {
                // Validate instrument cell value first (may be DBNull or null)
                var instrObj = selectedRow.Cells["colInstrument"].Value;
                int programNumber = programNumber = Convert.ToInt32(instrObj);
                if (programNumber == -1)  // -1 = placeholder "Select..." -> treat as missing selection
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBox.Show(
                        this,
                        $"No instrument selected for row #{eventNumber}. Please select an instrument before playing.",
                        "Missing Instrument",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort playback
                }

                // Validate phrase data exists in hidden data column before using it
                var phrase = (Phrase)selectedRow.Cells["colData"].Value;
                if (phrase.PhraseNotes.Count == 0)
                {
                    var eventNumber = selectedRow.Cells["colEventNumber"].Value?.ToString() ?? (selectedRow.Index + 1).ToString();
                    MessageBox.Show(
                        this,
                        $"No phrase data for row #{eventNumber}. Please add or assign a phrase before playing.",
                        "Missing Phrase",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return; // Abort playback
                }

                // Valid program number (0-127 or 255 for drums) — safe to cast now
                phrase.MidiProgramNumber = (byte)programNumber;
                phrases.Add(phrase);
            }

            try
            {
                // Step 1 - convert phrases to MIDI EVENTS - Absolute positions
                var midiEventLists = ConvertPhrasesToMidiEvents.Convert(phrases);
                var inputjson = Helpers.DebugObject(phrases);
                var outputjson = Helpers.DebugObject(midiEventLists);

                //  Step 2:
                // Merge midiEventLists lists that are for the same instrument
                // and adds global events

                //Assign track numbers to midi event lists
                var mergedMidiEventLists = MergeMidiEventsByInstrument.Convert(
                    midiEventLists,
                    tempo: 112,
                    timeSignatureNumerator: 4,
                    timeSignatureDenominator: 4);
                inputjson = Helpers.DebugObject(midiEventLists);
                outputjson = Helpers.DebugObject(mergedMidiEventLists);

                // Step 3 - Execute merged timed notes to MIDI document
                var midiDoc = ConvertMidiEventsToMidiDocument.Convert(
                    mergedMidiEventLists,
                    tempo: 112,
                    timeSignatureNumerator: 4,
                    timeSignatureDenominator: 4);

                await Player.PlayMidiFromPhrasesAsync(_midiPlaybackService, midiDoc, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error playing MIDI: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void HandleUpdateFormFromDesigner()
        {
            // Update the form to take into account any changes to Designer
            Globals.Writer?.UpdateFromDesigner(_designer);
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);

            // Technical this can run upon activation too, but only in initialize phase, just that one time
        }

        //                      T E S T   S C E N A R I O   B U T T O N S

        // This sets design test scenario D1
        public void HandleSetDesignTestScenarioD1()
        {
            _designer ??= new Designer.Designer();
            DesignerTests.SetTestDesignD1(_designer);
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(_designer);
            // KEEP. MessageBox.Show("Test Design D1 has been applied to the current design.", "Design Test Scenario D1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // This sets writer test scenario G1
        // Description: Set writer test values using the current design 
        public void HandleSetWriterTestScenarioG1()
        {
            _writer = WriterFormTests.SetTestWriterG1(_designer);
            ApplyFormData(_writer);
            // KEEP. MessageBox.Show("Test Writer G1 has been applied to the current generator settings.", "Generator Test Scenario G1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void HandleChordTest()
        {
            if (_designer?.HarmonicTimeline == null || _designer.HarmonicTimeline.Events.Count == 0)
            {
                MessageBox.Show(this,
                    "No harmonic events available in the current design.",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var harmonicEvent = _designer.HarmonicTimeline.Events[1];

            List<PhraseNote> notes;
            try
            {
                notes = ChordConverter.Convert(
                    harmonicEvent.Key,
                    harmonicEvent.Degree,
                    harmonicEvent.Quality,
                    harmonicEvent.Bass,
                    baseOctave: 4);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Failed to build chord: {ex.Message}",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (notes == null || notes.Count == 0)
            {
                MessageBox.Show(this,
                    "Chord conversion returned no notes.",
                    "Chord Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var lines = new List<string>();
            foreach (var note in notes)
            {
                var accidental = note.Alter switch
                {
                    1 => "#",
                    -1 => "b",
                    _ => ""
                };
                lines.Add($"{note.Step}{accidental} {note.Octave}");
            }

            var title = $"Chord: {harmonicEvent.Key} (Deg {harmonicEvent.Degree}, {harmonicEvent.Quality})";
            MessageBox.Show(this,
                string.Join(Environment.NewLine, lines),
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        public void HandleExportToNotion()
        {
            // Ensure score list exists and has at least one score
            if (_scoreList == null || _scoreList.Count == 0)
            {
                MessageBox.Show(this, "No score to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var path = Path.Combine("..", "..", "..", "Files", "NotionExchange", "Score.musicxml");
                var fullPath = Path.GetFullPath(path);
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var xml = MusicXmlScoreSerializer.Serialize(_scoreList[0]);
                File.WriteAllText(fullPath, xml, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                MessageBox.Show(this, $"Exported to {fullPath}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error exporting MusicXML:\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void HandleClearPhrases()
        {
            dgvPhrase.Rows.Clear();
        }

        // Branch on Command
        public void HandleExecute()
        {
            var pattern = cbCommand?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(pattern))
                return;

            switch (pattern)
            {
                case "Repeat Note":
                    // Check if any rows are selected
                    if (dgvPhrase.SelectedRows.Count == 0)
                    {
                        MessageBox.Show(this, "Please select one or more rows to apply this command.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var formData = CaptureFormData();
                    var (noteNumber, noteDurationTicks, repeatCount, isRest) =
                        GetRepeatingNotesParameters(formData);

                    var phrase = CreateRepeatingNotes.Execute(
                        noteNumber: noteNumber,
                        noteDurationTicks: noteDurationTicks,
                        repeatCount: repeatCount,
                        noteOnVelocity: 100,
                        isRest: isRest);

                    // Write the phrase object to colData (cell[0]) of each selected row
                    foreach (DataGridViewRow selectedRow in dgvPhrase.SelectedRows)
                    {
                        selectedRow.Cells["colData"].Value = phrase;
                        selectedRow.Cells["colPhrase"].Value = "Contains Phrase Data";
                    }
                    break;

                // Add additional cases for other patterns as needed
                default:
                    // No-op for unrecognized patterns
                    break;
            }
        }


        // This returns all 4 parameters
        private static (int noteNumber, int noteDurationTicks, int repeatCount, bool isRest)
            GetRepeatingNotesParameters(WriterFormData formData)
        {
            // Extract repeat count
            var repeatCount = formData.NumberOfNotes ?? 1;

            // Extract rest flag
            var isRest = formData.IsRest ?? false;

            // Calculate MIDI note number from step, accidental, and octave
            var step = formData.Step;
            var octave = formData.OctaveAbsolute ?? 4;
            var accidental = formData.Accidental;

            // Convert accidental string to alter value
            int alter = accidental switch
            {
                "Sharp" or "#" => 1,
                "Flat" or "b" => -1,
                "Natural" => 0,
                _ => 0
            };

            // Calculate MIDI note number
            int baseNote = step switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => 0
            };
            int noteNumber = (octave + 1) * 12 + baseNote + alter;

            // Calculate note duration in ticks
            var noteValue = formData.NoteValue;
            var dots = formData.Dots;
            var tupletNumber = formData.TupletNumber;
            var tupletCount = formData.TupletCount ?? 0;
            var tupletOf = formData.TupletOf ?? 0;

            const int ticksPerQuarterNote = 480;
            int duration = int.TryParse(noteValue, out int d) ? d : 4;

            // Base ticks for this duration (e.g., quarter=480, eighth=240)
            int baseTicks = (ticksPerQuarterNote * 4) / duration;

            // Apply dots (each dot adds half of the previous value)
            int dottedTicks = baseTicks;
            int addedValue = baseTicks / 2;
            for (int i = 0; i < dots; i++)
            {
                dottedTicks += addedValue;
                addedValue /= 2;
            }

            // Apply tuplet if specified
            int noteDurationTicks = dottedTicks;
            if (!string.IsNullOrWhiteSpace(tupletNumber) && tupletCount > 0 && tupletOf > 0)
            {
                // Tuplet adjusts duration: e.g., triplet = 2/3 of normal duration
                noteDurationTicks = (dottedTicks * tupletOf) / tupletCount;
            }

            return (noteNumber, noteDurationTicks, repeatCount, isRest);
        }
    }
}