namespace Music.Designer
{
    // Score Sections
    public class SectionTimelineClass
    {
        public List<SectionClass> Sections { get; set; } = new();

        private int _nextBar = 1;

        public void Reset()
        {
            Sections.Clear();
            _nextBar = 1;
        }

        // Back-compat: default to 4 bars when not specified
        public void Add(MusicConstants.eSectionType sectionType)
            => Add(sectionType, barCount: 4, name: null);

        // New overload: specify length and optional name
        public void Add(MusicConstants.eSectionType sectionType, int barCount, string? name = null)
        {
            var section = new SectionClass
            {
                SectionType = sectionType,
                BarCount = barCount > 0 ? barCount : 1,
                StartBar = _nextBar,
                Name = name
            };

            Sections.Add(section);
            _nextBar += section.BarCount;
        }

        // Total bars in the arrangement
        public int TotalBars => _nextBar - 1;

        // Find the section that contains the given bar (1-based), or null if none
        public SectionClass? FindByBar(int bar)
        {
            if (bar < 1) return null;
            // Sections are appended in time order, so linear scan is fine.
            // If you expect many sections, consider binary search.
            foreach (var s in Sections)
            {
                if (s.ContainsBar(bar))
                    return s;
            }
            return null;
        }

        /// <summary>
        /// Determines if a bar number falls within any of the selected sections.
        /// If no section names are provided, returns true (no filtering).
        /// </summary>
        /// <param name="barNumber">The bar number to check (1-based).</param>
        /// <param name="selectedSectionNames">List of section names to filter by.</param>
        /// <returns>True if the bar is in a selected section or if no sections are selected.</returns>
        public bool IsBarInSelectedSections(int barNumber, List<string> selectedSectionNames)
        {
            // If no sections selected, don't filter (process all bars)
            if (selectedSectionNames == null || selectedSectionNames.Count == 0)
                return true;

            // If no sections defined, process all bars
            if (Sections == null || Sections.Count == 0)
                return true;

            // Check if bar falls within any selected section
            foreach (var sectionName in selectedSectionNames)
            {
                var section = Sections
                    .FirstOrDefault(s => s?.Name?.Equals(sectionName, StringComparison.OrdinalIgnoreCase) == true);

                if (section != null && section.ContainsBar(barNumber))
                {
                    return true;
                }
            }

            return false;
        }

        // Recompute StartBar after external edits to BarCount or ordering
        public void RecalculateStarts()
        {
            _nextBar = 1;
            for (int i = 0; i < Sections.Count; i++)
            {
                var s = Sections[i];
                s.StartBar = _nextBar;
                _nextBar += s.BarCount > 0 ? s.BarCount : 1;
            }
        }

        // Preserve StartBar values and only sync internal next-bar based on existing sections
        public void SyncAfterExternalLoad()
        {
            if (Sections.Count == 0)
            {
                _nextBar = 1;
                return;
            }

            int lastEnd = 0;
            foreach (var s in Sections)
            {
                if (s == null) continue;
                int start = s.StartBar > 0 ? s.StartBar : 1;
                int len = s.BarCount > 0 ? s.BarCount : 1;
                int end = start + len - 1;
                if (end > lastEnd) lastEnd = end;
            }
            _nextBar = lastEnd + 1;
        }
    }
}