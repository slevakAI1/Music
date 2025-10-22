namespace Music.Design
{
    // Central defaults to keep timelines aligned
    public static class DesignDefaults
    {
        public const int TotalBars = 48;
        public const string GlobalTimeSignature = "4/4";
        public const int DefaultTempoBpm = 90;

        // Apply all defaults so the timelines end on the same beat
        public static void ApplyDefaultDesign(DesignClass design)
        {
            // Sections
            new SectionDefaultsClass().CreateTestSections(design.SectionSet);

            // Timelines
            design.TimeSignatureTimeline = TimeSignatureDefault.BuildDefaultTimeline();
            design.TempoTimeline = TempoDefault.BuildDefaultTimeline();
            design.HarmonicTimeline = HarmonicDefault.BuildDefaultTimeline();
        }
    }
}