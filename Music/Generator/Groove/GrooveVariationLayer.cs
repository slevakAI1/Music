namespace Music.Generator.Groove
{
    // AI: purpose=Hierarchical layer of variation candidates; later layers refine earlier ones.
    // AI: invariants=AppliesWhenTagsAll gates layer activation; all tags MUST be enabled for layer to apply.
    // AI: change=IsAdditiveOnly=true means only adds candidates, does not remove earlier ones; layering is ordered.
    public sealed class GrooveVariationLayer
    {
        public string LayerId { get; set; } = "";
        public List<string> AppliesWhenTagsAll { get; set; } = new();
        public List<GrooveCandidateGroup> CandidateGroups { get; set; } = new();
        public bool IsAdditiveOnly { get; set; }
    }
}
