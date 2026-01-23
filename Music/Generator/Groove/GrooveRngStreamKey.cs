// AI: purpose=Define canonical RNG stream keys for groove system deterministic randomness from Story A2.
// AI: invariants=Enum values stable; adding new values OK but never reorder/rename/remove existing ones.
// AI: deps=Used by GrooveRngHelper for deriving stable per-bar per-role RNG instances.
// AI: change=Story A2 acceptance criteria: complete list covering all groove phases + future drummer hooks.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Canonical RNG stream keys for groove system.
    /// Story A2: Each key represents a distinct random use case with its own deterministic sequence.
    /// Ensures same inputs + seeds => identical output across all groove phases.
    /// </summary>
    /// <remarks>
    /// CRITICAL: Never reorder, rename, or remove enum values. Only append new values at the end.
    /// Changing order breaks determinism for existing songs.
    /// </remarks>
    public enum GrooveRngStreamKey
    {
        // ===== Phase B: Variation Engine =====
        
        /// <summary>
        /// Selecting which variation group to use when multiple groups match enabled tags.
        /// Used in: Story B3 - Weighted Candidate Selection
        /// </summary>
        VariationGroupPick,

        /// <summary>
        /// Selecting individual candidates within a variation group.
        /// Used in: Story B3 - Weighted Candidate Selection
        /// </summary>
        CandidatePick,

        /// <summary>
        /// Breaking ties when candidates have identical weights after probability bias calculation.
        /// Used in: Story B3 - Weighted Candidate Selection (deterministic tie-breaking)
        /// </summary>
        TieBreak,

        // ===== Phase C: Density & Caps =====

        /// <summary>
        /// Selecting which onsets to prune when hard caps are exceeded.
        /// Used in: Story C3 - Enforce Hard Caps (when ties remain after stable sorting)
        /// </summary>
        PrunePick,

        /// <summary>
        /// Random selection when density targets require probabilistic choice.
        /// Used in: Story C2 - Select Until Target Reached
        /// </summary>
        DensityPick,

        // ===== Phase D: Velocity Shaping =====

        /// <summary>
        /// Adding small random variations to computed velocities for human realism.
        /// Used in: Story D2 - Velocity Shaping (optional jitter within VelocityRule bounds)
        /// </summary>
        VelocityJitter,

        /// <summary>
        /// Random selection for ghost note velocity variations.
        /// Used in: Story D2 - Velocity Shaping (ghost notes)
        /// </summary>
        GhostVelocityPick,

        // ===== Phase E: Timing & Feel =====

        /// <summary>
        /// Adding small random timing offsets for human realism (micro-timing variations).
        /// Used in: Story E2 - Role Timing Feel + Bias + Clamp
        /// </summary>
        TimingJitter,

        /// <summary>
        /// Random variations in swing amount within allowed range.
        /// Used in: Story E1 - Feel Timing (when swing amount has tolerance)
        /// </summary>
        SwingJitter,

        // ===== Future: Drummer Policy Hooks (reserved) =====

        /// <summary>
        /// Selecting fill patterns when multiple fills are valid for a phrase-end window.
        /// Reserved for: Future drummer policy (NEXT EPIC)
        /// </summary>
        FillPick,

        /// <summary>
        /// Random accent placement for humanization (beyond computed strength classification).
        /// Reserved for: Future drummer policy (NEXT EPIC)
        /// </summary>
        AccentPick,

        /// <summary>
        /// Random ghost note addition (decorative hits at low velocity).
        /// Reserved for: Future drummer policy (NEXT EPIC)
        /// </summary>
        GhostNotePick,

        /// <summary>
        /// Random flam/drag/roll ornament application.
        /// Reserved for: Future drummer policy (NEXT EPIC)
        /// </summary>
        OrnamentPick,

        /// <summary>
        /// Random cymbal choice when multiple cymbal options valid (crash vs ride).
        /// Reserved for: Future drummer policy (NEXT EPIC)
        /// </summary>
        CymbalPick,

        /// <summary>
        /// Random dynamics variation at phrase/section level.
        /// Reserved for: Future drummer policy (NEXT EPIC)
        /// </summary>
        DynamicsPick
    }
}
