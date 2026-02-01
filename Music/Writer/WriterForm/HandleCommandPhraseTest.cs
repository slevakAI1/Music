// AI: purpose=Handler to test drummer agent V1-based phrase generation and add result to SongContext and UI grid.
// AI: invariants=Uses real groove anchor pattern; passes StyleConfiguration to enable operator-based generation; mutates songContext.Song.PartTracks and Grid.
// AI: deps=Relies on Generator.Generator.Generate with StyleConfiguration, GrooveAnchorFactory, StyleConfigurationLibrary; changing generator API breaks this.
// AI: perf=Generation may allocate; run on UI thread currently; consider backgrounding if UI stalls for large songs.

using Music.Generator;
using Music.Generator.Agents.Common;
using Music.Generator.Groove;

namespace Music.Writer
{
    // AI: Command handler for drummer agent test; wraps generator call with style configuration and updates UI grid with results.
    public static class HandleCommandPhraseTest
    {
        // AI: HandlePhraseTest: shows input dialog, runs drummer agent generator with seed/genre/bars, appends 1 drum PartTrack to Song and grid.
        // AI: errors=any exception is shown via ShowError; no retry or partial-commit logic; invalid input caught by dialog validation.
        public static void HandlePhraseTest(
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
                using var dialog = new PhraseTestSettingsDialog();
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                // Capture parameters
                int seed = dialog.Seed;
                string genre = dialog.Genre;
                int bars = dialog.Bars;

                // Initialize RNG with seed for deterministic generation
                Rng.Initialize(seed);

                // Get groove anchor pattern for selected genre
                var groovePreset = new GroovePresetDefinition
                {
                    Identity = new GroovePresetIdentity
                    {
                        Name = genre,
                        BeatsPerBar = 4,
                        StyleFamily = genre
                    },
                    AnchorLayer = GrooveAnchorFactory.GetAnchor(genre)
                };
                songContext.GroovePresetDefinition = groovePreset;

                // Get StyleConfiguration for selected genre (enables drummer agent)
                var drummerStyle = StyleConfigurationLibrary.GetStyle(genre);
                if (drummerStyle == null)
                {
                    ShowError($"Style configuration not found for genre: {genre}");
                    return;
                }

                // Generate drum track using drummer agent pipeline (bars=0 means full song, >0 limits generation)
                var result = Generator.Generator.Generate(songContext, drummerStyle, bars);

                // Set descriptive name with seed and mark as drum set for correct playback
                string barsInfo = bars > 0 ? $" ({bars} bars)" : "";
                result.MidiProgramName = $"Drummer Agent{barsInfo} (Seed: {seed})";
                result.MidiProgramNumber = 255; // 255 = Drum Set sentinel in MidiVoices

                songContext.Song.PartTracks.Add(result);

                // Update Grid with drum track
                SongGridManager.AddNewPartTrack(result, dgSong);
                ShowSuccess(seed, genre, bars);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #region MessageBox

        // AI: ShowSuccess: displays seed for reproduction; message stable for testing; shows if limited or full song.
        private static void ShowSuccess(int seed, string genre, int bars)
        {
            string barsMessage = bars > 0 
                ? $"Generated first {bars} bars."
                : "Generated full song.";

            MessageBoxHelper.Show(
                $"Drummer agent track created successfully.\n\n" +
                $"Genre: {genre}\n" +
                $"Seed: {seed}\n" +
                $"{barsMessage}\n\n" +
                $"Use this seed to reproduce the same drum track.",
                "Drummer Agent Test",
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

        #endregion
    }
}
