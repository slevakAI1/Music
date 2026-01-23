// AI: purpose=Per-bar coverage state for PartTrack analysis; precedence: Locked > HasContent > Empty.
// AI: deps=Used by PartTrackBarCoverageAnalyzer; consumers use for generation gating (fill-only-empty, respect-locked).

namespace Music.Generator.Groove
{
    // AI: Enum values ordered by precedence for clarity; do not reorder without updating precedence logic.
    public enum BarFillState
    {
        Empty = 0,       // No events intersect this bar
        HasContent = 1,  // At least one event intersects this bar
        Locked = 2       // User-set "do not modify" flag (highest precedence)
    }
}
