// AI: purpose=Generator-agnostic bar context for song structure awareness; shared by all generators (drums, comp, melody, motifs).
// AI: invariants=BarNumber is 1-based; BarWithinSection is 0-based; BarsUntilSectionEnd >= 0.
// AI: deps=Section from SectionTrack; SegmentGrooveProfile from song context; built by BarContextBuilder.
// AI: change=Add fields only if needed by multiple generators; keep minimal to avoid coupling.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Provides per-bar context for generators: section position, phrase awareness, segment profile.
    /// Replaces drum-specific DrumBarContext with generator-agnostic equivalent.
    /// </summary>
    public sealed record BarContext(
        int BarNumber,
        Section? Section,
        SegmentGrooveProfile? SegmentProfile,
        int BarWithinSection,
        int BarsUntilSectionEnd);
}
