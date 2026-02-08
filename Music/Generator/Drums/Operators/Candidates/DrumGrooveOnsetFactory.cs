using Music.Generator.Drums.Operators;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.Candidates;

// AI: purpose=Drum-specific factory for creating GrooveOnset from drum variation candidates with provenance.
// AI: invariants=Creates GrooveOnset directly from drum types; no conversion to generic types needed.
// AI: change=FromVariation updated (GC-5) to create GrooveOnset directly instead of using deleted conversion methods.
public static class DrumGrooveOnsetFactory
{
    // AI: purpose=Creates GrooveOnset from drum variation candidate with provenance tracking.
    public static GrooveOnset FromVariation(
        DrumOnsetCandidate candidate,
        DrumCandidateGroup group,
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

    // AI: purpose=Creates GrooveOnset from WeightedCandidate (drum selection engine result).
    public static GrooveOnset FromWeightedCandidate(
        WeightedCandidate weightedCandidate,
        int barNumber,
        IReadOnlyList<string>? enabledTags = null)
    {
        ArgumentNullException.ThrowIfNull(weightedCandidate);
        return FromVariation(weightedCandidate.Candidate, weightedCandidate.Group, barNumber, enabledTags);
    }
}
