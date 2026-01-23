namespace Music.Generator.Groove
{
    // AI: purpose=Micro-timing template per role; defines timing feel and tick bias for pocket/groove simulation.
    // AI: invariants=RoleTimingBiasTicks can be 0; MaxAbsTimingBiasTicks clamps all applied biases; feel drives direction.
    // AI: change=Add role to RoleTimingFeel and RoleTimingBiasTicks; ensure MaxAbsTimingBiasTicks prevents extreme offsets.
    public sealed class GrooveTimingPolicy
    {
        public Dictionary<string, TimingFeel> RoleTimingFeel { get; set; } = new();
        public Dictionary<string, int> RoleTimingBiasTicks { get; set; } = new();
        public int MaxAbsTimingBiasTicks { get; set; }
    }
}
