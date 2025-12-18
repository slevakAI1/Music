namespace Music.Designer
{
    /// <summary>
    /// Design structure for a song
    /// </summary>
    public sealed class Designer
    {
        public string DesignId { get; }

        // Design Space
        public VoiceSet Voices { get; set; } = new();

        public SectionTimeline SectionTimeline { get; set; } = new();

        // Harmony timeline persisted with the design
        public HarmonyTimeline? HarmonyTimeline { get; set; }

        // New: independent timelines for tempo and time signature
        public TempoTimeline? TempoTimeline { get; set; }
        public TimeSignatureTimeline? TimeSignatureTimeline { get; set; }

        public Designer(string? designId = null)
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        }
    }
}