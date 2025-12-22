using Music.Generator;

namespace Music.Designer
{
    /// <summary>
    /// Design structure for a song
    /// </summary>
    public sealed class SongContext
    {
        public SongContext(string? designId = null)
        {
            Song = new Song();  // Future use
            GrooveTrack = new GrooveTrack();
            CurrentBar = 1;  // Future use
        }

        int CurrentBar { get; set; } = 0; // Future use

        public GrooveTrack? GrooveTrack { get; set; }

        // Harmony timeline persisted with the design
        public HarmonyTrack? HarmonyTrack { get; set; }

        public SectionTrack SectionTrack { get; set; } = new();

        public Song Song { get; set; }  // Future use

        public TempoTrack? TempoTrack { get; set; }

        public TimeSignatureTrack? TimeSignatureTrack { get; set; }

        // Design Space
        public VoiceSet Voices { get; set; } = new();
    }
}