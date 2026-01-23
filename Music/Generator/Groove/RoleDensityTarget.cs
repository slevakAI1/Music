namespace Music.Generator
{
    // AI: purpose=Density targets per role for a segment; desired target for candidate selection (not a hard cap).
    // AI: invariants=Density01 in [0..1]; MaxEventsPerBar is segment-specific cap (<= global cap in RoleConstraintPolicy).
    // AI: change=This is a "target" for selection; actual density may vary based on candidate availability and protection rules.
    public sealed class RoleDensityTarget
    {
        public string Role { get; set; } = "";
        public double Density01 { get; set; }
        public int MaxEventsPerBar { get; set; }
    }
}
