// AI: purpose=Factory for creating GrooveOnset records with proper provenance tracking (Story G2).
// AI: invariants=Anchor onsets get Source=Anchor; variation onsets get Source=Variation with GroupId/CandidateId.
// AI: deps=GrooveOnset, GrooveOnsetProvenance, GrooveOnsetCandidate, GrooveCandidateGroup.
// AI: change=Story G2: Centralized onset creation ensures provenance is always populated correctly.

namespace Music.Generator.Groove;

/// <summary>
/// Factory for creating GrooveOnset records with proper provenance tracking.
/// Story G2: Ensures provenance is consistently populated for all onset sources.
/// </summary>
public static class GrooveOnsetFactory
{
    /// <summary>
    /// Creates a GrooveOnset from an anchor layer beat position.
    /// Story G2: Sets Source=Anchor with null GroupId/CandidateId.
    /// </summary>
    /// <param name="role">Role name (e.g., "Kick", "Snare", "ClosedHat").</param>
    /// <param name="barNumber">Bar number (1-based).</param>
    /// <param name="beat">Beat position (1-based, can be fractional).</param>
    /// <param name="isMustHit">Whether this is a must-hit anchor (optional).</param>
    /// <param name="isNeverRemove">Whether this anchor cannot be removed (optional).</param>
    /// <param name="isProtected">Whether this anchor is protected from pruning (optional).</param>
    /// <returns>GrooveOnset with Anchor provenance.</returns>
    public static GrooveOnset FromAnchor(
        string role,
        int barNumber,
        decimal beat,
        bool isMustHit = false,
        bool isNeverRemove = false,
        bool isProtected = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        return new GrooveOnset
        {
            Role = role,
            BarNumber = barNumber,
            Beat = beat,
            Provenance = GrooveOnsetProvenance.ForAnchor(),
            IsMustHit = isMustHit,
            IsNeverRemove = isNeverRemove,
            IsProtected = isProtected
        };
    }

    /// <summary>
    /// Creates a GrooveOnset from a variation candidate selection.
    /// Story G2: Sets Source=Variation with GroupId, CandidateId, and optional TagsSnapshot.
    /// </summary>
    /// <param name="candidate">The selected candidate.</param>
    /// <param name="group">The group containing the candidate.</param>
    /// <param name="barNumber">Bar number (1-based).</param>
    /// <param name="enabledTags">Tags that were enabled at selection time (optional).</param>
    /// <returns>GrooveOnset with Variation provenance.</returns>
    public static GrooveOnset FromVariation(
        GrooveOnsetCandidate candidate,
        GrooveCandidateGroup group,
        int barNumber,
        IReadOnlyList<string>? enabledTags = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(group);

        string candidateId = GrooveOnsetProvenance.MakeCandidateId(group.GroupId, candidate.OnsetBeat);

        return new GrooveOnset
        {
            Role = candidate.Role,
            BarNumber = barNumber,
            Beat = candidate.OnsetBeat,
            Strength = candidate.Strength,
            Provenance = GrooveOnsetProvenance.ForVariation(group.GroupId, candidateId, enabledTags)
        };
    }

    /// <summary>
    /// Creates a GrooveOnset from a WeightedCandidate (used by selection engine).
    /// Story G2: Convenience overload for GrooveSelectionEngine results.
    /// </summary>
    /// <param name="weightedCandidate">The weighted candidate from selection.</param>
    /// <param name="barNumber">Bar number (1-based).</param>
    /// <param name="enabledTags">Tags that were enabled at selection time (optional).</param>
    /// <returns>GrooveOnset with Variation provenance.</returns>
    public static GrooveOnset FromWeightedCandidate(
        WeightedCandidate weightedCandidate,
        int barNumber,
        IReadOnlyList<string>? enabledTags = null)
    {
        ArgumentNullException.ThrowIfNull(weightedCandidate);
        return FromVariation(weightedCandidate.Candidate, weightedCandidate.Group, barNumber, enabledTags);
    }

    /// <summary>
    /// Creates a copy of a GrooveOnset with updated properties while preserving provenance.
    /// Story G2: Ensures post-processing stages don't lose provenance.
    /// </summary>
    /// <param name="onset">Original onset to copy.</param>
    /// <param name="strength">Optional new strength value.</param>
    /// <param name="velocity">Optional new velocity value.</param>
    /// <param name="timingOffsetTicks">Optional new timing offset.</param>
    /// <returns>New GrooveOnset with updated values and preserved provenance.</returns>
    public static GrooveOnset WithUpdatedProperties(
        GrooveOnset onset,
        OnsetStrength? strength = null,
        int? velocity = null,
        int? timingOffsetTicks = null)
    {
        ArgumentNullException.ThrowIfNull(onset);

        return onset with
        {
            Strength = strength ?? onset.Strength,
            Velocity = velocity ?? onset.Velocity,
            TimingOffsetTicks = timingOffsetTicks ?? onset.TimingOffsetTicks
            // Provenance is automatically preserved by 'with' expression
        };
    }
}
