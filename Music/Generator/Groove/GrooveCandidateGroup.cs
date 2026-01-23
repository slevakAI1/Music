namespace Music.Generator.Groove
{
    // AI: purpose=Grouping of onset candidates with shared tags and constraints; enables/disables via GroupTags.
    // AI: invariants=MaxAddsPerBar is group-level cap; BaseProbabilityBias is group-level weight for selection.
    // AI: change=GroupTags control group activation (e.g., segment.EnabledVariationTags); Candidates list is the pool.
    public sealed class GrooveCandidateGroup
    {
        public string GroupId { get; set; } = "";
        public List<string> GroupTags { get; set; } = new();
        public int MaxAddsPerBar { get; set; }
        public double BaseProbabilityBias { get; set; }
        public List<GrooveOnsetCandidate> Candidates { get; set; } = new();
    }
}
