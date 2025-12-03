using Music.Domain;

namespace Music.Writer
{
    /// <summary>
    /// Manages adding phrases to the phrase DataGridView.
    /// </summary>
    internal static class PhraseGridManager
    {
        /// <summary>
        /// Adds a phrase to the phrase grid with default instrument and auto-incrementing phrase number.
        /// </summary>
        /// <param name="phrase">The phrase to add</param>
        /// <param name="midiInstruments">List of available MIDI instruments</param>
        /// <param name="dgvPhrase">The DataGridView to add to</param>
        /// <param name="phraseNumber">Reference to the phrase counter (will be incremented)</param>
        internal static void AddPhraseToGrid(
            Phrase phrase,
            List<MidiInstrument> midiInstruments,
            DataGridView dgvPhrase,
            ref int phraseNumber)
        {
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

            // Resolve MIDI program number from the instrument name
            // var instrument = MidiInstrument.GetGeneralMidiInstruments()
            //     .FirstOrDefault(i => i.Name.Equals(midiProgramName, StringComparison.OrdinalIgnoreCase));

            //MidiProgramNumber = instrument?.ProgramNumber ?? 0; // Default to 0 (Acoustic Grand Piano) if not found




            // Column 1: MIDI Instrument dropdown - set to first instrument (Acoustic Grand Piano) as default
            // User can change this by clicking the dropdown
            row.Cells["colInstrument"].Value = midiInstruments[0].ProgramNumber;

            // Column 2: Stave - adds +1 each time new row is added with same instrument as existing row(s)
            row.Cells["colStave"].Value = dgvPhrase.Rows
                .Cast<DataGridViewRow>()
                .Count(r => r.Cells["colInstrument"].Value?.Equals(midiInstruments[0].ProgramNumber) == true);

            // Column 3: Event number
            row.Cells["colEventNumber"].Value = phraseName;

            // Column 4: Description
            row.Cells["colDescription"].Value = $"Part: {partName}";

            // Column 5: Phrase details (placeholder)
            row.Cells["colPhrase"].Value = "tbd";
        }
    }
}