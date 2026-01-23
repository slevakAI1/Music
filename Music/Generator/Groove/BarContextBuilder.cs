// AI: purpose=Builds bar context list from song structure; maps bar → section, phrase position, segment profile.
// AI: deps=SectionTrack.GetActiveSection; segment profiles from song context; returns immutable list.
// AI: change=Keep logic identical to DrumTrackGeneratorNew.BuildBarContexts for determinism (Story G1 requirement).

namespace Music.Generator.Groove
{
    /// <summary>
    /// Builds generator-agnostic bar contexts for all bars in song.
    /// Extracted from DrumTrackGeneratorNew to enable reuse across generators.
    /// </summary>
    public static class BarContextBuilder
    {
        /// <summary>
        /// Builds per-bar context list mapping bars to sections, phrase positions, and segment profiles.
        /// </summary>
        /// <param name="sectionTrack">Song section structure.</param>
        /// <param name="segmentProfiles">Segment groove profiles with bar ranges.</param>
        /// <param name="totalBars">Total number of bars in song.</param>
        /// <returns>Immutable list of bar contexts, one per bar (1-indexed).</returns>
        public static IReadOnlyList<BarContext> Build(
            SectionTrack sectionTrack,
            IReadOnlyList<SegmentGrooveProfile> segmentProfiles,
            int totalBars)
        {
            var contexts = new List<BarContext>(totalBars);

            for (int bar = 1; bar <= totalBars; bar++)
            {
                // Map bar to section
                sectionTrack.GetActiveSection(bar, out var section);

                // Calculate phrase position within section
                int barWithinSection = 0;
                int barsUntilSectionEnd = 0;

                if (section != null)
                {
                    barWithinSection = bar - section.StartBar;
                    int sectionEndBar = section.StartBar + section.BarCount - 1;
                    barsUntilSectionEnd = sectionEndBar - bar;
                }

                // Resolve segment profile for this bar
                SegmentGrooveProfile? segmentProfile = null;
                foreach (var profile in segmentProfiles)
                {
                    bool inRange = true;

                    if (profile.StartBar.HasValue && bar < profile.StartBar.Value)
                        inRange = false;

                    if (profile.EndBar.HasValue && bar > profile.EndBar.Value)
                        inRange = false;

                    if (inRange)
                    {
                        segmentProfile = profile;
                        break; // Use first matching profile
                    }
                }

                contexts.Add(new BarContext(
                    BarNumber: bar,
                    Section: section,
                    SegmentProfile: segmentProfile,
                    BarWithinSection: barWithinSection,
                    BarsUntilSectionEnd: barsUntilSectionEnd));
            }

            return contexts;
        }
    }
}
