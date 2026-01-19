// AI: purpose=Compute velocity per onset using role x strength lookup from GrooveAccentPolicy (Story D2).
// AI: invariants=Final velocity always in [1..127]; null onset velocity preserved if already set; deterministic output.
// AI: deps=GrooveAccentPolicy, VelocityRule, GroovePolicyDecision.VelocityBiasOverride, OnsetStrength.
// AI: change=Ghost handling: RoleGhostVelocity takes precedence over RoleStrengthVelocity[Ghost].

namespace Music.Generator;

/// <summary>
/// Computes velocity per onset using role x strength lookup from GrooveAccentPolicy.
/// Story D2: Velocity Shaping (Role x Strength).
/// </summary>
public static class VelocityShaper
{
    /// <summary>
    /// Default velocity rule used when no role/strength lookup matches.
    /// </summary>
    public static VelocityRule GlobalDefaultRule { get; } = new()
    {
        Typical = 80,
        AccentBias = 0,
        Min = 1,
        Max = 127
    };

    /// <summary>
    /// Strength fallback priority order when the requested strength is missing for a role.
    /// </summary>
    private static readonly OnsetStrength[] StrengthFallbackOrder =
    [
        OnsetStrength.Downbeat,
        OnsetStrength.Backbeat,
        OnsetStrength.Strong,
        OnsetStrength.Pickup,
        OnsetStrength.Offbeat
    ];

    /// <summary>
    /// Shapes velocities for a list of onsets. Onsets with existing velocity are preserved unchanged.
    /// Returns new immutable records with computed velocities.
    /// </summary>
    /// <param name="onsets">Source onsets to shape</param>
    /// <param name="accentPolicy">Accent policy with role/strength velocity rules</param>
    /// <param name="policyDecision">Optional policy decision with velocity bias override</param>
    /// <returns>New onset list with velocities computed where missing</returns>
    public static IReadOnlyList<GrooveOnset> ShapeVelocities(
        IReadOnlyList<GrooveOnset> onsets,
        GrooveAccentPolicy? accentPolicy,
        GroovePolicyDecision? policyDecision = null)
    {
        ArgumentNullException.ThrowIfNull(onsets);

        var result = new List<GrooveOnset>(onsets.Count);

        foreach (var onset in onsets)
        {
            // Preserve existing velocity if already set
            if (onset.Velocity.HasValue)
            {
                result.Add(onset);
                continue;
            }

            // Compute velocity using role + strength lookup
            var strength = onset.Strength ?? OnsetStrength.Strong; // Default to Strong if not classified
            int velocity = ComputeVelocity(onset.Role, strength, accentPolicy, policyDecision);

            result.Add(onset with { Velocity = velocity });
        }

        return result;
    }

    /// <summary>
    /// Computes velocity for a single onset using role x strength lookup with fallback chain.
    /// Order: base = Typical + AccentBias → apply policy override → clamp to rule bounds → clamp to MIDI range.
    /// </summary>
    /// <param name="role">Role name (e.g., "Kick", "Snare")</param>
    /// <param name="strength">Onset strength classification</param>
    /// <param name="accentPolicy">Accent policy with role/strength velocity rules</param>
    /// <param name="policyDecision">Optional policy decision with velocity bias override</param>
    /// <returns>Computed velocity clamped to [1..127]</returns>
    public static int ComputeVelocity(
        string role,
        OnsetStrength strength,
        GrooveAccentPolicy? accentPolicy,
        GroovePolicyDecision? policyDecision = null)
    {
        var (rule, _) = ResolveVelocityRule(role, strength, accentPolicy);

        // Normalize rule bounds to valid MIDI range [1..127]
        int ruleMin = NormalizeRuleBound(rule.Min);
        int ruleMax = NormalizeRuleBound(rule.Max);

        // If min > max after normalization, swap them for deterministic behavior
        if (ruleMin > ruleMax)
            (ruleMin, ruleMax) = (ruleMax, ruleMin);

        // Base velocity = Typical + AccentBias
        // For Ghost resolved via RoleGhostVelocity, AccentBias is treated as 0 (already baked into Typical)
        int baseVelocity = rule.Typical + rule.AccentBias;

        // Apply policy override (multiplier then additive) per Story D2 Q8-Q9
        // Order: biased = round(base * multiplier, AwayFromZero) + additive
        double multiplier = policyDecision?.VelocityMultiplierOverride ?? 1.0;
        int additive = policyDecision?.VelocityBiasOverride ?? 0;

        int biasedVelocity = (int)Math.Round(baseVelocity * multiplier, MidpointRounding.AwayFromZero) + additive;

        // Clamp to rule bounds, then to MIDI range
        int clampedToRule = Math.Clamp(biasedVelocity, ruleMin, ruleMax);
        int finalVelocity = Math.Clamp(clampedToRule, 1, 127);

        return finalVelocity;
    }

