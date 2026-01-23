namespace Music.Generator.Groove;

/// <summary>
/// Classifies onset positions into strength buckets for velocity and timing policies.
/// Classification is deterministic and meter-aware, with grid-relative offbeat/pickup detection.
/// </summary>
/// <remarks>
/// Classification precedence (computed): Pickup → Downbeat → Backbeat → Strong → Offbeat → Strong (fallback).
/// Explicit overrides always take precedence over computed classification.
/// </remarks>
public static class OnsetStrengthClassifier
{
    private const double BeatEpsilon = 0.002; // Tolerance for floating-point beat comparisons (shared with grid logic)

    /// <summary>
    /// Classifies an onset beat position into an OnsetStrength bucket.
    /// Respects explicit candidate strength when provided.
    /// </summary>
    /// <param name="beat">Beat position in 1-based quarter-note units (e.g., 1.0, 2.5, 4.75)</param>
    /// <param name="beatsPerBar">Meter numerator (e.g., 4 for 4/4, 6 for 6/8)</param>
    /// <param name="allowedSubdivisions">Active subdivision grid for grid-relative offbeat/pickup detection</param>
    /// <param name="explicitStrength">Optional explicit strength from GrooveOnsetCandidate (overrides computed)</param>
    /// <returns>OnsetStrength classification</returns>
    public static OnsetStrength Classify(
        decimal beat, 
        int beatsPerBar, 
        AllowedSubdivision allowedSubdivisions,
        OnsetStrength? explicitStrength = null)
    {
        // Honor explicit candidate strength override unconditionally
        if (explicitStrength.HasValue)
            return explicitStrength.Value;

        double beatPos = (double)beat;

        // Classification precedence: Pickup → Downbeat → Backbeat → Strong → Offbeat → fallback
        
        // 1. Pickup: anticipations (grid-relative, last subdivision before stronger beat)
        if (IsPickupPosition(beatPos, beatsPerBar, allowedSubdivisions))
            return OnsetStrength.Pickup;

        // 2. Downbeat: beat 1 (bar start)
        if (IsNearBeat(beatPos, 1.0))
            return OnsetStrength.Downbeat;

        // 3. Backbeat: meter-specific backbeat positions
        if (IsBackbeat(beatPos, beatsPerBar))
            return OnsetStrength.Backbeat;

        // 4. Strong: other strong beats (meter-specific)
        if (IsStrongBeat(beatPos, beatsPerBar))
            return OnsetStrength.Strong;

        // 5. Offbeat: grid-relative offbeat positions
        if (IsOffbeatPosition(beatPos, allowedSubdivisions))
            return OnsetStrength.Offbeat;

        // 6. Default fallback: treat as generic strong beat
        return OnsetStrength.Strong;
    }

    /// <summary>
    /// Determines if a beat position is a backbeat for the given meter.
    /// 
    /// Meter-specific rules (required meters):
    /// - 2/4: beat 2
    /// - 3/4: beat 2 (configurable; see GrooveAccentPolicy for style overrides)
    /// - 4/4: beats 2 and 4 (standard backbeat)
    /// - 5/4: beats 2 and 4 (asymmetric meter, works for 3+2 and 2+3)
    /// - 6/8: beat 4 (compound meter, second big pulse)
    /// - 7/4: beats 3 and 5 (asymmetric meter)
    /// - 12/8: beat 7 (compound meter, second big pulse at midpoint)
    /// 
    /// Fallback for other meters (deterministic):
    /// - Even meters (N): backbeat at (N/2 + 1)
    /// - Odd meters (N): backbeat at nearest integer to (N/2 + 0.5), rounded up
    /// </summary>
    private static bool IsBackbeat(double beat, int beatsPerBar)
    {
        return beatsPerBar switch
        {
            2 => IsNearBeat(beat, 2.0),                            // 2/4: simple duple
            3 => IsNearBeat(beat, 2.0),                            // 3/4: middle beat
            4 => IsNearBeat(beat, 2.0) || IsNearBeat(beat, 4.0),  // 4/4: standard backbeat
            5 => IsNearBeat(beat, 2.0) || IsNearBeat(beat, 4.0),  // 5/4: asymmetric
            6 => IsNearBeat(beat, 4.0),                            // 6/8: compound feel, second pulse
            7 => IsNearBeat(beat, 3.0) || IsNearBeat(beat, 5.0),  // 7/4: asymmetric
            12 => IsNearBeat(beat, 7.0),                           // 12/8: compound feel, midpoint pulse
            _ => IsBackbeatFallback(beat, beatsPerBar)             // Deterministic fallback
        };
    }

    /// <summary>
    /// Fallback backbeat detection for uncommon meters.
    /// Even meters: backbeat at (N/2 + 1)
    /// Odd meters: backbeat at nearest integer to (N/2 + 0.5), rounded up
    /// </summary>
    private static bool IsBackbeatFallback(double beat, int beatsPerBar)
    {
        if (beatsPerBar % 2 == 0)
        {
            // Even meter: backbeat is the first beat of second half
            int backbeatPosition = (beatsPerBar / 2) + 1;
            return IsNearBeat(beat, backbeatPosition);
        }
        else
        {
            // Odd meter: backbeat is near the midpoint, rounded up
            int backbeatPosition = (int)Math.Ceiling(beatsPerBar / 2.0 + 0.5);
            return IsNearBeat(beat, backbeatPosition);
        }
    }

