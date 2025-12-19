namespace Music.Designer
{
    // Builds the app's default harmony timeline (48 bars).
    public static class HarmonyTests
    {
        public static HarmonyTimeline CreateTestTimelineD1()
        {
            var timeline = new HarmonyTimeline();
            timeline.ConfigureGlobal(DesignerTests.GlobalTimeSignature);

            // Common 4-chord loop: I – V – vi – IV, two bars per chord across 48 bars.
            var pattern = new (int degree, string quality)[]
            {
                (1, "Major"),
                (5, "Dominant7"),
                (6, "Minor7"),
                (4, "Major")
            };

            for (int bar = 1; bar <= DesignerTests.TotalBars; bar++)
            {
                var p = pattern[((bar - 1) / 2) % pattern.Length];
                Add(timeline, bar, key: "C major", degree: p.degree, quality: p.quality);
            }

            return timeline;
        }

        private static void Add(HarmonyTimeline timeline, int bar, string key, int degree, string quality)
        {
            timeline.Add(new HarmonyEvent
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