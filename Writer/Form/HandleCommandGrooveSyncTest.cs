using Music.Generator;

namespace Music.Writer
{
    /// <summary>
    /// Command handler for the groove-driven generator test.
    /// </summary>
    public static class HandleCommandGrooveSyncTest
    {
        /// <summary>
        /// Handles the Harmony Groove Sync Test command.
        /// Generates synchronized test tracks using groove presets from the GrooveTrack track.
        /// </summary>
        public static void HandleGrooveSyncTest(
            SongContext songContext,
            DataGridView dgSong)
        {
            try
            {
                // Ensure BarTrack is up-to-date before generating
                int totalBars = songContext.HarmonyTrack.Events.Max(e => e.StartBar);
                songContext.BarTrack.RebuildFromTimingTrack(songContext.Song.TimeSignatureTrack, totalBars);

                // Generate all song tracks using the GrooveTrack
                var result = Generator.Generator.Generate(songContext);
                songContext.Song.PartTracks.Add(result.BassTrack);
                songContext.Song.PartTracks.Add(result.GuitarTrack);
                songContext.Song.PartTracks.Add(result.KeysTrack);
                songContext.Song.PartTracks.Add(result.DrumTrack);

                // Update Grid with song tracks
                SongGridManager.AddNewPartTrack(result.BassTrack, dgSong);
                SongGridManager.AddNewPartTrack(result.GuitarTrack, dgSong);
                SongGridManager.AddNewPartTrack(result.KeysTrack, dgSong);
                SongGridManager.AddNewPartTrack(result.DrumTrack, dgSong);
                ShowGrooveSuccess(4);
            }
            catch (Exception ex)
            {
                ShowGrooveError(ex);
            }
        }

        #region MessageBox

        private static void ShowGrooveSuccess(int addedCount)
        {
            MessageBoxHelper.Show(
                $"Successfully created {addedCount} synchronized tracks using groove track with controlled randomness.",
                "Groove Sync Test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static void ShowGrooveError(Exception ex)
        {
            MessageBoxHelper.Show(
                $"Error generating groove tracks:\n{ex.Message}",
                "Generation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        #endregion
    }
}