    /// <summary>
    /// Determines if a beat position is a strong beat (non-downbeat, non-backbeat emphasis).
    /// 
    /// Meter-specific rules (required meters):
    /// - 2/4: no additional strong beats
    /// - 3/4: beat 3 (last beat emphasis)
    /// - 4/4: beat 3 (strong beat, not backbeat)
    /// - 5/4: beat 3 (middle emphasis, works for 3+2 and 2+3)
    /// - 6/8: beats 3 and 6 (middle of each pulse group - optional)
    /// - 7/4: beats 2, 4, 6 (complement to backbeats)
    /// - 12/8: beats 4 and 10 (middle of each pulse group - optional)
    /// 
    /// Fallback for other meters (deterministic):
    /// - Even meters (N >= 4): beat (N/2)
    /// - Odd meters: all odd beats > 1 that are not the backbeat
    /// </summary>
    private static bool IsStrongBeat(double beat, int beatsPerBar)
    {
        return beatsPerBar switch
        {
            2 => false,                                                           // 2/4: no additional strong beats
            3 => IsNearBeat(beat, 3.0),                                          // 3/4: last beat
            4 => IsNearBeat(beat, 3.0),                                          // 4/4: strong beat
            5 => IsNearBeat(beat, 3.0),                                          // 5/4: middle emphasis
            6 => IsNearBeat(beat, 3.0) || IsNearBeat(beat, 6.0),                // 6/8: pulse midpoints
            7 => IsNearBeat(beat, 2.0) || IsNearBeat(beat, 4.0) || IsNearBeat(beat, 6.0), // 7/4: complement
            12 => IsNearBeat(beat, 4.0) || IsNearBeat(beat, 10.0),              // 12/8: pulse midpoints
            _ => IsStrongBeatFallback(beat, beatsPerBar)                         // Deterministic fallback
        };
    }

    /// <summary>
    /// Fallback strong beat detection for uncommon meters.
    /// Even meters (N >= 4): beat (N/2) is strong
    /// Odd meters: all odd beats > 1 that are not the backbeat
    /// </summary>
    private static bool IsStrongBeatFallback(double beat, int beatsPerBar)
    {
        int roundedBeat = (int)Math.Round(beat);
        if (Math.Abs(beat - roundedBeat) >= BeatEpsilon)
            return false; // Not on an integer beat

        if (beatsPerBar % 2 == 0)
        {
            // Even meter: beat (N/2) is strong (immediately before backbeat)
            if (beatsPerBar >= 4)
                return roundedBeat == beatsPerBar / 2;
            return false;
        }
        else
        {
            // Odd meter: all odd beats > 1 that are not the backbeat
            int backbeatPosition = (int)Math.Ceiling(beatsPerBar / 2.0 + 0.5);
            return roundedBeat > 1 && roundedBeat % 2 == 1 && roundedBeat != backbeatPosition;
        }
    }

    /// <summary>
    /// Detects offbeat positions relative to the active subdivision grid.
    /// 
    /// Grid-relative offbeat detection:
    /// - Eighth grid (step 0.5): integer + 0.5 (e.g., 1.5, 2.5, 3.5, 4.5)
    /// - Triplet grid (step 1/3): integer + 1/3 (middle triplet, e.g., 1.333, 2.333)
    /// - Sixteenth grid (step 0.25): integer + 0.5 (same as eighth)
    /// 
    /// Note: For triplet grids, can optionally also treat integer + 2/3 as offbeat,
    /// but current implementation uses middle triplet only (integer + 1/3).
    /// </summary>
    private static bool IsOffbeatPosition(double beat, AllowedSubdivision allowedSubdivisions)
    {
        double fractional = beat - Math.Floor(beat);

        // Eighth grid offbeats (.5 positions)
        if (allowedSubdivisions.HasFlag(AllowedSubdivision.Eighth) ||
            allowedSubdivisions.HasFlag(AllowedSubdivision.Sixteenth))
        {
            if (Math.Abs(fractional - 0.5) < BeatEpsilon)
                return true;
        }

        // Triplet grid offbeats (middle triplet at +1/3)
        if (allowedSubdivisions.HasFlag(AllowedSubdivision.EighthTriplet) ||
            allowedSubdivisions.HasFlag(AllowedSubdivision.SixteenthTriplet))
        {
            double thirdPosition = 1.0 / 3.0;
            if (Math.Abs(fractional - thirdPosition) < BeatEpsilon)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Detects pickup/anticipation positions relative to the active subdivision grid.
    /// Pickups are the last subdivision before a stronger beat.
    /// 
    /// Grid-relative pickup detection:
    /// - Sixteenth grid (step 0.25): integer + 0.75 (last 16th, e.g., 1.75, 2.75)
    /// - Triplet grid (step 1/3): integer + 2/3 (last triplet, e.g., 1.666, 2.666)
    /// - Bar-end anticipation: applies same rule to final beat of bar
    /// </summary>
    private static bool IsPickupPosition(double beat, int beatsPerBar, AllowedSubdivision allowedSubdivisions)
    {
        double fractional = beat - Math.Floor(beat);

        // Sixteenth grid pickups (.75 positions - last 16th before next beat)
        if (allowedSubdivisions.HasFlag(AllowedSubdivision.Sixteenth))
        {
            if (Math.Abs(fractional - 0.75) < BeatEpsilon)
                return true;
        }

        // Triplet grid pickups (last triplet at +2/3)
        if (allowedSubdivisions.HasFlag(AllowedSubdivision.EighthTriplet) ||
            allowedSubdivisions.HasFlag(AllowedSubdivision.SixteenthTriplet))
        {
            double twoThirdsPosition = 2.0 / 3.0;
            if (Math.Abs(fractional - twoThirdsPosition) < BeatEpsilon)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if beat position is near a target beat (within epsilon tolerance).
    /// </summary>
    private static bool IsNearBeat(double beat, double targetBeat)
    {
        return Math.Abs(beat - targetBeat) < BeatEpsilon;
    }
}

