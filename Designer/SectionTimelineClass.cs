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