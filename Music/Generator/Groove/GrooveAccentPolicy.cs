namespace Music.Generator.Groove
{
    // AI: purpose=Velocity shaping per role based on onset strength buckets; maps role+strength to VelocityRule.
    // AI: invariants=RoleStrengthVelocity outer key=role, inner key=OnsetStrength; RoleGhostVelocity optional per-role ghost range.
    // AI: change=Add role or strength bucket; update RoleStrengthVelocity with new mappings; ghost velocity is separate override.
    public sealed class GrooveAccentPolicy
    {
        public Dictionary<string, Dictionary<OnsetStrength, VelocityRule>> RoleStrengthVelocity { get; set; } = new();
        public Dictionary<string, VelocityRule> RoleGhostVelocity { get; set; } = new();
    }
}
