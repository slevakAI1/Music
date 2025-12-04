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
            var partName = phrase.MidiProgramName;

            // Add new row
            int newRowIndex = dgvPhrase.Rows.Add();
            var row = dgvPhrase.Rows[newRowIndex];

            // Column 0: Hidden data (Phrase object)
            row.Cells["colData"].Value = phrase;

            // Column 1: MIDI Instrument dropdown - leave unselected (null/DBNull)
            // User can select an instrument or leave it unselected
            row.Cells["colInstrument"].Value = DBNull.Value;

            // Column 2: Stave - default to 1 for newly added rows
            row.Cells["colStave"].Value = 1;

            // TODO fix this name!
            // Column 3: Event number
            row.Cells["colEventNumber"].Value = phraseName;

            // Column 4: Description
            if (partName != "Select...")
                row.Cells["colDescription"].Value = $"Part: {partName}";

            // Column 5: Phrase details (placeholder)
            row.Cells["colPhrase"].Value = "";
        }
    }
}