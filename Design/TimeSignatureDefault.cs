namespace Music.Design
{
    // Builds the app's default time signature timeline (single 4/4 event starting at bar 1).
    public static class TimeSignatureDefault
    {
        public static TimeSignatureTimeline BuildDefaultTimeline()
        {
            var timeline = new TimeSignatureTimeline();
            timeline.ConfigureGlobal("4/4");

            // Default: one 4/4 event spanning 4 bars
            timeline.Add(new TimeSignatureEvent
            {
                StartBar = 1,
                StartBeat = 1,
                Numerator = 4,
                Denominator = 4,
                DurationBeats = 4 * 4 // 4 bars of 4/4
            });

            return timeline;
        }
    }
}
