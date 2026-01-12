// AI: purpose=Assemble a canonical SongContext for tests/demos so all design tracks align to TotalBars.
// AI: invariants=After SetTestDesignD1 all design and song tracks cover TestDesigns.TotalBars and are contiguous.
// AI: deps=Depends on SectionTests, HarmonyTests, TimingTests, TempoTests, GrooveTrackTestData, and BarTrack.RebuildFromTimingTrack.
// AI: change=If you change defaults update all tests/fixtures and any code that asserts specific part names or TotalBars.

using Music.Designer;
using Music.Generator;

namespace Music.Generator
{
    // AI: mutates=This method populates many SongContext fields; callers expect these side-effects (no new SongContext created).
    // AI: order=Builds sections->voices->harmony->timing->tempo->groove->bartrack; BarTrack.RebuildFromTimingTrack must run last.
    // Central defaults to keep tracks aligned
    public static class TestDesigns
    {
        public const int TotalBars = 48;  // TO DO THIS SHOULD BE COMPUTED FROM THE SECTION TRACK
        public const string GlobalTimeSignature = "4/4";
        public const int DefaultTempoBpm = 120;  // per ai, 120 is acutally an industry default

        // Apply all defaults so the tracks end on the same beat
        public static void SetTestDesignD1(SongContext songContext)
        {
            // 1) Sections: apply default/test structure
            var sectionTestData = new SectionTests();
            sectionTestData.SetTestSectionsD1(songContext.SectionTrack);

            // 2) Voices: apply default voices
            songContext.Voices.SetTestVoicesD1();    // TODO - the others have separate classes!

            // 3) Harmony track: use the same defaults as the Harmony Editor's "Set Defaults"
            songContext.HarmonyTrack = HarmonyTests.CreateTestTrackD1();

            // 4) Time signature track: apply default (4/4 starting at bar 1)
            songContext.Song.TimeSignatureTrack = TimingTests.CreateTestTrackD1();

            // 5) Tempo track: include default tempo (112 BPM starting at bar 1)
            // AI: note=DefaultTempoBpm constant is 120; ensure TempoTests uses TestDesigns.DefaultTempoBpm and avoid hardcoded values.
            songContext.Song.TempoTrack = TempoTests.CreateTestTrackD1();

            // 6) Build bar track from timing track using SectionTrack total bars
            // AI: note=BarTrack must be built before GrooveTrack since GrooveEvent constructor requires non-null BarTrack.
            songContext.BarTrack.RebuildFromTimingTrack(songContext.Song.TimeSignatureTrack, songContext.SectionTrack.TotalBars);

            // 7) Groove track: set one event at bar 1 beat one for PopRockBasic preset
            songContext.GrooveTrack = GrooveTrackTestData.CreateTestGrooveD1(songContext.BarTrack);
        }
    }
}
