namespace Music.Designer
{
    /// <summary>
    /// Design structure for a song
    /// </summary>
    public sealed class SongContext
    {
        public string DesignId { get; }

        // Design Space
        public VoiceSet Voices { get; set; } = new();

        public SectionTrack SectionTrack { get; set; } = new();

        // Harmony timeline persisted with the design
        public HarmonyTrack? HarmonyTrack { get; set; }

        public GrooveTrack? GrooveTrack { get; set; }

        // New: independent timelines for tempo and time signature
        public TempoTrack? TempoTrack { get; set; }
        public TimeSignatureTrack? TimeSignatureTrack { get; set; }

        public SongContext(string? designId = null)
        {
            DesignId = designId ?? Guid.NewGuid().ToString("N");
        }
    }
}