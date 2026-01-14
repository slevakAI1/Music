// AI: purpose=Create canonical 64-bar section layout used by tests and demos (Intro→Verse→Chorus→...→Outro).
// AI: invariants=Durations sum to TestDesigns.TotalBars (48); sections ordered and contiguous; names and types expected by fixtures.
// AI: deps=Relies on SectionTrack.Add(type,barCount,name) and MusicConstants.eSectionType values; changing names breaks tests.
// AI: change=If altering structure or totals, update all tests and any hard-coded expectations.

namespace Music.Generator
{
    public sealed class SectionTests
    {
        // AI: SetTestSectionsD1 builds the standard section sequence; durations chosen to total 48 bars.
        public void SetTestSectionsD1(SectionTrack sections)
        {
            sections.Reset();

            // Durations sum to 64 bars to align with default tracks
            sections.Add(MusicConstants.eSectionType.Intro, 8, "Intro");
            sections.Add(MusicConstants.eSectionType.Verse, 8, "Verse 1");
            sections.Add(MusicConstants.eSectionType.Chorus, 8, "Chorus 1");
            sections.Add(MusicConstants.eSectionType.Verse, 8, "Verse 2");
            sections.Add(MusicConstants.eSectionType.Chorus, 8, "Chorus 2");
            sections.Add(MusicConstants.eSectionType.Bridge, 8, "Bridge 1");
            sections.Add(MusicConstants.eSectionType.Chorus, 8, "Chorus 3");
            sections.Add(MusicConstants.eSectionType.Outro, 8, "Outro");
        }
    }
}