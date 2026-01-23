namespace Music.Generator
{
    // AI: purpose=Global groove protection policy with hierarchical layers; aggregates all #1-#8 policies + merge semantics.
    // AI: invariants=HierarchyLayers ordered [0]=base, [1+]=refinements; Identity links to preset; MergePolicy controls overrides.
    // AI: change=Add new layer to HierarchyLayers for refinement; set AppliesWhenTagsAll to gate layer activation.
    public sealed class GrooveProtectionPolicy
    {
        public GroovePresetIdentity Identity { get; set; } = new();
        public GrooveSubdivisionPolicy SubdivisionPolicy { get; set; } = new();
        public GrooveRoleConstraintPolicy RoleConstraintPolicy { get; set; } = new();
        public GroovePhraseHookPolicy PhraseHookPolicy { get; set; } = new();
        public GrooveTimingPolicy TimingPolicy { get; set; } = new();
        public GrooveAccentPolicy AccentPolicy { get; set; } = new();
        public GrooveOrchestrationPolicy OrchestrationPolicy { get; set; } = new();
        public List<GrooveProtectionLayer> HierarchyLayers { get; set; } = new();
        public GrooveOverrideMergePolicy MergePolicy { get; set; } = new();
    }
}
