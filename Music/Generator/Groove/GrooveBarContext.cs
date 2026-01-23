// AI: purpose=Type alias for BarContext providing groove-specific naming; maintains backward compatibility with Story A1 requirements.
// AI: invariants=Alias only, no behavior change; GrooveBarContext and BarContext are identical types.
// AI: change=Story A1 acceptance criteria allows reuse of existing BarContext as GrooveBarContext.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Groove-specific naming for bar context. This is a type alias for BarContext.
    /// Provides per-bar context for groove system: section position, phrase awareness, segment profile.
    /// Story A1: Define stable groove output types for instrument-agnostic generation.
    /// </summary>
    public sealed record GrooveBarContext(
        int BarNumber,
        Section? Section,
        SegmentGrooveProfile? SegmentProfile,
        int BarWithinSection,
        int BarsUntilSectionEnd)
    {
        /// <summary>
        /// Convert from generic BarContext to GrooveBarContext (same structure).
        /// </summary>
        public static GrooveBarContext FromBarContext(BarContext barContext)
        {
            return new GrooveBarContext(
                barContext.BarNumber,
                barContext.Section,
                barContext.SegmentProfile,
                barContext.BarWithinSection,
                barContext.BarsUntilSectionEnd);
        }

        /// <summary>
        /// Convert to generic BarContext (same structure).
        /// </summary>
        public BarContext ToBarContext()
        {
            return new BarContext(
                BarNumber,
                Section,
                SegmentProfile,
                BarWithinSection,
                BarsUntilSectionEnd);
        }
    }
}
