namespace Music.Generator
{
    // AI: purpose=Defines how override layer merges with base policy; metadata-only (merging implementation separate).
    // AI: invariants=All flags default false for safety; OverrideReplacesLists=true means override replaces, else union.
    // AI: change=OverrideCanRemoveProtectedOnsets=true is dangerous; OverrideCanRelaxConstraints usually false.
    public sealed class GrooveOverrideMergePolicy
    {
        public bool OverrideReplacesLists { get; set; }
        public bool OverrideCanRemoveProtectedOnsets { get; set; }
        public bool OverrideCanRelaxConstraints { get; set; }
        public bool OverrideCanChangeFeel { get; set; }
    }
}
