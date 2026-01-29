// AI: purpose=Handler for groove preview audition; generates PartTrack from seed+genre and adds to UI grid.
// AI: invariants=Shows input dialog for parameters; uses BarTrack from songContext; mutates Song.PartTracks and Grid.
// AI: deps=Relies on Generator.GenerateGroovePreview, GroovePreviewDialog, SongGridManager.AddNewPartTrack.
// AI: change=Story 3.1: user enters seed/genre/bars, hears groove instance for audition workflow.

using Music.Generator;

namespace Music.Writer
{
    // AI: Command handler for groove preview; shows dialog, generates preview, updates grid, displays seed.
    public static class HandleCommandGrooveTest
    {
        // AI: HandleGrooveTest: shows input dialog, generates groove preview, adds to grid with seed in status.
        // AI: errors=Any exception shown via ShowError; no retry logic; invalid input caught by dialog validation.
        public static void HandleGrooveTest(
            SongContext songContext,
            DataGridView dgSong)
        {
            try
            {
                // Validate song context has required data
                if (songContext?.BarTrack == null)
                {
                    ShowError("Song context not initialized. Please set up timing track first.");
                    return;
                }

                // Show input dialog
                using var dialog = new GroovePreviewDialog();
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                // Capture parameters
                int seed = dialog.Seed;
                string genre = dialog.Genre;
                int bars = dialog.Bars;

                // Generate groove preview
                PartTrack groovePreview = Generator.Generator.GenerateGroovePreview(
                    seed,
                    genre,
                    songContext.BarTrack,
                    bars);

                // Set descriptive name with seed and mark as drum set for correct playback
                groovePreview.MidiProgramName = $"Groove Preview (Seed: {seed})";
                groovePreview.MidiProgramNumber = 255; // 255 = Drum Set sentinel in MidiVoices

                // Add to song context and grid
                songContext.Song.PartTracks.Add(groovePreview);
                SongGridManager.AddNewPartTrack(groovePreview, dgSong);

                ShowSuccess(seed, genre, bars);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #region MessageBox

        // AI: ShowSuccess: displays seed for reproduction; message stable for testing.
        private static void ShowSuccess(int seed, string genre, int bars)
        {
            MessageBoxHelper.Show(
                $"Groove preview created successfully.\n\n" +
                $"Genre: {genre}\n" +
                $"Seed: {seed}\n" +
                $"Bars: {bars}\n\n" +
                $"Use this seed to reproduce the same groove.",
                "Groove Preview",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // AI: ShowError: shows error message; overload for exception and string message.
        private static void ShowError(string message)
        {
            MessageBoxHelper.Show(
                $"Groove preview error:\n{message}",
                "Groove Preview Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        #endregion
    }
}
