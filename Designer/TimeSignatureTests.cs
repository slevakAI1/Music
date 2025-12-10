namespace Music.Designer
{
    // Builds the app's default time signature timeline (single 4/4 event starting at bar 1).
    public static class TimeSignatureTests
    {
        public static TimeSignatureTimeline CreateTestTimelineD1()
        {
            var timeline = new TimeSignatureTimeline();
            timeline.ConfigureGlobal(DesignerTests.GlobalTimeSignature);

            // One 4/4 event spanning the entire song
            // Duration is implicit - this event continues until another event or end of song
            timeline.Add(new TimeSignatureEvent
            {
                StartBar = 1,
                StartBeat = 1,
                Numerator = 4,
                Denominator = 4
            });

            return timeline;
        }
    }
}
