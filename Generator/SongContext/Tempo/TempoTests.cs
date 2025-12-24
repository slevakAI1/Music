namespace Music.Generator
{
    // Builds the app's default tempo track (single 90 BPM event starting at bar 1).
    public static class TempoTests
    {
        public static TempoTrack CreateTestTimelineD1()
        {
            var track = new TempoTrack();
            // Single event covering the entire song length
            track.Add(new TempoEvent
            {
                StartBar = 1,
                StartBeat = 1,
                TempoBpm = TestDesigns.DefaultTempoBpm,
            });

            return track;
        }
    }
}