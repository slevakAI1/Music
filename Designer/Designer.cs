namespace Music.Designer
{
    /// <summary>
    /// Design structure for a musical score
    /// </summary>
    public sealed class Designer
    {
        public string DesignId { get; }

        // Design Space
        public PartSetClass PartSet { get; set; } = new();

        public SectionTimelineClass SectionSet { get; set; } = new();

        // Harmonic timeline persisted with the design
        public HarmonicTimeline? HarmonicTimeline { get; set; }

        // New: independent timelines for tempo and time signature
        public TempoTimeline? TempoTimeline { get; set; }
        public TimeSignatureTimeline? TimeSignatureTimeline { get; set; }

        public Designer(string? designId = null)
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        }
    }
}