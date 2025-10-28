namespace Music.Design
{
    // Central defaults to keep timelines aligned
    public static class DesignDefaults
    {
        public const int TotalBars = 48;
        public const string GlobalTimeSignature = "4/4";
        public const int DefaultTempoBpm = 112;

        // Apply all defaults so the timelines end on the same beat
        public static void ApplyDefaultDesign(DesignClass design)
        {
            // 1) Sections: apply default/test structure
            var sectionsHelper = new SectionDefaultsClass();
            sectionsHelper.CreateTestSections(design.SectionSet);

            // 2) Voices: apply default voices
            design.VoiceSet.AddDefaultVoices();

            // 3) Harmonic timeline: use the same defaults as the Harmonic Editor's "Set Defaults"
            design.HarmonicTimeline = HarmonicDefault.BuildDefaultTimeline();

            // 4) Time signature timeline: apply default (4/4 starting at bar 1)
            design.TimeSignatureTimeline = TimeSignatureDefault.BuildDefaultTimeline();

            // 5) Tempo timeline: include default tempo (112 BPM starting at bar 1)
            design.TempoTimeline = TempoDefault.BuildDefaultTimeline();
        }
    }
}