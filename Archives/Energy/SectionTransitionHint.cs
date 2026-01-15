// AI: purpose=Enum for section boundary transition feel (Build/Release/Sustain/Drop) used by Stage 8+ arrangers.
// AI: invariants=Derived from (energyDelta, tensionDelta, section types); must be deterministic; used for motif placement.
// AI: deps=Computed by DeterministicTensionQuery; consumed by future Stage 8 phrase maps and Stage 9 motif placement.

namespace Music.Generator
{
    /// <summary>
    /// Describes the emotional/musical transition feel at a section boundary.
    /// Derived deterministically from energy delta, tension delta, and section types.
    /// Used by later stages (motifs, melody, arrangement ducking) to make contextual decisions.
    /// </summary>
    public enum SectionTransitionHint
    {
        /// <summary>No transition (e.g., first section, or no clear direction).</summary>
        None = 0,

        /// <summary>Building tension/energy toward next section (anticipatory).</summary>
        Build = 1,

        /// <summary>Releasing tension/energy (resolution moment, drop).</summary>
        Release = 2,

        /// <summary>Maintaining current energy/tension level (continuity).</summary>
        Sustain = 3,

        /// <summary>Sudden energy/tension drop (breakdown, EDM drop).</summary>
        Drop = 4
    }
}
