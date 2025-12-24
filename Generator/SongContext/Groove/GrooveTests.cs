namespace Music.Generator
{
    // Builds minimal groove test tracks used by default/test designs.
    public static class GrooveTests
    {
        // Adds a minimal GrooveTrack for test/default designs.
        // One groove event at bar 1 beat 1 using the PopRockBasic preset.
        public static GrooveTrack CreateTestGrooveD1()
        {
            var groove = new GrooveTrack();
            groove.BeatsPerBar = 4;
            groove.Add(new GrooveInstance
            {
                StartBar = 1,
                SourcePresetName = "PopRockBasic"
            });

            // Multiple event test case:
            //groove.Add(new GrooveEvent
            //{
            //    StartBar = 3,
            //    StartBeat = 1,
            //    GroovePresetName = "FunkSyncopated"
            //});

            return groove;
        }
    }
}