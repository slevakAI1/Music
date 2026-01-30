using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums;

// AI: purpose=Drum-specific grouping of onset candidates with shared tags and constraints.
// AI: invariants=MaxAddsPerBar is group-level cap; BaseProbabilityBias is group-level weight for selection.
// AI: change=Conversion methods added to interop with generic Groove.CandidateGroup.
public sealed class DrumCandidateGroup
{
    public string GroupId { get; set; } = "";
    public List<string> GroupTags { get; set; } = [];
    public int MaxAddsPerBar { get; set; }
    public double BaseProbabilityBias { get; set; }
    public List<DrumOnsetCandidate> Candidates { get; set; } = [];

    // AI: Converts from generic CandidateGroup for drum processing.
    public static DrumCandidateGroup FromCandidateGroup(CandidateGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return new DrumCandidateGroup
        {
            GroupId = group.GroupId,
            GroupTags = [.. group.GroupTags],
            MaxAddsPerBar = group.MaxAddsPerBar,
            BaseProbabilityBias = group.BaseProbabilityBias,
            Candidates = group.Candidates.Select(DrumOnsetCandidate.FromOnsetCandidate).ToList()
        };
    }

    // AI: Converts to generic CandidateGroup for groove system.
    public CandidateGroup ToCandidateGroup()
    {
        return new CandidateGroup
        {
            GroupId = GroupId,
            GroupTags = [.. GroupTags],
            MaxAddsPerBar = MaxAddsPerBar,
            BaseProbabilityBias = BaseProbabilityBias,
            Candidates = Candidates.Select(c => c.ToOnsetCandidate()).ToList()
        };
    }
}
