// AI: purpose=Builds bar context list from song structure; maps bar → section and phrase position.
// AI: deps=SectionTrack.GetActiveSection; returns immutable list.
// AI: change=Story 5.2: Moved from Groove namespace, removed segment profile resolution (section-awareness now in DrummerPolicyProvider).

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Builds drum-specific bar contexts for all bars in song.
    /// Story 5.2: Simplified from Groove.BarContextBuilder, removed segment profile logic.
    /// </summary>
    public static class DrumBarContextBuilder
    {
        /// <summary>
        /// Builds per-bar context list mapping bars to sections and phrase positions.
        /// Story 5.2: Removed segment profile parameter - section-aware density now handled by DrummerPolicyProvider.
        /// </summary>
        /// <param name="sectionTrack">Song section structure.</param>
        /// <param name="totalBars">Total number of bars in song.</param>
        /// <returns>Immutable list of bar contexts, one per bar (1-indexed).</returns>
        public static IReadOnlyList<BarContext> Build(
            SectionTrack sectionTrack,
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

                contexts.Add(new BarContext(
                    BarNumber: bar,
                    Section: section,
                    BarWithinSection: barWithinSection,
                    BarsUntilSectionEnd: barsUntilSectionEnd));
            }

            return contexts;
        }
    }
}
