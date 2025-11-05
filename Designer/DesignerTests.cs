namespace Music.Designer
{
    // Central defaults to keep timelines aligned
    public static class DesignerTests
    {
        public const int TotalBars = 48;
        public const string GlobalTimeSignature = "4/4";
        public const int DefaultTempoBpm = 112;

        // Apply all defaults so the timelines end on the same beat
        public static void SetTestDesignD1(Designer design)
        {
            // 1) Sections: apply default/test structure
            var sectionTestData = new SectionTests();
            sectionTestData.SetTestSectionsD1(design.SectionSet);

            // 2) Voices: apply default voices
            design.PartSet.SetTestVoicesD1();    // TODO - the others have separate classes!

            // 3) Harmonic timeline: use the same defaults as the Harmonic Editor's "Set Defaults"
            design.HarmonicTimeline = HarmonicTests.CreateTestTimelineD1();

            // 4) Time signature timeline: apply default (4/4 starting at bar 1)
            design.TimeSignatureTimeline = TimeSignatureTests.CreateTestTimelineD1();

            // 5) Tempo timeline: include default tempo (112 BPM starting at bar 1)
            design.TempoTimeline = TempoTests.CreateTestTimelineD1();
        }
    }
}
