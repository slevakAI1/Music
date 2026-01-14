// AI: purpose=Non-throwing validation helper for material track invariants; groundwork for future wiring.
// AI: invariants=Material tracks should use MaterialLocal domain; no negative ticks allowed.
// AI: deps=References PartTrack.Meta and PartTrackNoteEvents; does not throw.

using Music.Generator;

namespace Music.Song.Material;

/// <summary>
/// Non-throwing validation helper for PartTrack material invariants.
/// </summary>
internal static class PartTrackMaterialValidation
{
    /// <summary>
    /// Validates a PartTrack for material-specific constraints.
    /// Returns empty list if valid; otherwise returns list of issue descriptions.
    /// </summary>
    public static IReadOnlyList<string> Validate(PartTrack track)
    {
        ArgumentNullException.ThrowIfNull(track);

        var issues = new List<string>();

        if (track.Meta.Kind is PartTrackKind.MaterialFragment or PartTrackKind.MaterialVariant)
        {
            if (track.Meta.Domain != PartTrackDomain.MaterialLocal)
            {
                issues.Add("Material tracks should use MaterialLocal domain.");
            }

            if (track.PartTrackNoteEvents.Any(e => e.AbsoluteTimeTicks < 0))
            {
                issues.Add("Material tracks must not contain negative ticks.");
            }
        }

        return issues;
    }
}
