namespace Music.Generator
{
    /// <summary>
    /// The complete generated song, containing all tracks and temporal data.
    /// This is the runtime representation of a composed piece, separate from design templates.
    /// </summary>
    public sealed class ProposedSong
    {
        /// <summary>
        /// Unique identifier for this song instance.
        /// </summary>
        public string SongId { get; init; }

        /// <summary>
        /// Reference to the design that was used to generate this song (optional).
        /// </summary>
        public string? SourceDesignId { get; init; }

        // LEVAK NOTE ONLY - this has an issue... what if specific tracks/pieces are replaced using a new seed -
        //      this is not a global key situation

        /// <summary>
        /// The variation key/seed used during generation for reproducibility.
        /// </summary>
        public int VariationKey { get; set; }

        /// <summary>
        /// Global tempo track for the song.
        /// </summary>
        public ProposedSongTempoTrack TempoTrack { get; set; }

        /// <summary>
        /// Global time signature track for the song.
        /// </summary>
        public ProposedSongTimeSignatureTrack TimeSignatureTrack { get; set; }

        /// <summary>
        /// Harmony track reflecting the actual chords used in the song.
        /// </summary>
        public ProposedSongHarmonyTrack HarmonyTrack { get; set; }

        /// <summary>
        /// Section layout of the song (verse, chorus, etc. with bar positions).
        /// </summary>
        public ProposedSongSectionTrack SectionTrack { get; set; }

        /// <summary>
        /// All part/instrument tracks in the song.
        /// </summary>
        public List<ProposedSongPartTrack> PartTracks { get; set; }

        /// <summary>
        /// Total number of bars in the song.
        /// </summary>
        public int TotalBars { get; set; }

        /// <summary>
        /// Ticks per quarter note (standard: 480).
        /// </summary>
        public int TicksPerQuarterNote { get; init; }

        public ProposedSong()
        {
            SongId = Guid.NewGuid().ToString("N");
            TicksPerQuarterNote = 480;
            TempoTrack = new ProposedSongTempoTrack();
            TimeSignatureTrack = new ProposedSongTimeSignatureTrack();
            HarmonyTrack = new ProposedSongHarmonyTrack();
            SectionTrack = new ProposedSongSectionTrack();
            PartTracks = new List<ProposedSongPartTrack>();
        }
    }
}