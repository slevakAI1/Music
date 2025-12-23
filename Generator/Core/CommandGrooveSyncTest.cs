using Music.Designer;
using Music.Writer;
using Music.Writer.Generator;

namespace Music.Generator
{
    /// <summary>
    /// Command handler for the groove-driven generator test.
    /// Replaces the fixed test generator with groove-based song track generation.
    /// </summary>
    public static class CommandGrooveSyncTest
    {
        /// <summary>
        /// Handles the Harmony Groove Sync Test command.
        /// Generates synchronized test tracks using groove presets from the GrooveTrack timeline.
        /// SongTrackNumber is the next open/unused midi track number
        /// </summary>
        public static void HandleGrooveSyncTest(
            SongContext songContext,
            DataGridView dgSong,
            ref int songTrackNumber)
        {
            if (!ValidateHarmonyTrack(songContext.HarmonyTrack))
                return;

            if (!ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack))
                return;

            if (!ValidateGrooveTrack(songContext.GrooveTrack))
                return;

            try
            {
                // Generate all song tracks using the GrooveTrack
                var result = GrooveDrivenGenerator.Generate(
                    songContext.HarmonyTrack,
                    songContext.Song.TimeSignatureTrack,
                    songContext.GrooveTrack);

                int addedCount = 0;

                songContext.Song.PartTracks.Add(result.BassTrack);
                songContext.Song.PartTracks.Add(result.GuitarTrack);
                songContext.Song.PartTracks.Add(result.KeysTrack);
                songContext.Song.PartTracks.Add(result.DrumTrack);

                // Update Grid with song tracks!
                SongGridManager.AddNewTrack(result.BassTrack, dgSong, ref songTrackNumber);
                addedCount++;

                SongGridManager.AddNewTrack(result.GuitarTrack, dgSong, ref songTrackNumber);
                addedCount++;

                SongGridManager.AddNewTrack(result.KeysTrack, dgSong, ref songTrackNumber);
                addedCount++;

                SongGridManager.AddNewTrack(result.DrumTrack, dgSong, ref songTrackNumber);
                addedCount++;

                ShowGrooveSuccess(addedCount);
            }
            catch (Exception ex)
            {
                ShowGrooveError(ex);
            }
        }

        // Validation / Messagebox helpers

        #region Validation / Messagebox helpers

        private static bool ValidateHarmonyTrack(HarmonyTrack harmonyTimeline)
        {
            if (harmonyTimeline == null || harmonyTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show(
                    "No harmony events defined. Please add harmony events first.",
                    "Missing Harmony",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private static bool ValidateTimeSignatureTrack(TimeSignatureTrack timeSignatureTimeline)
        {
            if (timeSignatureTimeline == null || timeSignatureTimeline.Events.Count == 0)
            {
                MessageBoxHelper.Show(
                    "No time signature events defined. Please add at least one time signature event.",
                    "Missing Time Signature",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private static bool ValidateGrooveTrack(GrooveTrack grooveTrack)
        {
            if (grooveTrack == null || grooveTrack.Events.Count == 0)
            {
                MessageBoxHelper.Show(
                    "No groove events defined. Please add at least one groove event first.",
                    "Missing Groove",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

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