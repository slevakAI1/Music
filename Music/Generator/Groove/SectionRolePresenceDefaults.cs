namespace Music.Generator.Groove
{
    // AI: purpose=Default role presence for a section type; lightweight orchestration hint (e.g., Verse vs Chorus).
    // AI: invariants=SectionType is string (e.g., "Verse", "Chorus"); RolePresent gates role generation in section.
    // AI: change=RoleRegisterLiftSemitones shifts pitch up; RoleDensityMultiplier scales hits; both are section-specific.
    public sealed class SectionRolePresenceDefaults
    {
        public string SectionType { get; set; } = "";
        public Dictionary<string, bool> RolePresent { get; set; } = new();
        public Dictionary<string, int> RoleRegisterLiftSemitones { get; set; } = new();
        public Dictionary<string, double> RoleDensityMultiplier { get; set; } = new();
    }
}
