namespace Music.Generator.Groove
{
    // AI: purpose=Single protection layer in hierarchy; multiple layers form base->refined protection chain.
    // AI: invariants=AppliesWhenTagsAll gates layer activation; all tags MUST be enabled for layer to apply.
    // AI: change=IsAdditiveOnly=true means layer only adds protections, never removes earlier ones; layering is ordered.
    public sealed class GrooveProtectionLayer
    {
        public string LayerId { get; set; } = "";
        public List<string> AppliesWhenTagsAll { get; set; } = new();
        public Dictionary<string, RoleProtectionSet> RoleProtections { get; set; } = new();
        public bool IsAdditiveOnly { get; set; }
    }
}
