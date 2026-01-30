// AI: purpose=Factory for creating GrooveOnset records with proper provenance tracking (Story G2).
// AI: invariants=Anchor onsets get Source=Anchor; variation onsets get Source=Variation with GroupId/CandidateId.
// AI: deps=GrooveOnset, GrooveOnsetProvenance, OnsetCandidate, CandidateGroup.
// AI: change=Refactored to use instrument-agnostic types; drum-specific methods moved to Drums namespace.

namespace Music.Generator.Groove;

// AI: Factory for creating GrooveOnset with proper provenance tracking.
public static class GrooveOnsetFactory
{
    // AI: Creates GrooveOnset from anchor with Source=Anchor provenance.
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

    // AI: Creates GrooveOnset from variation candidate with Source=Variation provenance.
    public static GrooveOnset FromVariation(
        OnsetCandidate candidate,
        CandidateGroup group,
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
            Velocity = candidate.VelocityHint,
            TimingOffsetTicks = candidate.TimingHint,
            Provenance = GrooveOnsetProvenance.ForVariation(group.GroupId, candidateId, enabledTags)
        };
    }

    // AI: Creates copy with updated properties; preserves provenance via 'with' expression.
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
        };
    }
}
