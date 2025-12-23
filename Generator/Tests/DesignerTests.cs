using Music.Generator;

namespace Music.Designer
{
    // Central defaults to keep timelines aligned
    public static class DesignerTests
    {
        public const int TotalBars = 48;
        public const string GlobalTimeSignature = "4/4";
        public const int DefaultTempoBpm = 120;  // per ai, 120 is acutually an industry default

        // Apply all defaults so the timelines end on the same beat
        public static void SetTestDesignD1(SongContext songContext)
        {
            // 1) Sections: apply default/test structure
            var sectionTestData = new SectionTests();
            sectionTestData.SetTestSectionsD1(songContext.SectionTrack);

            // 2) Voices: apply default voices
            songContext.Voices.SetTestVoicesD1();    // TODO - the others have separate classes!

            // 3) Harmony timeline: use the same defaults as the Harmony Editor's "Set Defaults"
            songContext.HarmonyTrack = HarmonyTests.CreateTestTimelineD1();

            // 4) Time signature timeline: apply default (4/4 starting at bar 1)
            songContext.Song.TimeSignatureTrack = TimeSignatureTests.CreateTestTimelineD1();

            // 5) Tempo timeline: include default tempo (112 BPM starting at bar 1)
            songContext.Song.TempoTrack = TempoTests.CreateTestTimelineD1();

            // 6) Groove timeline: set one event at bar 1 beat one for PopRockBasic preset
            songContext.GrooveTrack = GrooveTests.CreateTestGrooveD1();
        }
    }
}
