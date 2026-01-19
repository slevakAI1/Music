namespace Music.Generator;

/// <summary>
/// Extension methods for integrating OnsetStrengthClassifier with groove types.
/// Demonstrates typical usage patterns for Story D1 implementation.
/// </summary>
public static class OnsetStrengthExtensions
{
    /// <summary>
    /// Classifies a GrooveOnset and returns a new onset with the strength field populated.
    /// Respects existing strength value if already set.
    /// </summary>
    public static GrooveOnset WithClassifiedStrength(this GrooveOnset onset, int beatsPerBar)
    {
        // If strength already set, preserve it
        if (onset.Strength.HasValue)
            return onset;

        var classified = OnsetStrengthClassifier.Classify(onset.Beat, beatsPerBar);
        
        return onset with { Strength = classified };
    }

    /// <summary>
    /// Classifies a GrooveOnsetCandidate's strength based on its position.
    /// The candidate's explicit Strength field takes precedence over computed classification.
    /// </summary>
    public static OnsetStrength GetEffectiveStrength(this GrooveOnsetCandidate candidate, int beatsPerBar)
    {
        return OnsetStrengthClassifier.Classify(candidate.OnsetBeat, beatsPerBar, candidate.Strength);
    }

    /// <summary>
    /// Batch classifies all onsets in a list, preserving existing strength values.
    /// </summary>
    public static List<GrooveOnset> ClassifyStrengths(this IEnumerable<GrooveOnset> onsets, int beatsPerBar)
    {
        return onsets.Select(o => o.WithClassifiedStrength(beatsPerBar)).ToList();
    }
}

/// <summary>
/// Example usage patterns for onset strength classification in groove pipelines.
/// These examples show how Story D1 integrates with existing groove system components.
/// </summary>
public static class OnsetStrengthUsageExamples
{
    /// <summary>
    /// Example: Classify anchors extracted from GrooveInstanceLayer
    /// </summary>
    public static List<GrooveOnset> ClassifyAnchorOnsets(
        GrooveInstanceLayer anchor,
        int barNumber,
        int beatsPerBar)
    {
        var onsets = new List<GrooveOnset>();

        // Extract kick anchors and classify
        foreach (var beat in anchor.KickOnsets)
        {
            var strength = OnsetStrengthClassifier.Classify(beat, beatsPerBar);
            onsets.Add(new GrooveOnset
            {
                Role = GrooveRoles.Kick,
                BarNumber = barNumber,
                Beat = beat,
                Strength = strength,
                Velocity = null,
                TimingOffsetTicks = null,
                Provenance = null,
                IsMustHit = false,
                IsNeverRemove = false,
                IsProtected = false
            });
        }

        // Extract snare anchors and classify
        foreach (var beat in anchor.SnareOnsets)
        {
            var strength = OnsetStrengthClassifier.Classify(beat, beatsPerBar);
            onsets.Add(new GrooveOnset
            {
                Role = GrooveRoles.Snare,
                BarNumber = barNumber,
                Beat = beat,
                Strength = strength,
                Velocity = null,
                TimingOffsetTicks = null,
                Provenance = null,
                IsMustHit = false,
                IsNeverRemove = false,
                IsProtected = false
            });
        }

        return onsets;
    }

    /// <summary>
    /// Example: Classify variation candidates, respecting explicit strength overrides
    /// </summary>
    public static OnsetStrength GetCandidateStrength(
        GrooveOnsetCandidate candidate,
        int beatsPerBar)
    {
        // Classifier automatically respects candidate.Strength if set
        return OnsetStrengthClassifier.Classify(
            candidate.OnsetBeat,
            beatsPerBar,
            candidate.Strength
        );
    }

    /// <summary>
    /// Example: Filter candidates by strength (e.g., only allow ghost notes in specific bars)
    /// </summary>
    public static List<GrooveOnsetCandidate> FilterCandidatesByStrength(
        List<GrooveOnsetCandidate> candidates,
        int beatsPerBar,
        params OnsetStrength[] allowedStrengths)
    {
        var allowedSet = new HashSet<OnsetStrength>(allowedStrengths);
        
        return candidates
            .Where(c => allowedSet.Contains(c.GetEffectiveStrength(beatsPerBar)))
            .ToList();
    }

    /// <summary>
    /// Example: Prepare onsets for velocity shaping (Story D2)
    /// All onsets must have classified strength before velocity shaping.
    /// </summary>
    public static List<GrooveOnset> PrepareOnsetsForVelocityShaping(
        List<GrooveOnset> onsets,
        int beatsPerBar)
    {
        return onsets
            .Select(o => o.Strength.HasValue 
                ? o 
                : o with { Strength = OnsetStrengthClassifier.Classify(o.Beat, beatsPerBar) })
            .ToList();
    }
}
