namespace Music.Generator.Groove;

// AI: purpose=Instrument-agnostic grouping of onset candidates with shared tags and constraints.
// AI: invariants=MaxAddsPerBar is group-level cap; BaseProbabilityBias is group-level weight for selection.
// AI: change=Created from DrumCandidateGroup to decouple groove from drum-specific types.
public sealed class CandidateGroup
{
    public string GroupId { get; set; } = "";
    public List<string> GroupTags { get; set; } = [];
    public int MaxAddsPerBar { get; set; }
    public double BaseProbabilityBias { get; set; }
    public List<OnsetCandidate> Candidates { get; set; } = [];
}
