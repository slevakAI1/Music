namespace Music.Design
{
    /// <summary>
    /// Design structure for a musical score
    /// </summary>
    public sealed class DesignClass
    {
        public string DesignId { get; }

        // Design Space
        public VoiceSetClass VoiceSet { get; set; } = new();

        public SectionSetClass SectionSet { get; set; } = new();

        // Harmonic timeline persisted with the design
        public HarmonicTimeline? HarmonicTimeline { get; set; }

        public DesignClass(string? designId = null)
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        }
    }
}