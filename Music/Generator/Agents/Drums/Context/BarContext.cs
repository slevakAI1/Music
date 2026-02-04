// AI: purpose=Drum-specific bar context for song structure awareness; provides section position and phrase information.
// AI: invariants=BarNumber is 1-based; BarWithinSection is 0-based; BarsUntilSectionEnd >= 0.
// AI: deps=Section from SectionTrack; built by DrumBarContextBuilder.

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Provides per-bar context for drum generator: section position and phrase awareness.
    /// Story 5.2: Simplified from Groove.BarContext, removed section-specific configuration.
    /// </summary>
    public sealed record BarContext(
        int BarNumber,
        Section? Section,
        int BarWithinSection,
        int BarsUntilSectionEnd);
}
