namespace Music.Generator;

/// <summary>
/// Classifies onset positions into strength buckets for velocity and timing policies.
/// Classification is deterministic and meter-aware.
/// </summary>
public static class OnsetStrengthClassifier
{
    private const double BeatEpsilon = 0.002; // Tolerance for floating-point beat comparisons

    /// <summary>
    /// Classifies an onset beat position into an OnsetStrength bucket.
    /// Respects explicit candidate strength when provided.
    /// </summary>
    /// <param name="beat">Beat position in 1-based quarter-note units (e.g., 1.0, 2.5, 4.75)</param>
    /// <param name="beatsPerBar">Meter numerator (e.g., 4 for 4/4, 3 for 3/4)</param>
    /// <param name="explicitStrength">Optional explicit strength from GrooveOnsetCandidate (overrides computed)</param>
    /// <returns>OnsetStrength classification</returns>
    public static OnsetStrength Classify(decimal beat, int beatsPerBar, OnsetStrength? explicitStrength = null)
    {
        // Honor explicit candidate strength override
        if (explicitStrength.HasValue)
            return explicitStrength.Value;

        double beatPos = (double)beat;

        // 1. Downbeat: beat 1 (bar start)
        if (IsNearBeat(beatPos, 1.0))
            return OnsetStrength.Downbeat;

        // 2. Pickup: anticipations (.75 positions, last 16th before strong beats)
        if (IsPickupPosition(beatPos))
            return OnsetStrength.Pickup;

        // 3. Backbeat: meter-specific backbeat positions
        if (IsBackbeat(beatPos, beatsPerBar))
            return OnsetStrength.Backbeat;

        // 4. Strong: other strong beats (meter-specific)
        if (IsStrongBeat(beatPos, beatsPerBar))
            return OnsetStrength.Strong;

        // 5. Offbeat: eighth note offbeats (.5 positions)
        if (IsOffbeatPosition(beatPos))
            return OnsetStrength.Offbeat;

        // 6. Default fallback: treat as generic strong beat
        return OnsetStrength.Strong;
    }

    /// <summary>
    /// Determines if a beat position is a backbeat for the given meter.
    /// 
    /// Meter-specific rules:
    /// - 4/4: beats 2 and 4 (standard backbeat)
    /// - 3/4: beat 2 (middle beat emphasis)
    /// - 2/4: beat 2
    /// - 6/8: beats 2 and 4 (compound meter, half-bar emphasis)
    /// - 5/4: beats 2 and 4 (asymmetric meter, fallback to even beats)
    /// - 7/4: beats 3 and 5 (asymmetric meter, fallback pattern)
    /// - Other meters: even beats (deterministic fallback)
    /// </summary>
    private static bool IsBackbeat(double beat, int beatsPerBar)
    {
        return beatsPerBar switch
        {
            4 => IsNearBeat(beat, 2.0) || IsNearBeat(beat, 4.0),  // Standard 4/4 backbeat
            3 => IsNearBeat(beat, 2.0),                            // 3/4 middle beat
            2 => IsNearBeat(beat, 2.0),                            // 2/4 backbeat
            6 => IsNearBeat(beat, 2.0) || IsNearBeat(beat, 4.0),  // 6/8 compound feel
            5 => IsNearBeat(beat, 2.0) || IsNearBeat(beat, 4.0),  // 5/4 asymmetric
            7 => IsNearBeat(beat, 3.0) || IsNearBeat(beat, 5.0),  // 7/4 asymmetric
            _ => IsEvenBeat(beat)                                   // Fallback: even beats
        };
    }

    /// <summary>
    /// Determines if a beat position is a strong beat (non-downbeat, non-backbeat emphasis).
    /// 
    /// Meter-specific rules:
    /// - 4/4: beat 3 (strong beat, not backbeat)
    /// - 3/4: beat 3 (last beat emphasis)
    /// - 2/4: no additional strong beats
    /// - 6/8: beats 3 and 5 (compound meter subdivisions)
    /// - 5/4: beat 3 or 5 (depending on grouping; use beat 3)
    /// - 7/4: beats 2, 4, 6 (complement to backbeats)
    /// - Other meters: odd beats beyond downbeat
    /// </summary>
    private static bool IsStrongBeat(double beat, int beatsPerBar)
    {
        return beatsPerBar switch
        {
            4 => IsNearBeat(beat, 3.0),                                          // 4/4 strong beat
            3 => IsNearBeat(beat, 3.0),                                          // 3/4 last beat
            2 => false,                                                           // 2/4 has no additional strong beats
            6 => IsNearBeat(beat, 3.0) || IsNearBeat(beat, 5.0),                // 6/8 compound subdivisions
            5 => IsNearBeat(beat, 3.0),                                          // 5/4 middle emphasis
            7 => IsNearBeat(beat, 2.0) || IsNearBeat(beat, 4.0) || IsNearBeat(beat, 6.0), // 7/4 complement
            _ => IsOddBeatBeyondDownbeat(beat)                                    // Fallback: odd beats
        };
    }

    /// <summary>
    /// Detects eighth note offbeats (.5 fractional positions).
    /// Examples: 1.5, 2.5, 3.5, 4.5
    /// </summary>
    private static bool IsOffbeatPosition(double beat)
    {
        double fractional = beat - Math.Floor(beat);
        return Math.Abs(fractional - 0.5) < BeatEpsilon;
    }

    /// <summary>
    /// Detects pickup/anticipation positions (.75 fractional, last 16th before strong beats).
    /// Examples: 1.75, 2.75, 3.75, 4.75
    /// Also includes the last 16th of the bar (e.g., 4.75 in 4/4).
    /// </summary>
    private static bool IsPickupPosition(double beat)
    {
        double fractional = beat - Math.Floor(beat);
        return Math.Abs(fractional - 0.75) < BeatEpsilon;
    }

    /// <summary>
    /// Checks if beat position is near a target beat (within epsilon tolerance).
    /// </summary>
    private static bool IsNearBeat(double beat, double targetBeat)
    {
        return Math.Abs(beat - targetBeat) < BeatEpsilon;
    }

    /// <summary>
    /// Checks if beat position is an even integer beat (2.0, 4.0, 6.0, etc.).
    /// Used as fallback backbeat detection for unusual meters.
    /// </summary>
    private static bool IsEvenBeat(double beat)
    {
        int rounded = (int)Math.Round(beat);
        return Math.Abs(beat - rounded) < BeatEpsilon && rounded > 0 && rounded % 2 == 0;
    }

    /// <summary>
    /// Checks if beat position is an odd integer beat beyond the downbeat (3.0, 5.0, 7.0, etc.).
    /// Used as fallback strong beat detection for unusual meters.
    /// </summary>
    private static bool IsOddBeatBeyondDownbeat(double beat)
    {
        int rounded = (int)Math.Round(beat);
        return Math.Abs(beat - rounded) < BeatEpsilon && rounded > 1 && rounded % 2 == 1;
    }
}
