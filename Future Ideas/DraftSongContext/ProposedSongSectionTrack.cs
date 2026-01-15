namespace Music.Generator
{
    /// <summary>
    /// Section layout of the song.
    /// </summary>
    public sealed class ProposedSongSectionTrack
    {
        /// <summary>
        /// Ordered list of sections in the song.
        /// </summary>
        public List<SongSection> Sections { get; set; }

        public ProposedSongSectionTrack()
        {
            Sections = new List<SongSection>();
        }
    }

    /// <summary>
    /// A single section in the song (verse, chorus, bridge, etc.).
    /// </summary>
    public sealed class SongSection
    {
        /// <summary>
        /// Section type.
        /// </summary>
        public SectionType SectionType { get; set; }

        /// <summary>
        /// Optional custom name (e.g., "Verse 1", "Chorus 2").
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Start bar (1-based).
        /// </summary>
        public int StartBar { get; set; }

        /// <summary>
        /// Number of bars in this section.
        /// </summary>
        public int BarCount { get; set; }

        /// <summary>
        /// ID of the pattern that was used to generate this section (for reuse tracking).
        /// </summary>
        public string? SourcePatternId { get; set; }

        public SongSection()
        {
            SectionType = SectionType.Verse;
            StartBar = 1;
            BarCount = 4;
        }
    }

    /// <summary>
    /// Standard section types for song structure.
    /// </summary>
    public enum SectionType
    {
        Intro,
        Verse,
        PreChorus,
        Chorus,
        PostChorus,
        Bridge,
        Solo,
        Breakdown,
        Outro,
        Custom
    }
}