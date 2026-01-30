// AI: purpose=Apply per-role micro-timing (feel/bias) on top of E1 feel timing (Story E2).
// AI: invariants=Deterministic output; Beat unchanged, only TimingOffsetTicks modified.
// AI: deps=TimingFeel, GrooveOnset; E2 runs after E1 (additive semantics).
// AI: change=Story 5.3: Simplified, removed policy dependencies (moved to Drum Generator domain).

namespace Music.Generator.Groove;

/// <summary>
/// Applies per-role micro-timing (feel/bias) on top of E1 feel timing.
/// Story E2: Role timing shifts allow roles to sit ahead, on-top, behind, or laid-back relative to grid.
/// </summary>
/// <remarks>
/// Operation order:
/// 1. Map TimingFeel to base tick offset
/// 2. Add role bias ticks
/// 3. Add per-role offset to existing TimingOffsetTicks (which may include E1 feel)
/// 4. Produce new GrooveOnset records
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
    /// <param name="feel">Timing feel to apply</param>
    /// <param name="biasTicks">Additional bias in ticks</param>
    /// <returns>New onset list with TimingOffsetTicks computed (additive to existing offsets)</returns>
    public static IReadOnlyList<GrooveOnset> ApplyRoleTiming(
        IReadOnlyList<GrooveOnset> onsets,
        TimingFeel feel = TimingFeel.OnTop,
        int biasTicks = 0)
    {
        ArgumentNullException.ThrowIfNull(onsets);

        if (onsets.Count == 0)
            return onsets;

        var result = new List<GrooveOnset>(onsets.Count);
        int baseOffset = MapTimingFeelToTicks(feel);
        int roleOffset = baseOffset + biasTicks;

        foreach (var onset in onsets)
        {
            // Add to existing offset (E1 feel timing or prior)
            int existingOffset = onset.TimingOffsetTicks ?? 0;
            int combinedOffset = existingOffset + roleOffset;

            result.Add(onset with { TimingOffsetTicks = combinedOffset });
        }

        return result;
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
    int FinalOffsetTicks);
