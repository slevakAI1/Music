namespace Music.Generator
{
    /// <summary>
    /// Tempo events for the song timeline.
    /// </summary>
    public sealed class SongTempoTrack
    {
        /// <summary>
        /// Ordered list of tempo events.
        /// </summary>
        public List<SongTempoEvent> Events { get; set; }

        public SongTempoTrack()
        {
            Events = new List<SongTempoEvent>();
        }
    }

    /// <summary>
    /// A single tempo event in the song.
    /// </summary>
    public sealed class SongTempoEvent
    {
        /// <summary>
        /// Bar number where this tempo starts (1-based).
        /// </summary>
        public int StartBar { get; set; }

        /// <summary>
        /// Beat within the bar (1-based).
        /// </summary>
        public int StartBeat { get; set; }

        /// <summary>
        /// Tempo in beats per minute.
        /// </summary>
        public int TempoBpm { get; set; }

        /// <summary>
        /// Absolute position in ticks.
        /// </summary>
        public long AbsolutePositionTicks { get; set; }

        public SongTempoEvent()
        {
            StartBar = 1;
            StartBeat = 1;
            TempoBpm = 120;
        }
    }
}