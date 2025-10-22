namespace Music.Design
{
    /// <summary>
    /// Design structure for a musical score
    /// </summary>
    public sealed class ScoreDesignClass
    {
        public string DesignId { get; }

        // Design Space
        public VoiceSetClass VoiceSet { get; } = new();

        public SectionSetClass SectionSet { get; } = new();

        // Harmonic timeline persisted with the design
        public HarmonicTimeline? HarmonicTimeline { get; set; }

        public ScoreDesignClass(string? designId = null)
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        }
    }
}