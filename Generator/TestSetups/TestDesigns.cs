using Music.Designer;
using Music.Generator;

namespace Music.Generator
{
    // Central defaults to keep tracks aligned
    public static class TestDesigns
    {
        public const int TotalBars = 48;
        public const string GlobalTimeSignature = "4/4";
        public const int DefaultTempoBpm = 120;  // per ai, 120 is acutually an industry default

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
            songContext.Song.TimeSignatureTrack = TimeSignatureTests.CreateTestTrackD1();

            // 5) Tempo track: include default tempo (112 BPM starting at bar 1)
            songContext.Song.TempoTrack = TempoTests.CreateTestTrackD1();

            // 6) Groove track: set one event at bar 1 beat one for PopRockBasic preset
            songContext.GrooveTrack = GrooveTests.CreateTestGrooveD1();
        }
    }
}
