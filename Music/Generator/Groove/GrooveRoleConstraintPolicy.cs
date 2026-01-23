namespace Music.Generator
{
    // AI: purpose=Rhythm constraints and caps per role; aggregates RoleRhythmVocabulary + density/sustain caps.
    // AI: invariants=RoleVocabulary holds vocab constraints; RoleMaxDensityPerBar and RoleMaxSustainSlots are hard caps.
    // AI: change=Add role to all three dictionaries when introducing new role; MaxSustainSlots applies to pads/keys only.
    public sealed class GrooveRoleConstraintPolicy
    {
        public Dictionary<string, RoleRhythmVocabulary> RoleVocabulary { get; set; } = new();
        public Dictionary<string, int> RoleMaxDensityPerBar { get; set; } = new();
        public Dictionary<string, int> RoleMaxSustainSlots { get; set; } = new();
    }
}
