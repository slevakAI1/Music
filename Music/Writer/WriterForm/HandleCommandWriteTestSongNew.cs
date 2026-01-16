// AI: purpose=Handler to generate groove-synced test tracks and add them to SongContext and UI grid.
// AI: invariants=Uses SectionTrack.TotalBars to compute totalBars; mutates songContext.Song.PartTracks and Grid; callers expect these side-effects.
// AI: deps=Relies on Generator.Generator.Generate, SongGridManager.AddNewPartTrack, and GrooveTrack presets; changing generator API breaks this.
// AI: perf=Generation may allocate; run on UI thread currently; consider backgrounding if UI stalls for large songs.

using Music.Generator;

namespace Music.Writer
{
    // AI: Command handler for groove-driven generator test; wraps generator call and updates UI grid with results.
    public static class HandleCommandWriteTestSongNew
    {
        // AI: HandleCommandWriteTestSong: runs generator, appends 4 PartTracks to Song and grid.
        // AI: errors=any exception is shown via ShowError; no retry or partial-commit logic.
        public static void HandleWriteTestSong(
            SongContext songContext,
            DataGridView dgSong)
        {
            try
            {
                // Generate all song tracks 
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
                ShowSuccess(4);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #region MessageBox

        // AI: ShowSuccess: message text should remain stable for tests that assert success dialogs.
        private static void ShowSuccess(int addedCount)
        {
            MessageBoxHelper.Show(
                $"Successfully created {addedCount} tracks.",
                "Write Test Song",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // AI: ShowError: shows exception message; avoid leaking sensitive info in production UI.
        private static void ShowError(Exception ex)
        {
            MessageBoxHelper.Show(
                $"Generator error:\n{ex.Message}",
                "Generation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        #endregion
    }
}