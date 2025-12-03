using Music.Domain;
using Music.Tests;

namespace Music.Writer
{
    // Helper class extracted from WriterForm for non-event, non-lifecycle logic.
    internal static class TransformRepeatingNotes
    {
        /// <summary>
        /// Adds a set of repeating note, chords or rests to the phrase grid.
        /// (Moved from WriterForm.ExecuteCommandWriteRepeatingNotes)
        /// </summary>
        internal static void ExecuteCommandWriteRepeatingNotes(
            WriterFormData writer,
            List<MidiInstrument> midiInstruments,
            DataGridView dgvPhrase,
            ref int phraseNumber)
        {
            // Get the params from Writer
            var numberOfNotes = writer.NumberOfNotes ?? 1;
            var midiProgramName = "Acoustic Grand Piano"; // Default instrument

            // Create the repeating phrase - this is a transform!
            var phrase = WriterFormDataToPhrase.Convert(
                writer,
                numberOfNotes,
                midiProgramName);

            // Add the phrase to the grid (just appends a new row for now
            // Set phrase name/number
            phraseNumber++;
            var phraseName = phraseNumber.ToString();

            // Get part name from the phrase
            var partName = phrase.MidiProgramName ?? "Unknown";

            // Add new row
            int newRowIndex = dgvPhrase.Rows.Add();
            var row = dgvPhrase.Rows[newRowIndex];

            // Column 0: Hidden data (Phrase object)
            row.Cells["colData"].Value = phrase;

            // Column 1: MIDI Instrument dropdown - set to first instrument (Acoustic Grand Piano) as default
            // User can change this by clicking the dropdown
            row.Cells["colInstrument"].Value = midiInstruments[0].ProgramNumber;

            // Column 2: Stave - adds +1 each time new row is added with same instrument as existing row(s)
            row.Cells["colStave"].Value = dgvPhrase.Rows.Cast<DataGridViewRow>().Count(r => r.Cells["colInstrument"].Value?.Equals(midiInstruments[0].ProgramNumber) == true);

            // Column 3: Event number
            row.Cells["colEventNumber"].Value = phraseName;

            // Column 4: Description
            row.Cells["colDescription"].Value = $"Part: {partName}";

            // Column 5: Phrase details (placeholder)
            row.Cells["colPhrase"].Value = "tbd";
        }
    }
}