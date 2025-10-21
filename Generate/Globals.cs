namespace Music.Generate
{
    public static class Globals
    {
        public static ScoreDesignClass? ScoreDesign { get; set; }

        // Holds the app/session harmonic timeline
        public static HarmonicTimeline? HarmonicTimeline { get; set; }

        public static readonly VoiceManagerClass VoiceManager = new();
        public static readonly ChordManagerClass ChordManager = new();
        public static readonly SectionSetManagerClass SectionManager = new();
    }
}