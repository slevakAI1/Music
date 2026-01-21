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

        /// <summary>
        /// Creates provenance for an anchor-layer onset.
        /// Story G2: Anchors have Source=Anchor with null GroupId/CandidateId.
        /// </summary>
        /// <returns>Provenance record for an anchor onset.</returns>
        public static GrooveOnsetProvenance ForAnchor()
        {
            return new GrooveOnsetProvenance
            {
                Source = GrooveOnsetSource.Anchor,
                GroupId = null,
                CandidateId = null,
                TagsSnapshot = null
            };
        }

        /// <summary>
        /// Creates provenance for a variation-selected onset.
        /// Story G2: Variations have Source=Variation with GroupId and CandidateId populated.
        /// </summary>
        /// <param name="groupId">Group ID from GrooveCandidateGroup.GroupId.</param>
        /// <param name="candidateId">Candidate ID (format: "{groupId}:{beat:F2}").</param>
        /// <param name="enabledTags">Enabled tags at selection time (null if unavailable).</param>
        /// <returns>Provenance record for a variation onset.</returns>
        public static GrooveOnsetProvenance ForVariation(
            string groupId,
            string candidateId,
            IReadOnlyList<string>? enabledTags = null)
        {
            return new GrooveOnsetProvenance
            {
                Source = GrooveOnsetSource.Variation,
                GroupId = groupId,
                CandidateId = candidateId,
                TagsSnapshot = enabledTags
            };
        }

        /// <summary>
        /// Creates a stable candidate ID from group ID and beat position.
        /// Uses same format as GrooveDiagnosticsCollector.MakeCandidateId for consistency.
        /// </summary>
        /// <param name="groupId">Group identifier.</param>
        /// <param name="beat">Beat position of the candidate.</param>
        /// <returns>Stable candidate identifier string.</returns>
        public static string MakeCandidateId(string groupId, decimal beat)
        {
            return $"{groupId}:{beat:F2}";
        }
    }
}
