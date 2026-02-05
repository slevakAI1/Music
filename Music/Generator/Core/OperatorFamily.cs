// AI: purpose=Classification enum for musical operators; groups operators by functional category across all instruments.
// AI: invariants=Enum values must remain stable (no reordering/renumbering) for determinism and serialization.
// AI: deps=Used by IMusicalOperator<T>.OperatorFamily; selection engine may weight by family.
// AI: change=Add new values at END only to preserve existing ordinals.

namespace Music.Generator.Core
{
    /// <summary>
    /// Classifies musical operators by functional category.
    /// Used across all instrument agents (Drums, Guitar, Keys, Bass, Vocals).
    /// </summary>
    public enum OperatorFamily
    {
        /// <summary>
        /// Small decorative additions (ghost notes, grace notes, subtle embellishments).
        /// Low density impact, high frequency of use.
        /// </summary>
        MicroAddition = 0,

        /// <summary>
        /// Rhythmic subdivision changes (double-time, half-time, triplet overlays).
        /// Transforms existing patterns to different rhythmic density.
        /// </summary>
        SubdivisionTransform = 1,

        /// <summary>
        /// Phrase boundary markers (fills, pickups, turnarounds, cadence figures).
        /// Typically applied at phrase/section ends.
        /// </summary>
        PhrasePunctuation = 2,

        /// <summary>
        /// Full pattern replacements (swap in a different groove/riff for a bar or phrase).
        /// High impact, low frequency of use.
        /// </summary>
        PatternSubstitution = 3,

        /// <summary>
        /// Style-specific idioms (genre-defining figures, signature licks, characteristic motions).
        /// Adds stylistic authenticity.
        /// </summary>
        StyleIdiom = 4
    }
}
