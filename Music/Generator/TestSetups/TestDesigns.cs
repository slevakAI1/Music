// AI: purpose=Assemble canonical SongContext for tests/demos so design tracks align to TotalBars
// AI: invariants=After SetTestDesignD1 tracks cover TotalBars contiguous; BarTrack rebuilt before motifs
// AI: deps=SectionTests,HarmonyTests,TimingTests,TempoTests,GrooveTrackTestData,MotifLibrary,BarTrack.RebuildFromTimingTrack
// AI: contract=Mutates provided SongContext in-place; callers expect these side-effects and alignment
using Music.Song.Material;

namespace Music.Generator
{
    // AI: order=Builds sections->voices->harmony->timing->tempo->bartrack->motifs; BarTrack.RebuildFromTimingTrack must run before motifs
    // AI: note=Do not create a new SongContext; this method mutates the provided instance
    public static class TestDesigns
    {
        public const int TotalBars = 48;  // TO DO THIS SHOULD BE COMPUTED FROM THE SECTION TRACK
        public const string GlobalTimeSignature = "4/4";
        public const int DefaultTempoBpm = 120;  // per ai, 120 is acutally an industry default

        // AI: entry=Populate songContext with sections, voices, harmony, timing, tempo, barTrack, and test motifs
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
            songContext.BarTrack.RebuildFromTimingTrack(
                songContext.Song.TimeSignatureTrack,
                songContext.SectionTrack,
                songContext.SectionTrack.TotalBars);

            // 8) Material bank: populate with test motifs for Stage 9+ testing
            // AI: note=Adds all 4 hardcoded test motifs from MotifLibrary; motifs stored as PartTracks with MaterialLocal domain.
            PopulateTestMotifs(songContext.MaterialBank);
        }

        // AI: helper=Add all MotifLibrary test motifs to MaterialBank as PartTracks; preserves MaterialLocal->PartTrack conversion
        private static void PopulateTestMotifs(MaterialBank bank)
        {
            var allMotifs = MotifLibrary.GetAllTestMotifs();

            foreach (var motif in allMotifs)
            {
                var track = motif.ToPartTrack();
                bank.Add(track);
            }
        }
    }
}
