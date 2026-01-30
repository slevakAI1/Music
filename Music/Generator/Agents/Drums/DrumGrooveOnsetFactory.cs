using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums;

// AI: purpose=Drum-specific factory methods for creating GrooveOnset from drum types.
// AI: invariants=Wraps generic GrooveOnsetFactory with conversion from drum types.
// AI: change=Created to maintain drum-specific API after groove system became instrument-agnostic.
public static class DrumGrooveOnsetFactory
{
    // AI: Creates GrooveOnset from DrumOnsetCandidate by converting to generic type first.
    public static GrooveOnset FromVariation(
        DrumOnsetCandidate candidate,
        DrumCandidateGroup group,
        int barNumber,
        IReadOnlyList<string>? enabledTags = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(group);

        return GrooveOnsetFactory.FromVariation(
            candidate.ToOnsetCandidate(),
            group.ToCandidateGroup(),
            barNumber,
            enabledTags);
    }

    // AI: Creates GrooveOnset from WeightedCandidate (drum selection engine result).
    public static GrooveOnset FromWeightedCandidate(
        WeightedCandidate weightedCandidate,
        int barNumber,
        IReadOnlyList<string>? enabledTags = null)
    {
        ArgumentNullException.ThrowIfNull(weightedCandidate);
        return FromVariation(weightedCandidate.Candidate, weightedCandidate.Group, barNumber, enabledTags);
    }
}
