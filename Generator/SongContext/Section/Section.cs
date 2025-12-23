namespace Music.Generator
{
    // This class represents one section of a song. A song is composed of a List<Section>.
    // The sections are assumed to be in order.
    // The first section is assumed to start at bar 1, beat 1.
    // Each section in a song is assumed to start at the next bar/beat immediately after the
    //   previous section.
    // Section durations (BarCount) are currently limited to whole bars.

    public class Section
    {
        // Event Data
        public MusicConstants.eSectionType SectionType { get; set; }

        public string? Name { get; set; }

        // StartBar is 1-based
        public int StartBar { get; set; }

        // For data entry assist only
        public int BarCount { get; set; } = 4;
    }
}