    /// <summary>
    /// Computes velocity with full diagnostics for debugging and testing.
    /// </summary>
    public static (int velocity, VelocityShapingDiagnostics diagnostics) ComputeVelocityWithDiagnostics(
        string role,
        OnsetStrength strength,
        GrooveAccentPolicy? accentPolicy,
        GroovePolicyDecision? policyDecision = null)
    {
        var (rule, source) = ResolveVelocityRule(role, strength, accentPolicy);

        int ruleMin = NormalizeRuleBound(rule.Min);
        int ruleMax = NormalizeRuleBound(rule.Max);
        if (ruleMin > ruleMax)
            (ruleMin, ruleMax) = (ruleMax, ruleMin);

        int baseVelocity = rule.Typical + rule.AccentBias;

        double multiplier = policyDecision?.VelocityMultiplierOverride ?? 1.0;
        int additive = policyDecision?.VelocityBiasOverride ?? 0;

        int preClampVelocity = (int)Math.Round(baseVelocity * multiplier, MidpointRounding.AwayFromZero) + additive;
        int clampedToRule = Math.Clamp(preClampVelocity, ruleMin, ruleMax);
        int finalVelocity = Math.Clamp(clampedToRule, 1, 127);

        var diagnostics = new VelocityShapingDiagnostics
        {
            Role = role,
            Strength = strength,
            RuleSource = source,
            Typical = rule.Typical,
            AccentBias = rule.AccentBias,
            BaseVelocity = baseVelocity,
            PolicyMultiplier = multiplier,
            PolicyAdditive = additive,
            PreClampVelocity = preClampVelocity,
            RuleMin = ruleMin,
            RuleMax = ruleMax,
            FinalVelocity = finalVelocity
        };

        return (finalVelocity, diagnostics);
    }

    /// <summary>
    /// Resolves velocity rule for role + strength with fallback chain.
    /// Precedence for Ghost: RoleGhostVelocity > RoleStrengthVelocity[Ghost] > fallbacks.
    /// Fallback order: Offbeat > Downbeat > Backbeat > Strong > Pickup > global default.
    /// </summary>
    /// <param name="role">Role name</param>
    /// <param name="strength">Onset strength</param>
    /// <param name="accentPolicy">Accent policy (can be null)</param>
    /// <returns>Resolved velocity rule and source indicator for diagnostics</returns>
    public static (VelocityRule rule, VelocityRuleSource source) ResolveVelocityRule(
        string role,
        OnsetStrength strength,
        GrooveAccentPolicy? accentPolicy)
    {
        if (accentPolicy is null)
            return (GlobalDefaultRule, VelocityRuleSource.FallbackGlobal);

        // Ghost special handling: RoleGhostVelocity takes precedence
        if (strength == OnsetStrength.Ghost)
        {
            if (accentPolicy.RoleGhostVelocity.TryGetValue(role, out var ghostRule))
            {
                // Create a copy with AccentBias = 0 since ghost velocity is the final target
                return (new VelocityRule
                {
                    Typical = ghostRule.Typical,
                    AccentBias = 0, // Ghost velocity is the direct target, no additional bias
                    Min = ghostRule.Min,
                    Max = ghostRule.Max
                }, VelocityRuleSource.RoleGhost);
            }

            // Fall through to try RoleStrengthVelocity[Ghost]
        }

        // Try direct role + strength lookup
        if (accentPolicy.RoleStrengthVelocity.TryGetValue(role, out var strengthDict))
        {
            if (strengthDict.TryGetValue(strength, out var directRule))
                return (directRule, VelocityRuleSource.RoleStrength);

            // Role found but strength missing - try Offbeat first
            if (strengthDict.TryGetValue(OnsetStrength.Offbeat, out var offbeatRule))
                return (offbeatRule, VelocityRuleSource.FallbackRoleOffbeat);

            // Try strength fallback order (first available)
            foreach (var fallbackStrength in StrengthFallbackOrder)
            {
                if (strengthDict.TryGetValue(fallbackStrength, out var fallbackRule))
                    return (fallbackRule, VelocityRuleSource.FallbackRoleFirst);
            }
        }

        // Role not found - use global default
        return (GlobalDefaultRule, VelocityRuleSource.FallbackGlobal);
    }

    /// <summary>
    /// Normalizes a rule bound to valid MIDI velocity range [1..127].
    /// </summary>
    private static int NormalizeRuleBound(int bound) => Math.Clamp(bound, 1, 127);
}

/// <summary>
/// Indicates how a velocity rule was resolved for diagnostics.
/// </summary>
public enum VelocityRuleSource
{
    /// <summary>Direct lookup of RoleStrengthVelocity[role][strength] succeeded.</summary>
    RoleStrength,

    /// <summary>Used RoleGhostVelocity[role] for Ghost strength.</summary>
    RoleGhost,

    /// <summary>Role found but requested strength missing; used Offbeat as fallback.</summary>
    FallbackRoleOffbeat,

    /// <summary>Role found but Offbeat missing; used first available strength in priority order.</summary>
    FallbackRoleFirst,

    /// <summary>Role not found in policy; used global default rule.</summary>
    FallbackGlobal
}

/// <summary>
/// Diagnostics record capturing all inputs and intermediate values for velocity computation.
/// </summary>
public sealed record VelocityShapingDiagnostics
{
    public required string Role { get; init; }
    public required OnsetStrength Strength { get; init; }
    public required VelocityRuleSource RuleSource { get; init; }
    public required int Typical { get; init; }
    public required int AccentBias { get; init; }
    public required int BaseVelocity { get; init; }
    public required double PolicyMultiplier { get; init; }
    public required int PolicyAdditive { get; init; }
    public required int PreClampVelocity { get; init; }
    public required int RuleMin { get; init; }
    public required int RuleMax { get; init; }
    public required int FinalVelocity { get; init; }
}
