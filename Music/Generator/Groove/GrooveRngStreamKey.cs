// AI: purpose=Define canonical RNG stream keys for groove system deterministic randomness.
// AI: invariants=Enum values stable; adding new values OK but never reorder/rename/remove existing ones.
// AI: deps=Used by GrooveRngHelper for deriving stable per-bar per-role RNG instances.
// AI: change=Only groove-specific operations

namespace Music.Generator.Groove
{
    /// <summary>
    /// Canonical RNG stream keys for groove system.
    /// Each key represents a distinct random use case with its own deterministic sequence.
    /// Ensures same inputs + seeds => identical output for timing, feel, and general weighted selection.
    /// </summary>
    /// <remarks>
    /// CRITICAL: Never reorder, rename, or remove enum values. Only append new values at the end.
    /// Changing order breaks determinism for existing songs.
    /// Groove system handles only timing/feel/position concerns. 
    /// </remarks>
    public enum GrooveRngStreamKey
    {
        /// <summary>
        /// General weighted candidate selection with tie-breaking.
        /// Used by: Drummer Agent's DrumWeightedCandidateSelector_Save for selecting from weighted candidates.
        /// </summary>
        CandidatePick,

        /// <summary>
        /// Breaking ties when items have identical weights after scoring.
        /// Used for: Deterministic tie-breaking in weighted selection algorithms.
        /// </summary>
        TieBreak,

        /// <summary>
        /// Adding small random variations to computed velocities for human realism.
        /// Used for: Velocity jitter within velocity rule bounds for natural feel.
        /// </summary>
        VelocityJitter,

        /// <summary>
        /// Adding small random timing offsets for human realism (micro-timing variations).
        /// Used for: Role timing feel with bias and clamp for natural timing drift.
        /// </summary>
        TimingJitter,

        /// <summary>
        /// Random variations in swing amount within allowed range.
        /// Used for: Feel timing when swing amount has tolerance for natural groove.
        /// </summary>
        SwingJitter
    }
}
