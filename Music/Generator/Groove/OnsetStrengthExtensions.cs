// AI: purpose=Extension methods for integrating OnsetStrengthClassifier with GrooveOnset.
// AI: invariants=Respects existing Strength value if already set; returns new onset with computed strength.
// AI: deps=OnsetStrengthClassifier for classification logic; AllowedSubdivision for grid context.
// AI: change=If adding extensions for other types, ensure they stay in correct namespace (Drum types in Drum namespace).

namespace Music.Generator.Groove;

/// <summary>
/// Extension methods for integrating OnsetStrengthClassifier with GrooveOnset.
/// Demonstrates typical usage patterns for onset strength classification in groove pipelines.
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
