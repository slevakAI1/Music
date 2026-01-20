// AI: purpose=Apply groove feel timing (straight/swing/shuffle/triplet) to onset positions (Story E1, F1).
// AI: invariants=Deterministic output; only eighth offbeats (n+0.5) are eligible; Beat unchanged, only TimingOffsetTicks modified.
// AI: deps=GrooveFeel, GrooveSubdivisionPolicy, SegmentGrooveProfile, AllowedSubdivision, MusicConstants.TicksPerQuarterNote, GrooveOverrideMergePolicy.
// AI: change=Story F1: OverrideCanChangeFeel policy controls whether segment feel/swing overrides are applied.

namespace Music.Generator;

/// <summary>
/// Applies groove feel timing (straight, swing, shuffle, triplet) to onset positions.
/// Story E1: Feel timing shifts eighth offbeats to create stylistically appropriate pocket.
/// Story F1: OverrideCanChangeFeel policy controls whether segment feel/swing overrides are applied.
/// </summary>
/// <remarks>
/// Eligibility: Only eighth offbeats (beat = integer + 0.5) are shifted.
/// Downbeats, sixteenths, and triplet positions are never shifted by E1.
/// Feel timing is additive: existing TimingOffsetTicks is preserved and feel offset is added.
/// 
/// Policy behavior (Story F1):
/// - When OverrideCanChangeFeel=false: ignore segment overrides, use base policy values
/// - When OverrideCanChangeFeel=true (or no policy provided): allow segment overrides
/// </remarks>
public static class FeelTimingEngine
{
    private const double BeatEpsilon = 0.002; // Tolerance for floating-point beat comparisons

    /// <summary>
    /// Applies feel timing to a list of onsets. Returns new immutable records with computed timing offsets.
    /// </summary>
    /// <param name="onsets">Source onsets to apply feel timing to</param>
    /// <param name="subdivisionPolicy">Base subdivision policy with Feel and SwingAmount01</param>
    /// <param name="segmentProfile">Optional segment profile with feel/swing overrides</param>
    /// <param name="mergePolicy">Optional merge policy controlling whether feel overrides are allowed</param>
    /// <returns>New onset list with TimingOffsetTicks computed for eligible onsets</returns>
    public static IReadOnlyList<GrooveOnset> ApplyFeelTiming(
        IReadOnlyList<GrooveOnset> onsets,
        GrooveSubdivisionPolicy subdivisionPolicy,
        SegmentGrooveProfile? segmentProfile = null,
        GrooveOverrideMergePolicy? mergePolicy = null)
    {
        ArgumentNullException.ThrowIfNull(onsets);
        ArgumentNullException.ThrowIfNull(subdivisionPolicy);

        if (onsets.Count == 0)
            return onsets;

        // Resolve effective feel and swing amount (respecting merge policy)
        var effectiveFeel = ResolveEffectiveFeel(subdivisionPolicy, segmentProfile, mergePolicy);
        var effectiveSwingAmount = ResolveEffectiveSwingAmount(subdivisionPolicy, segmentProfile, mergePolicy);

        // Straight feel: no shift applied
        if (effectiveFeel == GrooveFeel.Straight)
            return onsets;

        // Check if Eighth subdivision is allowed - if not, no feel timing can be applied
        bool eighthAllowed = subdivisionPolicy.AllowedSubdivisions.HasFlag(AllowedSubdivision.Eighth);

        var result = new List<GrooveOnset>(onsets.Count);

        foreach (var onset in onsets)
        {
            // Skip if not an eligible eighth offbeat or if Eighth not allowed
            if (!eighthAllowed || !IsEighthOffbeat(onset.Beat))
            {
                result.Add(onset);
                continue;
            }

            // Compute feel offset based on feel type
            int feelOffsetTicks = ComputeFeelOffsetTicks(effectiveFeel, effectiveSwingAmount);

            // Apply additive timing: existing offset + feel offset
            int existingOffset = onset.TimingOffsetTicks ?? 0;
            int newOffset = existingOffset + feelOffsetTicks;

            result.Add(onset with { TimingOffsetTicks = newOffset });
        }

        return result;
    }

    /// <summary>
    /// Resolves effective feel from segment override or policy fallback.
    /// Story F1: When mergePolicy.OverrideCanChangeFeel=false, ignores segment override.
    /// </summary>
    /// <param name="subdivisionPolicy">Base subdivision policy with Feel.</param>
    /// <param name="segmentProfile">Optional segment profile with feel override.</param>
    /// <param name="mergePolicy">Optional merge policy (null = allow overrides).</param>
    /// <returns>Effective feel to apply.</returns>
    public static GrooveFeel ResolveEffectiveFeel(
        GrooveSubdivisionPolicy subdivisionPolicy,
        SegmentGrooveProfile? segmentProfile,
        GrooveOverrideMergePolicy? mergePolicy = null)
    {
        // If policy disallows feel changes, use base only
        if (mergePolicy is not null && !mergePolicy.OverrideCanChangeFeel)
        {
            return subdivisionPolicy.Feel;
        }

        return segmentProfile?.OverrideFeel ?? subdivisionPolicy.Feel;
    }

