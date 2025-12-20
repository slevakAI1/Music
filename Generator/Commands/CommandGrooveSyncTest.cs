using Music.Designer;
using Music.MyMidi;
using Music.Writer;
using Music.Writer.Generator;

namespace Music.Generator
{
    /// <summary>
    /// Command handler for the groove-driven generator test.
    /// Replaces the fixed test generator with groove-based song track generation.
    /// </summary>
    public static class CommandGrooveSyncTest
    {
        /// <summary>
        /// Handles the Harmony Groove Sync Test command.
        /// Generates synchronized test tracks using a groove preset
        /// </summary>
        public static void HandleGrooveSyncTest(
            DataGridView dgSong,
            List<MidiVoices> midiInstruments,
            ref int songTrackNumber)
        {
            // Extract harmony timeline from the fixed harmony row
            var harmonyRow = dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY];
            var harmonyTimeline = harmonyRow.Cells["colData"].Value as HarmonyTimeline;

            if (harmonyTimeline == null || harmonyTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show(
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
                MessageBoxHelper.Show(
                    "No time signature events defined. Please add at least one time signature event.",
                    "Missing Time Signature",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Use a groove preset from Designer
                //var groovePreset = GroovePresets.GetPopRockBasic();
                var groovePreset = GroovePresets.GetRapBasic();

                // Generate all song tracks using the Generate method (which includes drums)
                var result = GrooveDrivenGenerator.Generate(
                    harmonyTimeline,
                    timeSignatureTimeline,
                    groovePreset);

                int addedCount = 0;

                // Add generated song tracks to grid
                if (result.BassTrack != null && result.BassTrack.SongTrackNoteEvents.Count > 0)
                {
                    SongGridManager.AddSongTrackToGrid(result.BassTrack, midiInstruments, dgSong, ref songTrackNumber);
                    addedCount++;
                }

                if (result.GuitarTrack != null && result.GuitarTrack.SongTrackNoteEvents.Count > 0)
                {
                    SongGridManager.AddSongTrackToGrid(result.GuitarTrack, midiInstruments, dgSong, ref songTrackNumber);
                    addedCount++;
                }

                if (result.KeysTrack != null && result.KeysTrack.SongTrackNoteEvents.Count > 0)
                {
                    SongGridManager.AddSongTrackToGrid(result.KeysTrack, midiInstruments, dgSong, ref songTrackNumber);
                    addedCount++;
                }

                if (result.DrumTrack != null && result.DrumTrack.SongTrackNoteEvents.Count > 0)
                {
                    SongGridManager.AddSongTrackToGrid(result.DrumTrack, midiInstruments, dgSong, ref songTrackNumber);
                    addedCount++;
                }

                MessageBoxHelper.Show(
                    $"Successfully created {addedCount} synchronized tracks using '{groovePreset.Name}' groove with controlled randomness.",
                    "Groove Sync Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show(
                    $"Error generating groove tracks:\n{ex.Message}",
                    "Generation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}