using Music.Design;

namespace Music.Design
{
    // Builds the app's default tempo timeline (single 90 BPM event starting at bar 1).
    public static class TempoDefault
    {
        public static TempoTimeline BuildDefaultTimeline()
        {
            var timeline = new TempoTimeline();
            // Single event covering the entire song length
            timeline.Add(new TempoEvent
            {
                StartBar = 1,
                StartBeat = 1,
                TempoBpm = DesignerDefaults.DefaultTempoBpm,
                DurationBeats = DesignerDefaults.TotalBars * timeline.BeatsPerBar
            });

            return timeline;
        }
    }
}