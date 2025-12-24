namespace Music.Generator
{
    // Builds the app's default harmony track (48 bars).
    public static class HarmonyTests
    {
        public static HarmonyTrack CreateTestTrackD1()
        {
            var track = new HarmonyTrack();
            track.ConfigureGlobal(TestDesigns.GlobalTimeSignature);

            // Common 4-chord loop: I – V – vi – IV, two bars per 48 bars.
            // Using standard chord symbols
            var pattern = new (int degree, string quality)[]
            {
                (1, ""),      // Major
                (5, "7"),     // Dominant7
                (6, "m7"),    // Minor7
                (4, "")       // Major
            };

            for (int bar = 1; bar <= TestDesigns.TotalBars; bar++)
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
                DurationBeats = 4,
                Key = key,
                Degree = degree,
                Quality = quality,
                Bass = "root"
            });
        }
    }
}