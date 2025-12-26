namespace Music.Generator
{
    // Builds the app's default time signature track (single 4/4 event starting at bar 1).
    public static class TimingTests
    {
        public static Timingtrack CreateTestTrackD1()
        {
            var track = new Timingtrack();
            track.ConfigureGlobal(TestDesigns.GlobalTimeSignature);

            // One 4/4 event spanning the entire song
            // Duration is implicit - this event continues until another event or end of song
            track.Add(new TimingEvent
            {
                StartBar = 1,
                StartBeat = 1,
                Numerator = 4,
                Denominator = 4
            });

            track.Add(new TimingEvent
            {
                StartBar = 2,
                StartBeat = 1,
                Numerator = 3,
                Denominator = 4
            });

            return track;
        }
    }
}
