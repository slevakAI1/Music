namespace Music.Generator.Agents.Drums
{
    // AI: purpose=Density targets per role for a segment; desired target for candidate selection (not a hard cap).
    // AI: invariants=Density01 in [0..1]; MaxEventsPerBar is segment-specific cap (<= global cap in RoleConstraintPolicy).
    // AI: change=Story 4.3: Moved from Generator/Groove to Generator/Agents/Drums; domain ownership = Drum Generator.
    public sealed class RoleDensityTarget
    {
        public string Role { get; set; } = "";
        public double Density01 { get; set; }
        public int MaxEventsPerBar { get; set; }
    }
}
