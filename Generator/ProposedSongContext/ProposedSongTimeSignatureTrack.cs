namespace Music.Generator
{
    /// <summary>
    /// Time signature events for the song timeline.
    /// </summary>
    public sealed class ProposedSongTimeSignatureTrack
    {
        /// <summary>
        /// Ordered list of time signature events.
        /// </summary>
        public List<SongTimeSignatureEvent> Events { get; set; }

        public ProposedSongTimeSignatureTrack()
        {
            Events = new List<SongTimeSignatureEvent>();
        }
    }

    /// <summary>
    /// A single time signature event in the song.
    /// </summary>
    public sealed class SongTimeSignatureEvent
    {
        /// <summary>
        /// Bar number where this time signature starts (1-based).
        /// </summary>
        public int StartBar { get; set; }

        /// <summary>
        /// Numerator (beats per bar).
        /// </summary>
        public int Numerator { get; set; }

        /// <summary>
        /// Denominator (beat unit, e.g., 4 = quarter note).
        /// </summary>
        public int Denominator { get; set; }

        /// <summary>
        /// Absolute position in ticks.
        /// </summary>
        public long AbsolutePositionTicks { get; set; }

        public SongTimeSignatureEvent()
        {
            StartBar = 1;
            Numerator = 4;
            Denominator = 4;
        }
    }
}