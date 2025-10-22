using Music.Design;

namespace Music.Design
{
    // Builds the app's default tempo timeline (single 90 BPM event starting at bar 1).
    public static class TempoDefault
    {
        public static TempoTimeline BuildDefaultTimeline()
        {
            var timeline = new TempoTimeline();
            // One event at 90 BPM covering 8 bars (default design length); adjust as needed.
            timeline.Add(new TempoEvent
            {
                StartBar = 1,
                StartBeat = 1,
                TempoBpm = 90,
                DurationBeats = 8 * timeline.BeatsPerBar
            });
            return timeline;
        }
    }
}