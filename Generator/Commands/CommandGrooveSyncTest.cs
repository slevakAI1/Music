using System.Windows.Forms;
using Music.Designer;
using Music.MyMidi;
using Music.Writer.Generator.Randomization;

namespace Music.Writer.Generator
{
    /// <summary>
    /// Command handler for the groove-driven generator test.
    /// Replaces the fixed test generator with groove-based phrase generation.
    /// </summary>
    public static class CommandGrooveSyncTest
    {
        /// <summary>
        /// Handles the Harmony Groove Sync Test command.
        /// Generates synchronized phrases using a groove template.
        /// </summary>
        public static void HandleGrooveSyncTest(
            DataGridView dgSong,
            List<MidiInstrument> midiInstruments,
            ref int phraseNumber,
            Form owner)
        {
            // Extract harmony timeline from the fixed harmony row
            var harmonyRow = dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY];
            var harmonyTimeline = harmonyRow.Cells["colData"].Value as HarmonyTimeline;

            if (harmonyTimeline == null || harmonyTimeline.Events.Count == 0)
            {
                MessageBox.Show(owner,
                    "No harmony events defined. Please add harmony events first.",
                    "Missing Harmony",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Extract time signature timeline
            var timeSignatureRow = dgSong.Rows[SongGridManager.FIXED_ROW_TIME_SIGNATURE];
            var timeSignatureTimeline = timeSignatureRow.Cells["colData"].Value as TimeSignatureTimeline;

            if (timeSignatureTimeline == null || timeSignatureTimeline.Events.Count == 0)
            {
                MessageBox.Show(owner,
                    "No time signature events defined. Please add at least one time signature event.",
                    "Missing Time Signature",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Use the PopRockBasic groove preset from Designer
                var groovePreset = GroovePresets.GetPopRockBasic();

                // Generate all phrases using the Generate method (which includes drums)
                var result = GrooveDrivenGenerator.Generate(
                    harmonyTimeline,
                    timeSignatureTimeline,
                    groovePreset);

                int addedCount = 0;

                // Add generated phrases to grid
                if (result.BassPhrase != null && result.BassPhrase.PhraseNotes.Count > 0)
                {
                    SongGridManager.AddPhraseToGrid(result.BassPhrase, midiInstruments, dgSong, ref phraseNumber);
                    addedCount++;
                }

                if (result.GuitarPhrase != null && result.GuitarPhrase.PhraseNotes.Count > 0)
                {
                    SongGridManager.AddPhraseToGrid(result.GuitarPhrase, midiInstruments, dgSong, ref phraseNumber);
                    addedCount++;
                }

                if (result.KeysPhrase != null && result.KeysPhrase.PhraseNotes.Count > 0)
                {
                    SongGridManager.AddPhraseToGrid(result.KeysPhrase, midiInstruments, dgSong, ref phraseNumber);
                    addedCount++;
                }

                if (result.DrumPhrase != null && result.DrumPhrase.PhraseNotes.Count > 0)
                {
                    SongGridManager.AddPhraseToGrid(result.DrumPhrase, midiInstruments, dgSong, ref phraseNumber);
                    addedCount++;
                }

                MessageBox.Show(owner,
                    $"Successfully created {addedCount} synchronized phrases using '{groovePreset.Name}' groove with controlled randomness.",
                    "Groove Sync Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner,
                    $"Error generating groove phrases:\n{ex.Message}",
                    "Generation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}