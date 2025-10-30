namespace Music.Design
{
    // Builds the app's default harmonic timeline (48 bars).
    public static class HarmonicDefault
    {
        public static HarmonicTimeline BuildDefaultTimeline()
        {
            var timeline = new HarmonicTimeline();
            timeline.ConfigureGlobal(DesignDefaults.GlobalTimeSignature);

            // Common 4-chord loop: I – V – vi – IV, one chord per bar across 48 bars.
            var pattern = new (int degree, string quality)[]
            {
                (1, "maj"),
                (5, "dom7"),
                (6, "min7"),
                (4, "maj")
            };

            for (int bar = 1; bar <= DesignDefaults.TotalBars; bar++)
            {
                var p = pattern[(bar - 1) % pattern.Length];
                Add(timeline, bar, key: "C major", degree: p.degree, quality: p.quality);
            }

            return timeline;
        }

        private static void Add(HarmonicTimeline timeline, int bar, string key, int degree, string quality)
        {
            timeline.Add(new HarmonicEvent
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