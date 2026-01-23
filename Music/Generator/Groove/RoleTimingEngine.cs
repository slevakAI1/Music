// AI: purpose=Apply per-role micro-timing (feel/bias) on top of E1 feel timing (Story E2).
// AI: invariants=Deterministic output; Beat unchanged, only TimingOffsetTicks modified; final offset clamped to [-MaxAbsTimingBiasTicks..+MaxAbsTimingBiasTicks].
// AI: deps=GrooveTimingPolicy, TimingFeel, GroovePolicyDecision, GrooveOnset; E2 runs after E1 (additive semantics).
// AI: change=TimingFeel maps to fixed ticks: Ahead=-10, OnTop=0, Behind=+10, LaidBack=+20; unknown roles default to OnTop/0 bias.

namespace Music.Generator.Groove;

/// <summary>
/// Applies per-role micro-timing (feel/bias) on top of E1 feel timing.
/// Story E2: Role timing shifts allow roles to sit ahead, on-top, behind, or laid-back relative to grid.
/// </summary>
/// <remarks>
/// Operation order:
/// 1. Resolve effective role feel and bias (policy decision → policy → defaults)
/// 2. Map TimingFeel to base tick offset
/// 3. Add RoleTimingBiasTicks to base offset
/// 4. Add per-role offset to existing TimingOffsetTicks (which may include E1 feel)
/// 5. Clamp final offset to [-MaxAbsTimingBiasTicks .. +MaxAbsTimingBiasTicks]
/// 6. Produce new GrooveOnset records
/// </remarks>
public static class RoleTimingEngine
{
    // TimingFeel base tick offsets (E2-1 specification)
    // Ahead: -10 (slightly ahead of grid, rushes subtly)
    // OnTop: 0 (very close to grid, tight)
    // Behind: +10 (slightly behind grid, lays back subtly)
    // LaidBack: +20 (more behind / relaxed, noticeably laid back)
    private const int AheadBaseTicks = -10;
    private const int OnTopBaseTicks = 0;
    private const int BehindBaseTicks = 10;
    private const int LaidBackBaseTicks = 20;

    /// <summary>
    /// Applies per-role timing to a list of onsets. Returns new immutable records with computed timing offsets.
    /// E2 is additive: role timing is added to any existing TimingOffsetTicks (from E1 or prior).
    /// </summary>
    /// <param name="onsets">Source onsets to apply role timing to</param>
    /// <param name="timingPolicy">Timing policy with per-role feel and bias settings</param>
    /// <param name="policyDecision">Optional policy decision with feel/bias overrides</param>
    /// <returns>New onset list with TimingOffsetTicks computed (additive to existing offsets, clamped)</returns>
    public static IReadOnlyList<GrooveOnset> ApplyRoleTiming(
        IReadOnlyList<GrooveOnset> onsets,
        GrooveTimingPolicy? timingPolicy,
        GroovePolicyDecision? policyDecision = null)
    {
        ArgumentNullException.ThrowIfNull(onsets);

        if (onsets.Count == 0)
            return onsets;

        // Default policy if none provided
        timingPolicy ??= new GrooveTimingPolicy();

        var result = new List<GrooveOnset>(onsets.Count);

        foreach (var onset in onsets)
        {
            // Compute role timing offset for this onset
            int roleOffset = ComputeRoleOffset(onset.Role, timingPolicy, policyDecision);

            // Add to existing offset (E1 feel timing or prior)
            int existingOffset = onset.TimingOffsetTicks ?? 0;
            int combinedOffset = existingOffset + roleOffset;

            // Clamp final offset to safety bounds
            int maxAbs = timingPolicy.MaxAbsTimingBiasTicks;
            int finalOffset = maxAbs > 0
                ? Math.Clamp(combinedOffset, -maxAbs, maxAbs)
                : combinedOffset; // If maxAbs is 0 or negative, no clamping

            result.Add(onset with { TimingOffsetTicks = finalOffset });
        }

        return result;
    }

    /// <summary>
    /// Computes the role timing offset in ticks for a given role.
    /// Formula: roleOffset = baseTickOffset(effectiveFeel) + effectiveRoleBias
    /// </summary>
    /// <param name="role">Role name (e.g., "Kick", "Snare")</param>
    /// <param name="timingPolicy">Timing policy with per-role settings</param>
    /// <param name="policyDecision">Optional policy decision with overrides</param>
    /// <returns>Role timing offset in ticks (can be positive or negative)</returns>
    public static int ComputeRoleOffset(
        string role,
        GrooveTimingPolicy? timingPolicy,
        GroovePolicyDecision? policyDecision = null)
    {
        timingPolicy ??= new GrooveTimingPolicy();

        var effectiveFeel = ResolveEffectiveFeel(role, timingPolicy, policyDecision);
        int effectiveBias = ResolveEffectiveBias(role, timingPolicy, policyDecision);

        int baseOffset = MapTimingFeelToTicks(effectiveFeel);

        return baseOffset + effectiveBias;
    }

