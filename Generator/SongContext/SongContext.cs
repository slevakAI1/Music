using Music.Generator;

namespace Music.Designer
{
    /// <summary>
    /// Design structure for a song.
    /// </summary>
    public sealed class SongContext
    {
        public SongContext()
        {
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
        public HarmonyTrack HarmonyTrack { get; set; } = new();

        public SectionTrack SectionTrack { get; set; } = new();

        /// <summary>
        /// Song being generated
        /// </summary>
        public Song Song { get; set; } = new();

        //public TempoTrack TempoTrack { get; set; } = new();

        //public TimeSignatureTrack TimeSignatureTrack { get; set; } = new();

        // Design space
        public VoiceSet Voices { get; set; } = new();
    }
}