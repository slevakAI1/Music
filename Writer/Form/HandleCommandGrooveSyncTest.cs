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
        /// Generates synchronized test tracks using groove presets from the GrooveTrack timeline.
        /// </summary>
        public static void HandleGrooveSyncTest(
            SongContext songContext,
            DataGridView dgSong)
        {
            try
            {
                // Generate all song tracks using the GrooveTrack
                var result = GrooveDrivenGenerator.Generate(songContext);
                songContext.Song.PartTracks.Add(result.BassTrack);
                songContext.Song.PartTracks.Add(result.GuitarTrack);
                songContext.Song.PartTracks.Add(result.KeysTrack);
                songContext.Song.PartTracks.Add(result.DrumTrack);

                // Update Grid with song tracks
                SongGridManager.AddNewTrack(result.BassTrack, dgSong);
                SongGridManager.AddNewTrack(result.GuitarTrack, dgSong);
                SongGridManager.AddNewTrack(result.KeysTrack, dgSong);
                SongGridManager.AddNewTrack(result.DrumTrack, dgSong);
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
                $"Successfully created {addedCount} synchronized tracks using groove timeline with controlled randomness.",
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