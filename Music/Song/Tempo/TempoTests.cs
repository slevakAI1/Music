// AI: purpose=Create default tempo track used by tests/demos with a single BPM event at bar 1.
// AI: invariants=Produces one TempoEvent at StartBar=1; TempoBpm equals TestDesigns.DefaultTempoBpm; consumers expect discrete events.
// AI: deps=Relies on TempoTrack.Add and TestDesigns.DefaultTempoBpm; changing those breaks fixtures and default demos.
// AI: change=If adding tempo maps/ramps, provide a new factory; keep this for legacy single-event tests.

namespace Music.Generator
{
    // AI: returns a TempoTrack with a single tempo event covering the song (legacy default behavior).
    public static class TempoTests
    {
        public static TempoTrack CreateTestTrackD1()
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