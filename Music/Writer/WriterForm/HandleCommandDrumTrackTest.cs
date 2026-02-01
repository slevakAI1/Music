// AI: purpose=Manual test for phrase-based DrumGenerator; uses MaterialBank phrases to build full track.
// AI: invariants=MaterialBank must contain phrases; always generates full song.
// AI: deps=Generator.GenerateFromPhrases; DrumTrackTestSettingsDialog for seed input.

using Music.Generator;

namespace Music.Writer
{
    // AI: Command handler for phrase-based drum track generation; uses MaterialBank and updates grid with result.
    public static class HandleCommandDrumTrackTest
    {
        // AI: HandleDrumTrackTest: shows input dialog, runs phrase-based generator, appends 1 drum PartTrack to Song and grid.
        // AI: errors=any exception is shown via ShowError; no retry or partial-commit logic; invalid input caught by dialog validation.
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

                var result = Generator.Generator.GenerateFromPhrases(songContext, seed, maxBars: 0);

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

        // AI: ShowSuccess: displays seed for reproduction; generates full song.
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

        // AI: ShowError: shows error message; overload for exception and string message.
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
