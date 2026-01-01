// AI: purpose=Track of contiguous song Sections; Add auto-assigns StartBar and _nextBar tracks next free bar.
// AI: invariants=Sections ordered and contiguous; StartBar is 1-based; TotalBars == _nextBar - 1.
// AI: deps=Used by arrangers, tests, and exporters; changing StartBar logic or names breaks serialization/tests.
// AI: change=If supporting gaps or fractional bars update Add, GetActiveSection, and consumers that assume contiguity.

namespace Music.Generator
{
    // AI: design=lightweight mutable track; keep behavior minimal here and move complex rules to builders/tests.
    public class SectionTrack
    {
        public List<Section> Sections { get; set; } = new();


        private int _nextBar = 1;

        // AI: Reset clears sections and resets next bar to 1; callers may reuse same SectionTrack instance.
        public void Reset()
        {
            Sections.Clear();
            _nextBar = 1;
        }

        // AI: Add: creates a Section with StartBar=_nextBar; enforces BarCount>=1 and advances _nextBar by BarCount.
        // AI: note=Does NOT check overlaps or merge sections; callers must ensure semantics if needed.
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

        // AI: GetActiveSection: returns true and the section that contains the bar, false if bar<1 or no section covers it.
        // AI: edge=bar must be >=1; method walks from end to find latest StartBar <= bar (assumes ordered Sections).
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

        // AI: TotalBars: computed as next free bar - 1; reflects last bar index covered by Sections.
        public int TotalBars => _nextBar - 1;

    }
}