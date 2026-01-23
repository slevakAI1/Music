namespace Music.Generator.Groove
{
    // AI: purpose=Protection sets for a single role; defines must-hit/protected/never-remove/never-add onset constraints.
    // AI: invariants=Onsets in 1-based quarter-note units (e.g., 1.5, 2.0); MustHit cannot be removed by variation.
    // AI: change=NeverRemoveOnsets is hard prohibition (e.g., backbeat); NeverAddOnsets forbids additions (style clean).
    public sealed class RoleProtectionSet
    {
        public List<decimal> MustHitOnsets { get; set; } = new();
        public List<decimal> ProtectedOnsets { get; set; } = new();
        public List<decimal> NeverRemoveOnsets { get; set; } = new();
        public List<decimal> NeverAddOnsets { get; set; } = new();
    }
}
