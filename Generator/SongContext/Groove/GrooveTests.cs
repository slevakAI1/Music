namespace Music.Generator
{
    // Builds minimal grooveTrack test tracks used by default/test designs.
    public static class GrooveTests
    {
        // Adds a minimal GrooveTrack for test/default designs.
        // One grooveTrack event at bar 1 beat 1 using the PopRockBasic preset.
        public static GrooveTrack CreateTestGrooveD1()
        {
            var grooveTrack = new GrooveTrack();


            grooveTrack.BeatsPerBar = 4;     //  TO DO - this needs to be in the preset!!! move it there and reference it from there.



            grooveTrack.Add(new GrooveEvent
            {
                StartBar = 1,
                SourcePresetName = "PopRockBasic"
            });

            // Multiple event test case:
            //grooveTrack.Add(new GrooveEvent
            //{
            //    StartBar = 3,
            //    StartBeat = 1,
            //    GroovePresetName = "FunkSyncopated"
            //});

            return grooveTrack;
        }
    }
}