    /// <summary>
    /// Resolves effective timing feel for a role using field-level override precedence.
    /// Order: GroovePolicyDecision override → GrooveTimingPolicy → default (OnTop)
    /// </summary>
    public static TimingFeel ResolveEffectiveFeel(
        string role,
        GrooveTimingPolicy? timingPolicy,
        GroovePolicyDecision? policyDecision = null)
    {
        // Policy decision override wins first
        if (policyDecision?.RoleTimingFeelOverride.HasValue == true)
            return policyDecision.RoleTimingFeelOverride.Value;

        // Then try policy lookup for role
        if (timingPolicy?.RoleTimingFeel is not null &&
            timingPolicy.RoleTimingFeel.TryGetValue(role, out var policyFeel))
            return policyFeel;

        // Default to OnTop for unknown roles
        return TimingFeel.OnTop;
    }

    /// <summary>
    /// Resolves effective timing bias in ticks for a role using field-level override precedence.
    /// Order: GroovePolicyDecision override → GrooveTimingPolicy → default (0)
    /// </summary>
    public static int ResolveEffectiveBias(
        string role,
        GrooveTimingPolicy? timingPolicy,
        GroovePolicyDecision? policyDecision = null)
    {
        // Policy decision override wins first
        if (policyDecision?.RoleTimingBiasTicksOverride.HasValue == true)
            return policyDecision.RoleTimingBiasTicksOverride.Value;

        // Then try policy lookup for role
        if (timingPolicy?.RoleTimingBiasTicks is not null &&
            timingPolicy.RoleTimingBiasTicks.TryGetValue(role, out var policyBias))
            return policyBias;

        // Default to 0 for unknown roles
        return 0;
    }

    /// <summary>
    /// Maps TimingFeel enum to base tick offset.
    /// </summary>
    /// <param name="feel">Timing feel to map</param>
    /// <returns>Base tick offset (Ahead=-10, OnTop=0, Behind=+10, LaidBack=+20)</returns>
    public static int MapTimingFeelToTicks(TimingFeel feel) => feel switch
    {
        TimingFeel.Ahead => AheadBaseTicks,
        TimingFeel.OnTop => OnTopBaseTicks,
        TimingFeel.Behind => BehindBaseTicks,
        TimingFeel.LaidBack => LaidBackBaseTicks,
        _ => OnTopBaseTicks // Future-proofing: unknown feel types default to OnTop
    };

    /// <summary>
    /// Computes role timing with diagnostic information for debugging/tracing.
    /// </summary>
    public static RoleTimingDiagnostics ComputeRoleTimingWithDiagnostics(
        GrooveOnset onset,
        GrooveTimingPolicy? timingPolicy,
        GroovePolicyDecision? policyDecision = null)
    {
        timingPolicy ??= new GrooveTimingPolicy();

        var effectiveFeel = ResolveEffectiveFeel(onset.Role, timingPolicy, policyDecision);
        int effectiveBias = ResolveEffectiveBias(onset.Role, timingPolicy, policyDecision);

        int baseFeelOffset = MapTimingFeelToTicks(effectiveFeel);
        int roleOffset = baseFeelOffset + effectiveBias;

        int existingOffset = onset.TimingOffsetTicks ?? 0;
        int preClampCombined = existingOffset + roleOffset;

        int maxAbs = timingPolicy.MaxAbsTimingBiasTicks;
        int finalOffset = maxAbs > 0
            ? Math.Clamp(preClampCombined, -maxAbs, maxAbs)
            : preClampCombined;

        bool wasClamped = finalOffset != preClampCombined;

        return new RoleTimingDiagnostics(
            Role: onset.Role,
            Beat: onset.Beat,
            EffectiveTimingFeel: effectiveFeel,
            EffectiveTimingBiasTicks: effectiveBias,
            BaseFeelOffsetTicks: baseFeelOffset,
            RoleOffsetTicks: roleOffset,
            ExistingOffsetTicks: existingOffset,
            PreClampCombinedOffset: preClampCombined,
            FinalOffsetTicks: finalOffset,
            WasClamped: wasClamped);
    }
}

/// <summary>
/// Diagnostic record for role timing computations (Story G1 support).
/// </summary>
public sealed record RoleTimingDiagnostics(
    string Role,
    decimal Beat,
    TimingFeel EffectiveTimingFeel,
    int EffectiveTimingBiasTicks,
    int BaseFeelOffsetTicks,
    int RoleOffsetTicks,
    int ExistingOffsetTicks,
    int PreClampCombinedOffset,
    int FinalOffsetTicks,
    bool WasClamped);
