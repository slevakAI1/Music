using Music.Design;

namespace Music.Design
{
    // Builds the app's default tempo timeline (single 90 BPM event starting at bar 1).
    public static class TempoDefault
    {
        public static TempoTimeline BuildDefaultTimeline()
        {
            var timeline = new TempoTimeline();

            // Default: one 90 BPM event spanning 4 bars (assuming quarter-note beat = 4 beats/bar)
            timeline.Add(new TempoEvent
            {
                StartBar = 1,
                StartBeat = 1,
                TempoBpm = 90,
                DurationBeats = 4 * 4 // 4 bars at 4 beats/bar
            });

            return timeline;
        }
    }
}