namespace Music.Generator
{
    // Builds the app's default harmony track (48 bars).
    public static class HarmonyTests
    {
        public static HarmonyTrack CreateTestTrackD1()
        {
            var track = new HarmonyTrack();

            // Common 4-chord loop: I – V – vi – IV, two bars per 48 bars.
            // Using standard chord symbols
            var pattern = new (int degree, string quality)[]
            {
                (1, ""),      // Major
                (5, "7"),     // Dominant7
                (6, "m7"),    // Minor7
                (4, "")       // Major
            };

            // Place each pattern item on one bar, then skip the next bar (i.e. bars 1,3,5,...)
            for (int bar = 1; bar <= TestDesigns.TotalBars; bar += 2)
            {
                var p = pattern[((bar - 1) / 2) % pattern.Length];
                Add(track, bar, key: "C major", degree: p.degree, quality: p.quality);
            }

            return track;
        }

        private static void Add(HarmonyTrack track, int bar, string key, int degree, string quality)
        {
            track.Add(new HarmonyEvent
            {
                StartBar = bar,
                StartBeat = 1,
                // DurationBeats covers two bars (8 beats) so the event spans the written bar and the skipped bar
                DurationBeats = 8,
                Key = key,
                Degree = degree,
                Quality = quality,
                Bass = "root"
            });
        }
    }
}