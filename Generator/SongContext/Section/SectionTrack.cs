namespace Music.Generator
{
    // This is a design track for song sections
    // The SectionTrack contains an ordered list of Sections that constitutes an entire song.

    public class SectionTrack
    {
        public List<Section> Sections { get; set; } = new();


        private int _nextBar = 1;

        public void Reset()
        {
            Sections.Clear();
            _nextBar = 1;
        }

        public void Add(MusicConstants.eSectionType sectionType, int barCount, string? name = null)
        {
            var section = new Section
            {
                SectionType = sectionType,
                BarCount = barCount > 0 ? barCount : 1,
                StartBar = _nextBar,
                Name = name
            };

            Sections.Add(section);
            _nextBar += section.BarCount;
        }

        /// <summary>
        /// Gets the active section at the specified bar.
        /// Returns the section that contains this bar.
        /// </summary>
        public bool GetActiveSection(int bar, out Section? section)
        {
            if (bar < 1)
            {
                section = null;
                return false;
            }

            for (int i = Sections.Count - 1; i >= 0; i--)
            {
                if (Sections[i].StartBar <= bar)
                {
                    section = Sections[i];
                    return true;
                }
            }

            section = null;
            return false;
        }

        // Total bars in the arrangement
        public int TotalBars => _nextBar - 1;

    }
}