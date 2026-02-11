// AI: purpose=Generate a groove PartTrack for audition and append it to the current Song and UI grid.
// AI: invariants=Requires songContext.BarTrack; dialog validates seed/genre/bars; operation is deterministic by seed.
// AI: deps=Generator.SongGenerator.GenerateGroovePreview; CreateDrumPhraseSettingsDialog; SongGridManager.AddNewPartTrack

using Music.Generator;

namespace Music.Writer
{
    // AI: purpose=Handle UI command to generate groove preview and update Song+DataGridView.
    public static class HandleCommandGrooveTest
    {
        // AI: entry=Validate SongContext; show modal settings; generate deterministic groove PartTrack from phrases.
        // AI: effects=Appends one PartTrack to songContext.Song.PartTracks and adds row via SongGridManager.
        // AI: errors=All exceptions shown via ShowError; dialog validation prevents bad parameters.
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
                using var dialog = new CreateDrumPhraseSettingsDialog();
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                // Capture parameters
                int seed = dialog.Seed;
                string genre = dialog.Genre;
                int bars = dialog.Bars;

                // Generate groove preview
                PartTrack groovePreview = Generator.SongGenerator.GenerateGroovePreview(
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

        // AI: purpose=Inform user of successful generation and provide seed to reproduce the groove.
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

        // AI: purpose=Present generation errors to the user via MessageBox; keep messages safe for UI.
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