    /// <summary>
    /// Resolves effective swing amount from segment override or policy fallback.
    /// Story F1: When mergePolicy.OverrideCanChangeFeel=false, ignores segment override.
    /// Result is clamped to [0.0..1.0].
    /// </summary>
    /// <param name="subdivisionPolicy">Base subdivision policy with SwingAmount01.</param>
    /// <param name="segmentProfile">Optional segment profile with swing override.</param>
    /// <param name="mergePolicy">Optional merge policy (null = allow overrides).</param>
    /// <returns>Effective swing amount clamped to [0.0..1.0].</returns>
    public static double ResolveEffectiveSwingAmount(
        GrooveSubdivisionPolicy subdivisionPolicy,
        SegmentGrooveProfile? segmentProfile,
        GrooveOverrideMergePolicy? mergePolicy = null)
    {
        // If policy disallows feel changes, use base only (swing is part of feel)
        if (mergePolicy is not null && !mergePolicy.OverrideCanChangeFeel)
        {
            return Math.Clamp(subdivisionPolicy.SwingAmount01, 0.0, 1.0);
        }

        double rawValue = segmentProfile?.OverrideSwingAmount01 ?? subdivisionPolicy.SwingAmount01;
        return Math.Clamp(rawValue, 0.0, 1.0);
    }

    /// <summary>
    /// Determines if a beat position is an eligible eighth offbeat (n + 0.5 pattern).
    /// </summary>
    /// <param name="beat">Beat position in 1-based quarter-note units</param>
    /// <returns>True if the beat is an eighth offbeat eligible for feel shifting</returns>
    public static bool IsEighthOffbeat(decimal beat)
    {
        // Eighth offbeat pattern: integer + 0.5
        // Get the fractional part of the beat
        double beatValue = (double)beat;
        double fractionalPart = beatValue - Math.Floor(beatValue);

        // Check if fractional part is approximately 0.5
        return Math.Abs(fractionalPart - 0.5) < BeatEpsilon;
    }

    /// <summary>
    /// Computes the feel offset in ticks for an eligible eighth offbeat.
    /// </summary>
    /// <param name="feel">The groove feel to apply</param>
    /// <param name="swingAmount01">Swing amount (0.0 to 1.0), used only for Swing feel</param>
    /// <returns>Timing offset in ticks (positive = later)</returns>
    /// <remarks>
    /// Swing formula: linear interpolation from n+0.5 to n+2/3 based on swingAmount01.
    /// - At swingAmount01 = 0.0: no shift
    /// - At swingAmount01 = 1.0: full shift to triplet position (2/3 - 0.5 = 1/6 beat)
    /// 
    /// Shuffle and TripletFeel: fixed triplet mapping (ignores swingAmount01).
    /// - Always shifts to n+2/3 (full 1/6 beat shift).
    /// </remarks>
    public static int ComputeFeelOffsetTicks(GrooveFeel feel, double swingAmount01)
    {
        // Calculate maximum shift: from n+0.5 to n+2/3 = 1/6 beat
        // 1/6 beat in ticks = TicksPerQuarterNote / 6
        int maxShiftTicks = MusicConstants.TicksPerQuarterNote / 6; // 480 / 6 = 80 ticks

        return feel switch
        {
            GrooveFeel.Straight => 0,
            GrooveFeel.Swing => (int)Math.Round(maxShiftTicks * swingAmount01, MidpointRounding.AwayFromZero),
            GrooveFeel.Shuffle => maxShiftTicks, // Fixed triplet mapping
            GrooveFeel.TripletFeel => maxShiftTicks, // Same as Shuffle for E1 scope
            _ => 0 // Future-proofing: unknown feel types get no shift
        };
    }

    /// <summary>
    /// Computes feel offset with diagnostic information for debugging/tracing.
    /// </summary>
    /// <param name="onset">The onset to compute diagnostics for.</param>
    /// <param name="subdivisionPolicy">Base subdivision policy.</param>
    /// <param name="segmentProfile">Optional segment profile with overrides.</param>
    /// <param name="mergePolicy">Optional merge policy controlling feel overrides.</param>
    /// <returns>Diagnostics record with computed values and sources.</returns>
    public static FeelTimingDiagnostics ComputeFeelOffsetWithDiagnostics(
        GrooveOnset onset,
        GrooveSubdivisionPolicy subdivisionPolicy,
        SegmentGrooveProfile? segmentProfile,
        GrooveOverrideMergePolicy? mergePolicy = null)
    {
        var effectiveFeel = ResolveEffectiveFeel(subdivisionPolicy, segmentProfile, mergePolicy);
        var effectiveSwingAmount = ResolveEffectiveSwingAmount(subdivisionPolicy, segmentProfile, mergePolicy);
        bool eighthAllowed = subdivisionPolicy.AllowedSubdivisions.HasFlag(AllowedSubdivision.Eighth);
        bool isEligible = eighthAllowed && IsEighthOffbeat(onset.Beat);
        int feelOffsetTicks = isEligible && effectiveFeel != GrooveFeel.Straight
            ? ComputeFeelOffsetTicks(effectiveFeel, effectiveSwingAmount)
            : 0;
        int existingOffset = onset.TimingOffsetTicks ?? 0;
        int finalOffset = existingOffset + feelOffsetTicks;

        return new FeelTimingDiagnostics(
            Role: onset.Role,
            Beat: onset.Beat,
            EffectiveFeel: effectiveFeel,
            EffectiveSwingAmount01: effectiveSwingAmount,
            EighthAllowed: eighthAllowed,
            IsEligibleOffbeat: isEligible,
            ExistingOffsetTicks: existingOffset,
            FeelOffsetTicks: feelOffsetTicks,
            FinalOffsetTicks: finalOffset);
    }
}

/// <summary>
/// Diagnostic record for feel timing computations (Story G1 support).
/// </summary>
public sealed record FeelTimingDiagnostics(
    string Role,
    decimal Beat,
    GrooveFeel EffectiveFeel,
    double EffectiveSwingAmount01,
    bool EighthAllowed,
    bool IsEligibleOffbeat,
    int ExistingOffsetTicks,
    int FeelOffsetTicks,
    int FinalOffsetTicks);
