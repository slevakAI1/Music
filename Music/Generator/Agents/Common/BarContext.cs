// AI: purpose=Canonical per-bar context shared across agents; contains structural info for groove/drums.
// AI: invariants=BarNumber is 1-based; BarWithinSection is 0-based; BarsUntilSectionEnd >= 0.
// AI: built-by=DrumBarContextBuilder (Drums.Context); do not add behaviour here.
// AI: change=Replaced DrumBarContext alias; use BarContext as single immutable DTO for bar info.

namespace Music.Generator.Agents.Common
{
    // Provides per-bar context - section position and phrase position.
    // Purpose: small immutable DTO to transport bar metadata (section, phrase offsets). Keep behavior out.
    public sealed record BarContext(
        int BarNumber,
        Section? Section,
        int BarWithinSection,
        int BarsUntilSectionEnd);
}
