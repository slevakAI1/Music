// AI: purpose=UI command: generate drummer-agent phrase-based drum PartTrack and append to Song+grid.
// AI: invariants=Requires songContext.BarTrack; groove anchor must be available; seed makes generation deterministic.
// AI: deps=Generator.SongGenerator.Generate; GrooveAnchorFactory; CreateDrumPhraseSettingsDialog; Rng.Initialize
// AI: perf=Potentially expensive; runs on UI thread. Move to background thread if UI responsiveness degrades.

using Music.Generator;
using Music.Generator.Groove;
using Music.MyMidi;

namespace Music.Writer
{
    // AI: purpose=Handle UI command to configure and run drummer generator; update Song and DataGridView.
    public static class HandleCommandCreateDrumPhrase
    {
        // AI: entry=Validate SongContext; show modal seed/genre/bars dialog; init RNG and groove preset.
        // AI: effects=Appends one drum PartTrack to songContext.Song.PartTracks and updates grid via SongGridManager.
        // AI: errors=All exceptions shown via ShowError; dialog prevents invalid inputs.
        public static void HandleDrumPhraseTest(
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
                int repeat = dialog.Repeat;
                int numberOfOperators = dialog.NumberOfOperators;

                int effectiveBars = bars;
                if (songContext.SectionTrack != null && bars > songContext.SectionTrack.TotalBars)
                    effectiveBars = songContext.SectionTrack.TotalBars;

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

                // Generate drum track using drummer agent pipeline (bars=0 means full song, >0 limits generation)
                var result = Generator.SongGenerator.Generate(songContext, bars, numberOfOperators);

                ApplyPhraseRepeat(result, songContext.BarTrack, effectiveBars, repeat);

                // Set descriptive name with seed and mark as drum set for correct playback
                string barsInfo = bars > 0 ? $" ({bars} bars)" : "";
                result.MidiProgramName = $"Drummer Agent{barsInfo} (Seed: {seed})";
                result.MidiProgramNumber = 255; // 255 = Drum Set sentinel in MidiVoices

                songContext.Song.PartTracks.Add(result);

                // Update Grid with drum track
                SongGridManager.AddNewPartTrack(result, dgSong);
                if (dgSong.Rows.Count > SongGridManager.FIXED_ROWS_COUNT)
                {
                    int newRowIndex = dgSong.Rows.Count - 1;
                    dgSong.ClearSelection();
                    dgSong.Rows[newRowIndex].Selected = true;

                    var instrumentCol = dgSong.Columns["colInstrument"];
                    if (instrumentCol != null && dgSong.Rows[newRowIndex].Cells[instrumentCol.Index] != null)
                    {
                        dgSong.CurrentCell = dgSong.Rows[newRowIndex].Cells[instrumentCol.Index];
                    }
                }
                ShowSuccess(seed, genre, bars);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #region MessageBox

        private static void ApplyPhraseRepeat(
            PartTrack track,
            BarTrack barTrack,
            int phraseBars,
            int repeatCount)
        {
            ArgumentNullException.ThrowIfNull(track);
            ArgumentNullException.ThrowIfNull(barTrack);

            if (repeatCount <= 1 || phraseBars <= 0)
                return;

            long phraseLengthTicks = barTrack.GetBarEndTick(phraseBars);
            if (phraseLengthTicks <= 0)
                return;

            List<PartTrackEvent> originalEvents = track.PartTrackNoteEvents;
            var repeatedEvents = new List<PartTrackEvent>(originalEvents.Count * repeatCount);

            for (int repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
            {
                long offset = phraseLengthTicks * repeatIndex;
                foreach (PartTrackEvent source in originalEvents)
                {
                    repeatedEvents.Add(CloneEventWithOffset(source, offset));
                }
            }

            track.PartTrackNoteEvents = repeatedEvents
                .OrderBy(e => e.AbsoluteTimeTicks)
                .ToList();
        }

        private static PartTrackEvent CloneEventWithOffset(PartTrackEvent source, long offset)
        {
            ArgumentNullException.ThrowIfNull(source);

            var parameters = source.Parameters.Count == 0
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(source.Parameters);

            return new PartTrackEvent
            {
                AbsoluteTimeTicks = source.AbsoluteTimeTicks + offset,
                DeltaTicks = source.DeltaTicks,
                Type = source.Type,
                Parameters = parameters,
                NoteNumber = source.NoteNumber,
                NoteDurationTicks = source.NoteDurationTicks,
                NoteOnVelocity = source.NoteOnVelocity
            };
        }

        // AI: purpose=Notify user of successful generation and provide seed for reproduction; indicates bars generated.
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

        // AI: purpose=Present generator errors to the user via MessageBox; messages must be safe for UI display.
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
