// AI: purpose=Generate default 52-bar test harmony track used in demos/tests; pattern repeats I-V-vi-IV on odd bars.
// AI: invariants=Produces events at odd StartBar values 1..TotalBars; callers expect DurationBeats to cover two bars here.
// AI: deps=Relies on HarmonyTrack.Add behavior and TestDesigns.TotalBars; renaming fields or constants breaks fixtures.
// AI: change=If altering pattern or TotalBars update dependent tests and any test fixtures that assert these events.

namespace Music.Generator
{
    // AI: factory=CreateTestTrackD1 returns a paired-bar I–V–vi–IV loop; events start at beat 1 and span two bars each.
    public static class HarmonyTests
    {
        public static HarmonyTrack CreateTestTrackD1()
        {
            var track = new HarmonyTrack();

            // Pattern: I – V – vi – IV (standard pop progression)
            var pattern = new (int degree, string quality)[]
            {
                (1, ""),      // Major
                (5, "7"),     // Dominant7
                (6, "m7"),    // Minor7
                (4, "")       // Major
            };

            // Place each pattern item on one bar, then skip the next bar (i.e. bars 1,3,5,...)
            for (int bar = 1; bar <= TestDesigns.TotalBars; bar += 2)
            {
                var p = pattern[((bar - 1) / 2) % pattern.Length];
                Add(track, bar, key: "C major", degree: p.degree, quality: p.quality);
            }

            return track;
        }

        // AI: Add helper: creates HarmonyEvent with StartBeat=1, DurationBeats=8 (spans two bars), Bass default "root".
        private static void Add(HarmonyTrack track, int bar, string key, int degree, string quality)
        {
            track.Add(new HarmonyEvent
            {
                StartBar = bar,
                StartBeat = 1,
                // DurationBeats covers two bars (8 beats) so the event spans the written bar and the skipped bar
                DurationBeats = 8,
                Key = key,
                Degree = degree,
                Quality = quality,
                Bass = "root"
            });
        }
    }
}