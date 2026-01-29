using Music.Generator.Agents.Drums;

namespace Music.Generator.Groove;

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
    /// <param name="onset">The onset to classify</param>
    /// <param name="beatsPerBar">Meter numerator</param>
    /// <param name="allowedSubdivisions">Active subdivision grid for grid-relative detection</param>
    public static GrooveOnset WithClassifiedStrength(
        this GrooveOnset onset, 
        int beatsPerBar,
        AllowedSubdivision allowedSubdivisions)
    {
        // If strength already set, preserve it
        if (onset.Strength.HasValue)
            return onset;

        var classified = OnsetStrengthClassifier.Classify(onset.Beat, beatsPerBar, allowedSubdivisions);
        
        return onset with { Strength = classified };
    }

    /// <summary>
    /// Classifies a DrumOnsetCandidate's strength based on its position.
    /// The candidate's explicit Strength field takes precedence over computed classification.
    /// </summary>
    /// <param name="candidate">The candidate to classify</param>
    /// <param name="beatsPerBar">Meter numerator</param>
    /// <param name="allowedSubdivisions">Active subdivision grid for grid-relative detection</param>
    public static OnsetStrength GetEffectiveStrength(
        this DrumOnsetCandidate candidate, 
        int beatsPerBar,
        AllowedSubdivision allowedSubdivisions)
    {
        return OnsetStrengthClassifier.Classify(
            candidate.OnsetBeat, 
            beatsPerBar, 
            allowedSubdivisions,
            candidate.Strength);
    }

    /// <summary>
    /// Batch classifies all onsets in a list, preserving existing strength values.
    /// </summary>
    /// <param name="onsets">Onsets to classify</param>
    /// <param name="beatsPerBar">Meter numerator</param>
    /// <param name="allowedSubdivisions">Active subdivision grid for grid-relative detection</param>
    public static List<GrooveOnset> ClassifyStrengths(
        this IEnumerable<GrooveOnset> onsets, 
        int beatsPerBar,
        AllowedSubdivision allowedSubdivisions)
    {
        return onsets.Select(o => o.WithClassifiedStrength(beatsPerBar, allowedSubdivisions)).ToList();
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
    /// <param name="anchor">The anchor layer containing base onsets</param>
    /// <param name="barNumber">Bar number in the song</param>
    /// <param name="beatsPerBar">Meter numerator</param>
    /// <param name="allowedSubdivisions">Active subdivision grid</param>
    public static List<GrooveOnset> ClassifyAnchorOnsets(
        GrooveInstanceLayer anchor,
        int barNumber,
        int beatsPerBar,
        AllowedSubdivision allowedSubdivisions)
    {
        var onsets = new List<GrooveOnset>();

        // Extract kick anchors and classify
        foreach (var beat in anchor.KickOnsets)
        {
            var strength = OnsetStrengthClassifier.Classify(beat, beatsPerBar, allowedSubdivisions);
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
            var strength = OnsetStrengthClassifier.Classify(beat, beatsPerBar, allowedSubdivisions);
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
    /// <param name="candidate">The candidate to classify</param>
    /// <param name="beatsPerBar">Meter numerator</param>
    /// <param name="allowedSubdivisions">Active subdivision grid</param>
    public static OnsetStrength GetCandidateStrength(
        DrumOnsetCandidate candidate,
        int beatsPerBar,
        AllowedSubdivision allowedSubdivisions)
    {
        // Classifier automatically respects candidate.Strength if set
        return OnsetStrengthClassifier.Classify(
            candidate.OnsetBeat,
            beatsPerBar,
            allowedSubdivisions,
            candidate.Strength
        );
    }

    /// <summary>
    /// Example: Filter candidates by strength (e.g., only allow ghost notes in specific bars)
    /// </summary>
    /// <param name="candidates">Candidates to filter</param>
    /// <param name="beatsPerBar">Meter numerator</param>
    /// <param name="allowedSubdivisions">Active subdivision grid</param>
    /// <param name="allowedStrengths">Allowed strength values</param>
    public static List<DrumOnsetCandidate> FilterCandidatesByStrength(
        List<DrumOnsetCandidate> candidates,
        int beatsPerBar,
        AllowedSubdivision allowedSubdivisions,
        params OnsetStrength[] allowedStrengths)
    {
        var allowedSet = new HashSet<OnsetStrength>(allowedStrengths);
        
        return candidates
            .Where(c => allowedSet.Contains(c.GetEffectiveStrength(beatsPerBar, allowedSubdivisions)))
            .ToList();
    }

    /// <summary>
    /// Example: Prepare onsets for velocity shaping (Story D2)
    /// All onsets must have classified strength before velocity shaping.
    /// </summary>
    /// <param name="onsets">Onsets to prepare</param>
    /// <param name="beatsPerBar">Meter numerator</param>
    /// <param name="allowedSubdivisions">Active subdivision grid</param>
    public static List<GrooveOnset> PrepareOnsetsForVelocityShaping(
        List<GrooveOnset> onsets,
        int beatsPerBar,
        AllowedSubdivision allowedSubdivisions)
    {
        return onsets
            .Select(o => o.Strength.HasValue 
                ? o 
                : o with { Strength = OnsetStrengthClassifier.Classify(o.Beat, beatsPerBar, allowedSubdivisions) })
            .ToList();
    }
}

