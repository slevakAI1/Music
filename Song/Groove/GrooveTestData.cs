// AI: purpose=Create minimal GrooveTrack used by tests/default song scaffolds.
// AI: invariants=Produces one GrooveEvent with StartBar=1 and SourcePresetName="PopRockBasic"; callers may mutate result.
// AI: deps=Relies on GrooveEvent and GrooveTrack shape; renaming props breaks tests/assets.
// AI: change=If altering preset names, update this factory and any test fixtures that rely on it.

namespace Music.Generator
{
    // AI: factory method: returns a GrooveTrack with a single event at bar 1 using PopRockBasic preset.
    public static class GrooveTrackTestData
    {
        // AI: CreateTestGrooveD1 used in unit tests and default demos; keep commented multi-event example for manual testing.
        public static GrooveTrack CreateTestGrooveD1(BarTrack barTrack)
        {
            var grooveTrack = new GrooveTrack();

            grooveTrack.Add(new GrooveEvent()
            {
                StartBar = 1,
                SourcePresetName = "PopRockBasic"
            });

            // Multiple event test case - do not delete this:
            //grooveTrack.Add(new GrooveEvent(barTrack)
            //{
            //    StartBar = 3,
            //    StartBeat = 1,
            //    GroovePresetName = "FunkSyncopated"
            //});

            return grooveTrack;
        }
    }
}