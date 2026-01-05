// AI: purpose=Handler to generate groove-synced test tracks and add them to SongContext and UI grid.
// AI: invariants=Uses SectionTrack.TotalBars to compute totalBars; mutates songContext.Song.PartTracks and Grid; callers expect these side-effects.
// AI: deps=Relies on Generator.Generator.Generate, SongGridManager.AddNewPartTrack, and GrooveTrack presets; changing generator API breaks this.
// AI: perf=Generation may allocate; run on UI thread currently; consider backgrounding if UI stalls for large songs.

using Music.Generator;

namespace Music.Writer
{
    // AI: Command handler for groove-driven generator test; wraps generator call and updates UI grid with results.
    public static class HandleCommandGrooveSyncTest
    {
        // AI: HandleGrooveSyncTest: rebuilds BarTrack from SectionTrack, runs generator, appends 4 PartTracks to Song and grid.
        // AI: errors=any exception is shown via ShowGrooveError; no retry or partial-commit logic.
        public static void HandleGrooveSyncTest(
            SongContext songContext,
            DataGridView dgSong)
        {
            try
            {
                // Ensure BarTrack is up-to-date before generating
                int totalBars = songContext.SectionTrack.TotalBars;
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

        // AI: ShowGrooveSuccess: message text should remain stable for tests that assert success dialogs.
        private static void ShowGrooveSuccess(int addedCount)
        {
            MessageBoxHelper.Show(
                $"Successfully created {addedCount} synchronized tracks using groove track with controlled randomness.",
                "Groove Sync Test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // AI: ShowGrooveError: shows exception message; avoid leaking sensitive info in production UI.
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