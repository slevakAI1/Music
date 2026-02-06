// AI: purpose=Manual test handler: build a phrase-based drum PartTrack and append to current Song.
// AI: invariants=MaterialBank contains at least one phrase; BarTrack initialized before call.
// AI: deps=Generator.SongGenerator.GenerateFromPhrases; DrumTrackTestSettingsDialog; Rng.Initialize; SongGridManager

using Music.Generator;

namespace Music.Writer
{
    // AI: purpose=Handle UI command to generate phrase-based drum track and update Song+grid.
    public static class HandleCommandDrumTrackTest
    {
        // AI: entry=Validates SongContext and MaterialBank; shows seed dialog; generates deterministic drum PartTrack.
        // AI: effects=Adds one drum PartTrack to songContext.Song.PartTracks and updates DataGridView via SongGridManager.
        // AI: errors=All exceptions are presented via ShowError; dialog validation prevents invalid seed input.
        public static void HandleDrumTrackTest(
            SongContext songContext,
            DataGridView dgSong)
        {
            try
            {
                if (songContext?.BarTrack == null)
                {
                    ShowError("Song context not initialized. Please set up timing track first.");
                    return;
                }

                if (songContext.MaterialBank.GetPhrases().Count == 0)
                {
                    ShowError("No phrases found in MaterialBank. Generate and save phrases first.");
                    return;
                }

                using var dialog = new DrumTrackTestSettingsDialog();
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                int seed = dialog.Seed;

                Rng.Initialize(seed);

                var result = Generator.SongGenerator.GenerateFromPhrases(songContext, seed, maxBars: 0);

                result.MidiProgramName = $"Drums (Phrase-Based) (Seed: {seed})";
                result.MidiProgramNumber = 255;

                songContext.Song.PartTracks.Add(result);
                SongGridManager.AddNewPartTrack(result, dgSong);
                ShowSuccess(seed);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        // AI: purpose=Notify user of successful creation and seed to reproduce the result.
        private static void ShowSuccess(int seed)
        {
            MessageBoxHelper.Show(
                $"Phrase-based drum track created successfully.\n\n" +
                $"Seed: {seed}\n" +
                $"Generated full song.\n\n" +
                $"Use this seed to reproduce the same drum track.",
                "Drum Track Test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // AI: purpose=Present generator errors to user via MessageBox; message must be safe for UI display.
        private static void ShowError(string message)
        {
            MessageBoxHelper.Show(
                $"Generator error:\n{message}",
                "Generation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
