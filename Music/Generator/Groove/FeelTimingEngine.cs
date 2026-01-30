// AI: purpose=Apply groove feel timing (straight/swing/shuffle/triplet) to onset positions (Story E1).
// AI: invariants=Deterministic output; only eighth offbeats (n+0.5) are eligible; Beat unchanged, only TimingOffsetTicks modified.
// AI: deps=GrooveFeel, AllowedSubdivision, MusicConstants.TicksPerQuarterNote.

namespace Music.Generator.Groove;

/// <summary>
/// Applies groove feel timing (straight, swing, shuffle, triplet) to onset positions.
/// Story E1: Feel timing shifts eighth offbeats to create stylistically appropriate pocket.
/// </summary>
/// <remarks>
/// Eligibility: Only eighth offbeats (beat = integer + 0.5) are shifted.
/// Downbeats, sixteenths, and triplet positions are never shifted by E1.
/// Feel timing is additive: existing TimingOffsetTicks is preserved and feel offset is added.
/// </remarks>
public static class FeelTimingEngine
{
    private const double BeatEpsilon = 0.002; // Tolerance for floating-point beat comparisons

    /// <summary>
    /// Applies feel timing to a list of onsets. Returns new immutable records with computed timing offsets.
    /// </summary>
    /// <param name="onsets">Source onsets to apply feel timing to</param>
    /// <param name="feel">The groove feel to apply</param>
    /// <param name="swingAmount01">Swing amount (0.0 to 1.0), used only for Swing feel</param>
    /// <param name="allowedSubdivisions">Allowed subdivisions flags</param>
    /// <returns>New onset list with TimingOffsetTicks computed for eligible onsets</returns>
    public static IReadOnlyList<GrooveOnset> ApplyFeelTiming(
        IReadOnlyList<GrooveOnset> onsets,
        GrooveFeel feel = GrooveFeel.Straight,
        double swingAmount01 = 0.0,
        AllowedSubdivision allowedSubdivisions = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth)
    {
        ArgumentNullException.ThrowIfNull(onsets);

        if (onsets.Count == 0)
            return onsets;

        // Straight feel: no shift applied
        if (feel == GrooveFeel.Straight)
            return onsets;

        // Check if Eighth subdivision is allowed - if not, no feel timing can be applied
        bool eighthAllowed = allowedSubdivisions.HasFlag(AllowedSubdivision.Eighth);

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
            int feelOffsetTicks = ComputeFeelOffsetTicks(feel, swingAmount01);

            // Apply additive timing: existing offset + feel offset
            int existingOffset = onset.TimingOffsetTicks ?? 0;
            int newOffset = existingOffset + feelOffsetTicks;

            result.Add(onset with { TimingOffsetTicks = newOffset });
        }

        return result;
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
    /// <param name="feel">The groove feel to apply.</param>
    /// <param name="swingAmount01">Swing amount (0.0 to 1.0).</param>
    /// <param name="allowedSubdivisions">Allowed subdivisions flags.</param>
    /// <returns>Diagnostics record with computed values and sources.</returns>
    public static FeelTimingDiagnostics ComputeFeelOffsetWithDiagnostics(
        GrooveOnset onset,
        GrooveFeel feel = GrooveFeel.Straight,
        double swingAmount01 = 0.0,
        AllowedSubdivision allowedSubdivisions = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth)
    {
        bool eighthAllowed = allowedSubdivisions.HasFlag(AllowedSubdivision.Eighth);
        bool isEligible = eighthAllowed && IsEighthOffbeat(onset.Beat);
        int feelOffsetTicks = isEligible && feel != GrooveFeel.Straight
            ? ComputeFeelOffsetTicks(feel, swingAmount01)
            : 0;
        int existingOffset = onset.TimingOffsetTicks ?? 0;
        int finalOffset = existingOffset + feelOffsetTicks;

        return new FeelTimingDiagnostics(
            Role: onset.Role,
            Beat: onset.Beat,
            EffectiveFeel: feel,
            EffectiveSwingAmount01: swingAmount01,
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
