// AI: purpose=Handler to test drummer agent-based drum track generation and add result to SongContext and UI grid.
// AI: invariants=Uses real groove anchor pattern; passes StyleConfiguration to enable operator-based generation; mutates songContext.Song.PartTracks and Grid.
// AI: deps=Relies on Generator.Generator.Generate with StyleConfiguration, GrooveAnchorFactory, StyleConfigurationLibrary; changing generator API breaks this.
// AI: perf=Generation may allocate; run on UI thread currently; consider backgrounding if UI stalls for large songs.

using Music.Generator;
using Music.Generator.Agents.Common;
using Music.Generator.Groove;

namespace Music.Writer
{
    // AI: Command handler for drummer agent test; wraps generator call with style configuration and updates UI grid with results.
    public static class HandleCommandAgentTest
    {
        // AI: HandleAgentTest: runs drummer agent generator, appends 1 drum PartTrack to Song and grid.
        // AI: errors=any exception is shown via ShowError; no retry or partial-commit logic.
        public static void HandleAgentTest(
            SongContext songContext,
            DataGridView dgSong)
        {
            try
            {
                // Use real PopRock groove anchor pattern (not empty layer)
                var groovePreset = new GroovePresetDefinition
                {
                    Identity = new GroovePresetIdentity
                    {
                        Name = "PopRock",
                        BeatsPerBar = 4,
                        StyleFamily = "PopRock"
                    },
                    AnchorLayer = GrooveAnchorFactory.GetAnchor("PopRock")
                };
                songContext.GroovePresetDefinition = groovePreset;

                // Use StyleConfiguration to enable drummer agent (operator-based generation)
                var drummerStyle = StyleConfigurationLibrary.PopRock;

                // Generate drum track using drummer agent pipeline
                var result = Generator.Generator.Generate(songContext, drummerStyle);

                songContext.Song.PartTracks.Add(result);

                // Update Grid with drum track
                SongGridManager.AddNewPartTrack(result, dgSong);
                ShowSuccess(1);
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
                $"Successfully created {addedCount} drum track(s) using drummer agent.",
                "Drummer Agent Test",
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
