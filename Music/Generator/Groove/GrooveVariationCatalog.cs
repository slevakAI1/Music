namespace Music.Generator.Groove
{
    // AI: purpose=Catalog of optional rhythmic candidates for groove variation; grouped by tags with hierarchical layers.
    // AI: invariants=HierarchyLayers ordered [0]=base, [1+]=refinements; Identity links to preset; KnownTags for UI checklists.
    // AI: change=Add tags to KnownTags for UI; CompatibilityTags hint selection/planning (e.g., "NoTriplets", "SafeForMotifs").
    public sealed class GrooveVariationCatalog
    {
        public GroovePresetIdentity Identity { get; set; } = new();
        public List<GrooveVariationLayer> HierarchyLayers { get; set; } = new();
        public List<string> KnownTags { get; set; } = new();
        public List<string> CompatibilityTags { get; set; } = new();
    }
}
