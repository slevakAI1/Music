using Music.Generator;

namespace Music.Designer
{
    /// <summary>
    /// Design structure for a song.
    /// </summary>
    public sealed class SongContext
    {
        public SongContext(string? designId = null)
        {
            // designId intentionally retained for future use
        }

        /// <summary>
        /// Current bar (1-based).
        /// </summary>
        public int CurrentBar { get; set; } = 1;

        /// <summary>
        /// Design groove track (initialised to avoid null checks).
        /// </summary>
        public GrooveTrack GrooveTrack { get; set; } = new();

        // Harmony timeline persisted with the design (optional)
        public HarmonyTrack? HarmonyTrack { get; set; }

        public SectionTrack SectionTrack { get; set; } = new();

        /// <summary>
        /// Design-space Song object (template).
        /// </summary>
        public Song Song { get; set; } = new();

        public TempoTrack? TempoTrack { get; set; }

        public TimeSignatureTrack? TimeSignatureTrack { get; set; }

        // Design space
        public VoiceSet Voices { get; set; } = new();
    }
}