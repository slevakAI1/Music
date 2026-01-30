namespace Music.Generator.Agents.Drums;

// AI: purpose=Drum-specific grouping of onset candidates with shared tags and constraints.
// AI: invariants=MaxAddsPerBar is group-level cap; BaseProbabilityBias is group-level weight for selection.
// AI: change=Conversion methods removed (GC-4); drum candidate groups stand alone, no generic conversion needed.
public sealed class DrumCandidateGroup
{
    public string GroupId { get; set; } = "";
    public List<string> GroupTags { get; set; } = [];
    public int MaxAddsPerBar { get; set; }
    public double BaseProbabilityBias { get; set; }
    public List<DrumOnsetCandidate> Candidates { get; set; } = [];
}
