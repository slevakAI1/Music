namespace Music.Design
{
    // Builds the app's default harmonic timeline (8 events).
    public static class HarmonicDefault
    {
        public static HarmonicTimeline BuildDefaultTimeline()
        {
            var timeline = new HarmonicTimeline();
            timeline.ConfigureGlobal("4/4");

            // Provided 4-event pattern, repeated to total 8 events.
            Add(timeline, bar: 1, key: "C major", degree: 1, quality: "maj");
            Add(timeline, bar: 2, key: "C major", degree: 5, quality: "dom7");
            Add(timeline, bar: 3, key: "C major", degree: 6, quality: "min7");
            Add(timeline, bar: 4, key: "C major", degree: 4, quality: "maj");

            Add(timeline, bar: 5, key: "C major", degree: 1, quality: "maj");
            Add(timeline, bar: 6, key: "C major", degree: 5, quality: "dom7");
            Add(timeline, bar: 7, key: "C major", degree: 6, quality: "min7");
            Add(timeline, bar: 8, key: "C major", degree: 4, quality: "maj");

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