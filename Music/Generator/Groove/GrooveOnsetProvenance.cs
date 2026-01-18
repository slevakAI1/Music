// AI: purpose=Tracks origin and metadata for groove onsets (Story G2).
// AI: invariants=Source required; GroupId/CandidateId nullable; does not affect output determinism.
// AI: usage=Attached to GrooveOnset.Provenance for explainability and debugging.

namespace Music.Generator
{
    /// <summary>
    /// Source type for a groove onset.
    /// </summary>
    public enum GrooveOnsetSource
    {
        /// <summary>
        /// Onset came from the anchor layer (base pattern).
        /// </summary>
        Anchor,

        /// <summary>
        /// Onset came from variation selection (candidate added).
        /// </summary>
        Variation
    }

    /// <summary>
    /// Provenance information for a groove onset.
    /// Story G2: Tracks where each onset came from for explainability.
    /// </summary>
    public sealed record GrooveOnsetProvenance
    {
        /// <summary>
        /// Source of this onset (Anchor or Variation).
        /// </summary>
        public required GrooveOnsetSource Source { get; init; }

        /// <summary>
        /// Group ID if this onset came from a candidate group (variation only).
        /// Nullable for anchors.
        /// </summary>
        public string? GroupId { get; init; }

        /// <summary>
        /// Candidate ID if this onset came from a specific candidate (variation only).
        /// Nullable for anchors.
        /// </summary>
        public string? CandidateId { get; init; }

        /// <summary>
        /// Snapshot of tags active when this onset was selected (optional).
        /// Useful for understanding why a candidate was chosen.
        /// </summary>
        public IReadOnlyList<string>? TagsSnapshot { get; init; }
    }
}
