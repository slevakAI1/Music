namespace Music.Generator
{
    /// <summary>
    /// The complete generated song, containing all tracks and temporal data.
    /// This is the runtime representation of a composed piece, separate from design templates.
    /// </summary>
    public sealed class Song
    {
        /// <summary>
        /// Global tempo track for the song.
        /// </summary>
        public SongTempoTrack TempoTrack { get; set; }

        /// <summary>
        /// Global time signature track for the song.
        /// </summary>
        public SongTimeSignatureTrack TimeSignatureTrack { get; set; }

        /// <summary>
        /// All part/instrument tracks in the song.
        /// </summary>
        public List<SongPartTrack> PartTracks { get; set; }

        /// <summary>
        /// Total number of bars in the song.
        /// </summary>
        public int TotalBars { get; set; }

        /// <summary>
        /// Ticks per quarter note (standard: 480).
        /// </summary>
        public int TicksPerQuarterNote { get; init; }

        public Song()
        {
            TempoTrack = new SongTempoTrack();
            TimeSignatureTrack = new SongTimeSignatureTrack();
            PartTracks = new List<SongPartTrack>();
        }
    }
}