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
        /// Generates synchronized test tracks using groove presets from the GrooveTrack timeline.
        /// SongTrackNumber is the next open/unused midi track number
        /// </summary>
        public static void HandleGrooveSyncTest(
            SongContext songContext,
            DataGridView dgSong,
            ref int songTrackNumber)
        {
            // Extract harmony timeline from the fixed harmony row
            var harmonyRow = dgSong.Rows[SongGridManager.FIXED_ROW_HARMONY];
            var harmonyTimeline = harmonyRow.Cells["colData"].Value as HarmonyTrack;

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
            var timeSignatureTimeline = timeSignatureRow.Cells["colData"].Value as TimeSignatureTrack;

            if (timeSignatureTimeline == null || timeSignatureTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show(
                    "No time signature events defined. Please add at least one time signature event.",
                    "Missing Time Signature",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Get the GrooveTrack from the designer
            var grooveTrack = Globals.SongContext?.GrooveTrack;

            if (grooveTrack == null || grooveTrack.Events.Count == 0)
            {
                MessageBoxHelper.Show(
                    "No groove events defined. Please add at least one groove event first.",
                    "Missing Groove",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Generate all song tracks using the GrooveTrack timeline
                var result = GrooveDrivenGenerator.Generate(
                    harmonyTimeline,
                    timeSignatureTimeline,
                    grooveTrack);

                int addedCount = 0;



                // TO DO NEXT!  This needs to write to the Song and the Song needs to write to the grid!







                // Add generated song tracks to grid
                if (result.BassTrack != null && result.BassTrack.SongTrackNoteEvents.Count > 0)
                {
                    SongGridManager.AddSongTrackToGrid(result.BassTrack, dgSong, ref songTrackNumber);
                    addedCount++;
                }

                if (result.GuitarTrack != null && result.GuitarTrack.SongTrackNoteEvents.Count > 0)
                {
                    SongGridManager.AddSongTrackToGrid(result.GuitarTrack, dgSong, ref songTrackNumber);
                    addedCount++;
                }

                if (result.KeysTrack != null && result.KeysTrack.SongTrackNoteEvents.Count > 0)
                {
                    SongGridManager.AddSongTrackToGrid(result.KeysTrack, dgSong, ref songTrackNumber);
                    addedCount++;
                }

                if (result.DrumTrack != null && result.DrumTrack.SongTrackNoteEvents.Count > 0)
                {
                    SongGridManager.AddSongTrackToGrid(result.DrumTrack, dgSong, ref songTrackNumber);
                    addedCount++;
                }










                MessageBoxHelper.Show(
                    $"Successfully created {addedCount} synchronized tracks using groove timeline with controlled randomness.",
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