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

            design.VoiceSet.AddDefaultVoices();

            //TEMPO DEFAULT - TODO TEST THIS
            //
            // Ensure the design has a tempo timeline and that the first tempo event
            // represents the default tempo value. If missing, insert a default tempo event.
            design.TempoTimeline ??= new TempoTimeline();

            // Ensure Events list exists
            design.TempoTimeline.Events ??= new List<TempoEvent>();

            // Add a single default event covering the default total bars
            var duration = DesignDefaults.TotalBars * design.TempoTimeline.BeatsPerBar;
            design.TempoTimeline.Add(new TempoEvent
            {
                StartBar = 1,
                StartBeat = 1,
                TempoBpm = DesignDefaults.DefaultTempoBpm,
                DurationBeats = duration
            });

        }
    }
}