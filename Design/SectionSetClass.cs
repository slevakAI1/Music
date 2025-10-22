using System.Collections.Generic;

namespace Music.Design
{
    // Score Sections
    public class SectionSetClass
    {
        public List<SectionClass> _sections = new();
        public IReadOnlyList<SectionClass> Sections => _sections;

        private int _nextBar = 1;

        public void Reset()
        {
            _sections.Clear();
            _nextBar = 1;
        }

        // Back-compat: default to 4 bars when not specified
        public void Add(DesignEnums.eSectionType sectionType)
            => Add(sectionType, barCount: 4, name: null);

        // New overload: specify length and optional name
        public void Add(DesignEnums.eSectionType sectionType, int barCount, string? name = null)
        {
            var section = new SectionClass
            {
                SectionType = sectionType,
                BarCount = barCount > 0 ? barCount : 1,
                StartBar = _nextBar,
                Name = name
            };

            _sections.Add(section);
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
            foreach (var s in _sections)
            {
                if (s.ContainsBar(bar))
                    return s;
            }
            return null;
        }

        // Recompute StartBar after external edits to BarCount or ordering
        public void RecalculateStarts()
        {
            _nextBar = 1;
            for (int i = 0; i < _sections.Count; i++)
            {
                var s = _sections[i];
                s.StartBar = _nextBar;
                _nextBar += s.BarCount > 0 ? s.BarCount : 1;
            }
        }
    }
}