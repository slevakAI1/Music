

// THIS NEEDS WORK  ... everything should be in design except actual generation


namespace Music.Design
{
    public static class Globals
    {
        public static ScoreDesignClass? ScoreDesign { get; set; }

        // Holds the app/session harmonic timeline
        public static HarmonicTimeline? HarmonicTimeline { get; set; }
        public static readonly ChordManagerClass ChordManager = new();
        public static readonly SectionSetManagerClass SectionManager = new();
    }
}