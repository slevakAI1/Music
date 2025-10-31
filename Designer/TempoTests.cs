using Music.Designer;

namespace Music.Designer
{
    // Builds the app's default tempo timeline (single 90 BPM event starting at bar 1).
    public static class TempoTests
    {
        public static TempoTimeline CreateTestTimelineD1()
        {
            var timeline = new TempoTimeline();
            // Single event covering the entire song length
            timeline.Add(new TempoEvent
            {
                StartBar = 1,
                StartBeat = 1,
                TempoBpm = DesignerTests.DefaultTempoBpm,
                DurationBeats = DesignerTests.TotalBars * timeline.BeatsPerBar
            });

            return timeline;
        }
    }
}