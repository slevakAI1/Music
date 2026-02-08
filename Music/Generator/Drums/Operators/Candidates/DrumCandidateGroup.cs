namespace Music.Generator.Drums.Operators.Candidates;

// AI: purpose=Group onset candidates with shared tags, constraints and selection bias for drum selection.
// AI: invariants=MaxAddsPerBar caps adds per bar; BaseProbabilityBias biases group selection weights.
// AI: deps=Used by selection engine; Candidates contains DrumOnsetCandidate instances; not thread-safe.
public sealed class DrumCandidateGroup
{
    // AI: contract=GroupId identifies group; GroupTags aid filtering; Candidates mutated during selection
    public string GroupId { get; set; } = "";
    public List<string> GroupTags { get; set; } = [];
    public int MaxAddsPerBar { get; set; }
    public double BaseProbabilityBias { get; set; }
    public List<DrumOnsetCandidate> Candidates { get; set; } = [];
}
