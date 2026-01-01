// AI: purpose=Factory for a default Timingtrack used by tests/demos; produces discrete time-signature events.
// AI: invariants=This returns one TimingEvent at StartBar=1 (covers song until changed); Numerator/Denominator should be valid ints.
// AI: deps=Relies on Timingtrack.Add and exporters that expect discrete events; altering model affects consumers.
// AI: change=If adding tempo maps/ramps or non-discrete signatures, add new factory instead of altering this legacy helper.

namespace Music.Generator
{
    // AI: CreateTestTrackD1: returns a Timingtrack with a single 4/4 event; keep inline example of multi-event usage.
    public static class TimingTests
    {
        public static Timingtrack CreateTestTrackD1()
        {
            var track = new Timingtrack();

            // AI: single 4/4 event spanning song; duration implicit until next TimingEvent or end of song.
            track.Add(new TimingEvent
            {
                StartBar = 1,
                Numerator = 4,
                Denominator = 4
            });

            // Do not delete - example of adding another time signature event
            //track.Add(new TimingEvent
            //{
            //    StartBar = 2,
            //    Numerator = 3,
            //    Denominator = 4
            //});

            return track;
        }
    }
}
