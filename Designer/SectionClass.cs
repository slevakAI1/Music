namespace Music.Designer
{

    // Current plan is to limit sections to whole bars. Will try to deal with lead ins and
    // lead outs in the section generators

    public class SectionClass
    {
        // Existing
        public MusicConstants.eSectionType SectionType { get; set; }

        // Placement and size in bars
        // StartBar is 1-based
        public int StartBar { get; set; }

        // Defaults to 4 bars unless specified at add time
        public int BarCount { get; set; } = 4;

        // Optional: friendly label (e.g., "Chorus A", "Verse 2")
        public string? Name { get; set; }

        // Optional: stable ID if you later want to map to a reusable harmonic pattern
        public Guid Id { get; } = Guid.NewGuid();

        // Helper: returns true if this section spans the given bar
        public bool ContainsBar(int bar) => bar >= StartBar && bar < StartBar + BarCount;

        // Helper: 0-based bar index within this section (or -1 if not in this section)
        public int GetLocalBarIndex(int bar) => ContainsBar(bar) ? (bar - StartBar) : -1;
    }
}
