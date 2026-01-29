// AI: purpose=Type alias for BarContext providing drum-specific naming; maintains backward compatibility.
// AI: invariants=Alias only, no behavior change; DrumBarContext and BarContext are identical types.
// AI: change=Story 5.2: Renamed from DrumBarContext, moved to Drums namespace, removed SegmentGrooveProfile.

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Drum-specific naming for bar context. This is a type alias for BarContext.
    /// Provides per-bar context for drum system: section position and phrase awareness.
    /// Story 5.2: Moved from Groove namespace, simplified by removing section-specific configuration.
    /// </summary>
    public sealed record DrumBarContext(
        int BarNumber,
        Section? Section,
        int BarWithinSection,
        int BarsUntilSectionEnd)
    {
        /// <summary>
        /// Convert from generic BarContext to DrumBarContext (same structure).
        /// </summary>
        public static DrumBarContext FromBarContext(BarContext barContext)
        {
            return new DrumBarContext(
                barContext.BarNumber,
                barContext.Section,
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
                BarWithinSection,
                BarsUntilSectionEnd);
        }
    }
}
